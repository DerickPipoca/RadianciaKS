using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadianciaKS.Application.DTOs.Licensing;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;
using Moq.EntityFrameworkCore;
using RadianciaKS.Infrastructure.Licensing.Configuration;
using RadianciaKS.Infrastructure.Services;

namespace RadianciaKS.UnitTests.Infrastructure
{
    public class LicenseSyncServiceTests
    {
        private readonly Mock<IApplicationDbContext> _dbContextMock;
        private readonly Mock<IKeygenService> _keygenServiceMock;
        private readonly Mock<IOptions<KeygenSettings>> _settingsMock;
        private readonly Mock<ILogger<LicenseSyncService>> _loggerMock;
        private readonly LicenseSyncService _service;
        private readonly KeygenSettings _settings;

        public LicenseSyncServiceTests()
        {
            _dbContextMock = new Mock<IApplicationDbContext>();
            _keygenServiceMock = new Mock<IKeygenService>();
            _settingsMock = new Mock<IOptions<KeygenSettings>>();
            _loggerMock = new Mock<ILogger<LicenseSyncService>>();

            _settings = new KeygenSettings
            {
                AccountId = "acc-test-123",
                MachineName = "Terminal-PDV-Principal",
                HeartbeatHours = 6
            };
            _settingsMock.Setup(s => s.Value).Returns(_settings);

            _service = new LicenseSyncService(
                _dbContextMock.Object,
                _keygenServiceMock.Object,
                _settingsMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task SyncLicenseAsync_WhenNoLicenseInDatabase_ShouldLogWarningAndReturnEarly()
        {
            // Arrange
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense>());

            // Act
            await _service.SyncLicenseAsync();

            // Assert
            _keygenServiceMock.Verify(
                k => k.ValidateKeyAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SyncLicenseAsync_WhenMachineFingerprintIsMissing_ShouldGenerateGuidFingerprint()
        {
            // Arrange
            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "LIC-ABC-123",
                MachineFingerprint = null,
                Status = LicenseStatus.UNVALIDATED
            };
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            var onlineResult = new KeygenValidationResult(
                IsOnline: true,
                IsValid: true,
                Code: "VALID",
                Expiry: DateTimeOffset.UtcNow.AddMonths(1),
                LicenseId: "lic-id-1",
                ErrorMessage: null
            );

            _keygenServiceMock
                .Setup(k => k.ValidateKeyAsync(license.LicenseKey, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(onlineResult);

            // Act
            await _service.SyncLicenseAsync();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(license.MachineFingerprint));
            Assert.Equal(32, license.MachineFingerprint.Length); // GUID no formato "N"
            _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Theory]
        [InlineData("NO_MACHINE")]
        [InlineData("NO_MACHINES")]
        [InlineData("FINGERPRINT_SCOPE_MISMATCH")]
        public async Task SyncLicenseAsync_WhenKeygenDemandsMachine_ShouldRegisterMachineAndRevalidate(string failureCode)
        {
            // Arrange
            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "KEY-TERMINAL-01",
                MachineFingerprint = "fingerprint-fixo-pdv",
                Status = LicenseStatus.UNVALIDATED
            };
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            var initialResult = new KeygenValidationResult(
                IsOnline: true,
                IsValid: false,
                Code: failureCode,
                Expiry: null,
                LicenseId: "lic-central-99",
                ErrorMessage: "Máquina não vinculada"
            );

            var finalResult = new KeygenValidationResult(
                IsOnline: true,
                IsValid: true,
                Code: "VALID",
                Expiry: DateTimeOffset.UtcNow.AddYears(1),
                LicenseId: "lic-central-99",
                ErrorMessage: null
            );

            _keygenServiceMock
                .SetupSequence(k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialResult)
                .ReturnsAsync(finalResult);

            _keygenServiceMock
                .Setup(k => k.RegisterMachineAsync(
                    license.LicenseKey,
                    initialResult.LicenseId!,
                    license.MachineFingerprint,
                    _settings.MachineName,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _service.SyncLicenseAsync();

            // Assert
            _keygenServiceMock.Verify(
                k => k.RegisterMachineAsync(license.LicenseKey, "lic-central-99", license.MachineFingerprint, _settings.MachineName, It.IsAny<CancellationToken>()),
                Times.Once);

            _keygenServiceMock.Verify(
                k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            Assert.Equal(LicenseStatus.ACTIVE, license.Status);
            Assert.Equal(finalResult.Expiry, license.ExpiresAt);
        }

        [Fact]
        public async Task SyncLicenseAsync_WhenMachineRegistrationFails_ShouldKeepInitialValidationResult()
        {
            // Arrange
            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "KEY-TERMINAL-01",
                MachineFingerprint = "fp-terminal-01",
                Status = LicenseStatus.UNVALIDATED
            };
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            var resultNoMachine = new KeygenValidationResult(
                IsOnline: true,
                IsValid: false,
                Code: "NO_MACHINE",
                Expiry: null,
                LicenseId: "lic-99",
                ErrorMessage: "Sem terminal cadastrado"
            );

            _keygenServiceMock
                .Setup(k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultNoMachine);

            _keygenServiceMock
                .Setup(k => k.RegisterMachineAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _service.SyncLicenseAsync();

            // Assert
            _keygenServiceMock.Verify(
                k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.Equal(LicenseStatus.UNVALIDATED, license.Status);
        }

        [Theory]
        [InlineData("VALID", LicenseStatus.ACTIVE)]
        [InlineData("SUSPENDED", LicenseStatus.SUSPENDED)]
        [InlineData("EXPIRED", LicenseStatus.EXPIRED)]
        [InlineData("UNKNOWN_CODE", LicenseStatus.UNVALIDATED)]
        public async Task SyncLicenseAsync_WhenOnline_ShouldMapStatusAndSyncDates(string code, LicenseStatus expectedStatus)
        {
            // Arrange
            var initialTime = DateTimeOffset.UtcNow.AddDays(-5);
            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "TEST-KEY-XYZ",
                MachineFingerprint = "fp-123",
                Status = LicenseStatus.UNVALIDATED,
                LastKnownSystemTime = initialTime,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(10)
            };
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            var expectedExpiry = DateTimeOffset.UtcNow.AddMonths(6);
            var result = new KeygenValidationResult(
                IsOnline: true,
                IsValid: code == "VALID",
                Code: code,
                Expiry: expectedExpiry,
                LicenseId: "lic-1",
                ErrorMessage: null
            );

            _keygenServiceMock
                .Setup(k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            // Act
            await _service.SyncLicenseAsync();

            // Assert
            Assert.Equal(expectedStatus, license.Status);
            Assert.Equal(expectedExpiry, license.ExpiresAt);
            Assert.NotNull(license.LastValidatedAt);
            Assert.True(license.LastKnownSystemTime > initialTime);
            _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SyncLicenseAsync_WhenOffline_ShouldTriggerContingencyAndTouchSystemTime()
        {
            // Arrange: licença já ativa mantida em contingência local
            var initialSystemTime = DateTimeOffset.UtcNow.AddHours(-2);
            var originalExpiry = DateTimeOffset.UtcNow.AddDays(30);

            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "KEY-CONTINGENCY",
                MachineFingerprint = "fp-local",
                Status = LicenseStatus.ACTIVE,
                LastKnownSystemTime = initialSystemTime,
                ExpiresAt = originalExpiry
            };
            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            var offlineResult = new KeygenValidationResult(
                IsOnline: false,
                IsValid: false,
                Code: "OFFLINE",
                Expiry: null,
                LicenseId: null,
                ErrorMessage: "Timeout de conexão"
            );

            _keygenServiceMock
                .Setup(k => k.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offlineResult);

            // Act
            await _service.SyncLicenseAsync();

            // Assert: status e data de expiração permanecem intactos, apenas o relógio de sistema avança
            Assert.Equal(LicenseStatus.ACTIVE, license.Status);
            Assert.Equal(originalExpiry, license.ExpiresAt);
            Assert.True(license.LastKnownSystemTime > initialSystemTime);
            _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SyncLicenseAsync_WhenUnexpectedExceptionOccurs_ShouldCatchAndLogError()
        {
            // Arrange: simula falha crítica de banco de dados
            _dbContextMock
                .Setup(db => db.SystemLicenses)
                .Throws(new InvalidOperationException("Falha de conexão com PostgreSQL"));

            // Act & Assert: não deve estourar exceção para o caller
            var exception = await Record.ExceptionAsync(() => _service.SyncLicenseAsync());
            Assert.Null(exception);
        }
    }
}