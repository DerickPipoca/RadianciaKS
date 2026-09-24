using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class PrintServiceTests : IDisposable
    {
        private readonly Mock<IStoreSettingsService> _storeSettingsServiceMock;
        private readonly PrintService _printService;
        private readonly string _tempFilePath;

        public PrintServiceTests()
        {
            _storeSettingsServiceMock = new Mock<IStoreSettingsService>();
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"receipt_test_{Guid.NewGuid():N}.bin");

            _printService = new PrintService(_storeSettingsServiceMock.Object);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // Ignora bloqueios temporários de exclusão em testes
                }
            }
        }

        [Fact]
        public async Task PrintReceiptAsync_Should_ReturnTrue_And_WriteAllReceiptSections_When_OrderIsComplete()
        {
            _storeSettingsServiceMock
                .Setup(s => s.GetSettings())
                .ReturnsAsync(new StoreSettingsResponseDto
                {
                    StoreName = "Hamburgueria Radiancia KS",
                    ServiceCharge = 10.0m,
                    Address = "Av. Brasil, 1500",
                    CNPJ = "04252011000110",
                    ReceiptFooter = "Agradecemos a preferencia!"
                });

            var order = new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                ReceiptUrl = "https://dfe-portal.svrs.rs.gov.br/nfce/qrcode?p=123456",
                PaidByName = "Gerente Carlos",
                CreatedByName = "Garcom Joao",
                CreatedAt = new DateTime(2026, 9, 15, 14, 30, 0, DateTimeKind.Utc),
                ServiceFeeAmount = 8.50m,
                TotalAmount = 93.50m,
                Items = new List<OrderItemResponseDto>
            {
                new OrderItemResponseDto
                {
                    ProductName = "Hamburguer Artesanal Especial Mega Blaster com Molho Secreto da Casa", // Dispara o corte com reticencias em FormatLine
                    Quantity = 2,
                    UnitPrice = 35.00m,
                    SelectedModifiers = new List<OrderItemModifierResponseDto>
                    {
                        new OrderItemModifierResponseDto { Name = "Bacon Extra" },
                        new OrderItemModifierResponseDto { Name = "Queijo Cheddar" }
                    }
                }
            },
                Payments = new List<PaymentResponseDto>
            {
                new PaymentResponseDto { Method = PaymentMethod.Cash, Amount = 20.00m },
                new PaymentResponseDto { Method = PaymentMethod.Pix, Amount = 30.00m },
                new PaymentResponseDto { Method = PaymentMethod.CreditCard, Amount = 20.00m },
                new PaymentResponseDto { Method = PaymentMethod.DebitCard, Amount = 20.00m },
                new PaymentResponseDto { Method = (PaymentMethod)99, Amount = 3.50m }
            }
            };

            var service = new PrintService(_storeSettingsServiceMock.Object);

            var success = await service.PrintReceiptAsync(order, _tempFilePath);

            success.ShouldBeTrue();
            File.Exists(_tempFilePath).ShouldBeTrue();
            var fileBytes = await File.ReadAllBytesAsync(_tempFilePath);
            fileBytes.Length.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task PrintReceiptAsync_Should_HandleUnpaidOrder_DefaultNfce_And_MissingOptionalFields()
        {
            _storeSettingsServiceMock
                .Setup(s => s.GetSettings())
                .ReturnsAsync(new StoreSettingsResponseDto
                {
                    StoreName = "Lanchonete Express",
                    ServiceCharge = 0m,
                    Address = "Rua Teste, 100",
                    CNPJ = "00000000000000",
                    ReceiptFooter = string.Empty // Ramo onde o rodape nao e adicionado
                });

            var order = new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                ReceiptUrl = string.Empty, // Dispara a URL padrao de contingencia da NFC-e
                PaidByName = string.Empty,  // Dispara o fallback "n/a"
                CreatedByName = "Garcom Pedro",
                CreatedAt = new DateTime(2026, 9, 15, 14, 30, 0, DateTimeKind.Utc),
                ServiceFeeAmount = 0m,
                TotalAmount = 25.00m,
                Items = new List<OrderItemResponseDto>
            {
                new OrderItemResponseDto
                {
                    ProductName = "Suco 300ml", // Nome curto (nao trunca)
                    Quantity = 1,
                    UnitPrice = 25.00m,
                    SelectedModifiers = new List<OrderItemModifierResponseDto>() // Lista de modificadores vazia
                }
            },
                Payments = new List<PaymentResponseDto>() // Dispara o ramo "NAO PAGO"
            };

            var service = new PrintService(_storeSettingsServiceMock.Object);

            var success = await service.PrintReceiptAsync(order, _tempFilePath);

            success.ShouldBeTrue();
            File.Exists(_tempFilePath).ShouldBeTrue();
            var fileBytes = await File.ReadAllBytesAsync(_tempFilePath);
            fileBytes.Length.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task PrintReceiptAsync_WithValidLogo_ShouldGenerateEscPosRasterAndPrintSuccessfully()
        {
            SetupStoreSettingsMock();
            // Arrange: gera imagem 24x8 contendo pixels pretos, brancos e transparentes
            byte[] logoBytes = CreateTestImageBytes(width: 24, height: 8);
            var order = CreateSampleOrder();
            string tempFile = Path.GetTempFileName();

            try
            {
                // Act
                bool result = await _printService.PrintReceiptAsync(order, tempFile, logoBytes);

                // Assert
                Assert.True(result);
                byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile);

                // Cabeçalho de imagem ESC/POS Raster: GS 'v' '0' 0 (0x1D, 0x76, 0x30, 0x00)
                byte[] expectedRasterHeader = new byte[] { 0x1D, 0x76, 0x30, 0x00 };
                Assert.True(writtenBytes.AsSpan().IndexOf(expectedRasterHeader) >= 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PrintReceiptAsync_WithLogoWiderThanMaxWidth_ShouldResizeAndGenerateRaster()
        {
            SetupStoreSettingsMock();
            // Arrange: largura 400px (superior ao maxWidth de 350px) para testar o Resize
            byte[] logoBytes = CreateTestImageBytes(width: 400, height: 100);
            var order = CreateSampleOrder();
            string tempFile = Path.GetTempFileName();

            try
            {
                // Act
                bool result = await _printService.PrintReceiptAsync(order, tempFile, logoBytes);

                // Assert
                Assert.True(result);
                byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile);
                byte[] expectedRasterHeader = new byte[] { 0x1D, 0x76, 0x30, 0x00 };
                Assert.True(writtenBytes.AsSpan().IndexOf(expectedRasterHeader) >= 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PrintReceiptAsync_WithCorruptLogoBytes_ShouldHandleCatchGracefullyAndPrintReceipt()
        {
            // Arrange
            SetupStoreSettingsMock();
            byte[] corruptLogoBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var order = CreateSampleOrder();
            string tempFile = Path.GetTempFileName();

            try
            {
                // Act
                bool result = await _printService.PrintReceiptAsync(order, tempFile, corruptLogoBytes);

                // Assert
                Assert.True(result);
                byte[] writtenBytes = await File.ReadAllBytesAsync(tempFile);
                Assert.NotEmpty(writtenBytes);
                byte[] expectedRasterHeader = new byte[] { 0x1D, 0x76, 0x30, 0x00 };
                Assert.False(writtenBytes.AsSpan().IndexOf(expectedRasterHeader) >= 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private static byte[] CreateTestImageBytes(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x % 3 == 0)
                    {
                        // Preto opaco (alpha > 128, luminância < 165 -> seta bit no raster)
                        image[x, y] = new Rgba32(0, 0, 0, 255);
                    }
                    else if (x % 3 == 1)
                    {
                        // Branco opaco (alpha > 128, luminância >= 165 -> não seta bit)
                        image[x, y] = new Rgba32(255, 255, 255, 255);
                    }
                    else
                    {
                        // Transparente (alpha <= 128 -> ignorado)
                        image[x, y] = new Rgba32(0, 0, 0, 50);
                    }
                }
            }

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        private static OrderResponseDto CreateSampleOrder()
        {
            return new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                TotalAmount = 55.00m,
                ServiceFeeAmount = 5.00m,
                CreatedAt = DateTime.Now,
                Items = new List<OrderItemResponseDto>
                {
                    new()
                    {
                        ProductName = "X-Burguer Especial",
                        Quantity = 1,
                        UnitPrice = 50.00m
                    }
                },
                Payments = new List<PaymentResponseDto>()
            };
        }

        private void SetupStoreSettingsMock()
        {
            _storeSettingsServiceMock.Setup(s => s.GetSettings())
                .ReturnsAsync(new StoreSettingsResponseDto
                {
                    StoreName = "Radiância KS Grill",
                    ServiceCharge = 10m,
                    CNPJ = "12.345.678/0001-90",
                    Address = "Av. Principal, 100",
                    ReceiptFooter = "Obrigado pela preferência!"
                });
        }

        [Fact]
        public async Task PrintReceiptAsync_Should_ThrowException_When_StoreSettingsServiceFails()
        {
            _storeSettingsServiceMock
                .Setup(s => s.GetSettings())
                .ThrowsAsync(new InvalidOperationException("Falha ao carregar parametros da loja."));

            var order = new OrderResponseDto
            {
                Items = new List<OrderItemResponseDto>(),
                Payments = new List<PaymentResponseDto>()
            };

            var service = new PrintService(_storeSettingsServiceMock.Object);

            var exception = await Should.ThrowAsync<Exception>(() =>
                service.PrintReceiptAsync(order, _tempFilePath));

            exception.Message.ShouldContain("[PrintService] Falha na impressão: Falha ao carregar parametros da loja.");
        }
    }
}