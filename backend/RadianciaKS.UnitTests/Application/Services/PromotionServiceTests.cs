using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class PromotionServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<PromotionRequestDto>> _validatorMock;
        private readonly PromotionService _promotionService;

        public PromotionServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<PromotionRequestDto>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _promotionService = new PromotionService(
                _contextMock.Object,
                _validatorMock.Object
            );
        }

        private Mock<DbSet<Promotion>> SetupPromotionsDbSet(List<Promotion> promotions)
        {
            var mockDbSet = promotions.BuildMockDbSet();
            _contextMock.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            return mockDbSet;
        }

        private Mock<DbSet<PromotionModifier>> SetupPromotionModifiersDbSet(List<PromotionModifier>? modifiers = null)
        {
            var list = modifiers ?? [];
            var mockDbSet = list.BuildMockDbSet();
            _contextMock.Setup(c => c.PromotionModifiers).Returns(mockDbSet.Object);
            return mockDbSet;
        }

        private static Product CreateMockProduct(Action<Product>? configure = null)
        {
            var categoryId = Guid.NewGuid();
            var optionId = Guid.NewGuid();
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Hambúrguer Especial",
                Price = 30.00m,
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "Lanches" },
                Active = true,
                ModifierGroups =
                [
                    new ModifierGroup
                {
                    Id = Guid.NewGuid(),
                    Name = "Adicionais",
                    Active = true,
                    Options =
                    [
                        new ModifierOption
                        {
                            Id = optionId,
                            Name = "Bacon",
                            AdditionalPrice = 5.00m,
                            Active = true
                        }
                    ]
                }
                ]
            };

            configure?.Invoke(product);
            return product;
        }

        private static Promotion CreateMockPromotion(Action<Promotion>? configure = null)
        {
            var baseProduct = CreateMockProduct();
            var option = baseProduct.ModifierGroups.First().Options.First();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Promoção Burger Day",
                Description = "Desconto no burger e adicionais",
                Running = true,
                BaseProductId = baseProduct.Id,
                BaseProduct = baseProduct,
                PromotionalPrice = 24.00m,
                PromotionModifiers =
                [
                    new PromotionModifier
                {
                    Id = Guid.NewGuid(),
                    ModifierOptionId = option.Id,
                    OverridePrice = 2.50m
                }
                ]
            };

            configure?.Invoke(promo);
            return promo;
        }

        [Fact]
        public async Task GetPromotionById_Should_ReturnPromotionDto_WithPromotionalPriceAndModifierOverrides_When_Exists()
        {
            var promo = CreateMockPromotion();
            SetupPromotionsDbSet([promo]);

            var result = await _promotionService.GetPromotionById(promo.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(promo.Id);
            result.Name.ShouldBe(promo.Name);
            result.BaseProduct.PromotionalPrice.ShouldBe(24.00m);
            result.BaseProduct.IsPromotional.ShouldBeTrue();
            result.BaseProduct.ModifierGroups.First().Options.First().PromotionalPrice.ShouldBe(2.50m);
            result.BaseProduct.ModifierGroups.First().Options.First().IsPromotional.ShouldBeTrue();
        }

        [Fact]
        public async Task GetPromotionById_Should_ThrowArgumentException_When_NotFound()
        {
            SetupPromotionsDbSet([]);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _promotionService.GetPromotionById(Guid.NewGuid()));

            exception.Message.ShouldContain("Promoção não encontrada.");
        }

        [Fact]
        public async Task GetActivePromotions_Should_ReturnOnlyRunningPromotions_WithCalculatedOverrides()
        {
            var activePromo = CreateMockPromotion(p => p.Running = true);
            var inactivePromo = CreateMockPromotion(p =>
            {
                p.Running = false;
                p.Name = "Promo Inativa";
            });

            SetupPromotionsDbSet([activePromo, inactivePromo]);

            var result = (await _promotionService.GetActivePromotions()).ToList();

            result.Count.ShouldBe(1);
            result.First().Id.ShouldBe(activePromo.Id);
            result.First().Name.ShouldBe(activePromo.Name);
        }

        [Fact]
        public async Task GetAllPromotions_Should_ReturnPagedResponse()
        {
            var promo1 = CreateMockPromotion(p => p.Name = "Promo 1");
            var promo2 = CreateMockPromotion(p => p.Name = "Promo 2");

            SetupPromotionsDbSet([promo1, promo2]);

            var queryParameters = new ProductQueryParameters
            {
                PageNumber = 1,
                PageSize = 10
            };

            var result = await _promotionService.GetAllPromotions(queryParameters);

            result.ShouldNotBeNull();
            result.TotalRecords.ShouldBe(2);
            result.Data.Count().ShouldBe(2);
        }

        [Fact]
        public async Task CreatePromotion_Should_PersistPromotionWithModifiers_And_ReturnCreatedDto()
        {
            var baseProduct = CreateMockProduct();
            var optionId = baseProduct.ModifierGroups.First().Options.First().Id;
            var promotions = new List<Promotion>();

            var mockDbSet = SetupPromotionsDbSet(promotions);
            SetupPromotionModifiersDbSet();

            mockDbSet
                .Setup(m => m.Add(It.IsAny<Promotion>()))
                .Callback<Promotion>(p =>
                {
                    if (p.Id == Guid.Empty) p.Id = Guid.NewGuid();
                    p.BaseProduct = baseProduct;
                    promotions.Add(p);
                });

            var dto = new PromotionRequestDto
            {
                Name = "Terça do Burger",
                Description = "Preço especial",
                BaseProductId = baseProduct.Id,
                PromotionalPrice = 22.00m,
                PromotionModifiers =
                [
                    new()
                {
                    ModifierOptionId = optionId,
                    OverridePrice = 2.00m
                }
                ]
            };

            var result = await _promotionService.CreatePromotion(dto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Terça do Burger");
            result.BaseProduct.IsPromotional.ShouldBeTrue();
            result.BaseProduct.PromotionalPrice.ShouldBe(22.00m);
            result.BaseProduct.ModifierGroups.First().Options.First().IsPromotional.ShouldBeTrue();
            result.BaseProduct.ModifierGroups.First().Options.First().PromotionalPrice.ShouldBe(2.00m);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task ToggleRunningStatus_Should_InvertRunningStatus_And_SaveChanges()
        {
            var promo = CreateMockPromotion(p => p.Running = true);
            SetupPromotionsDbSet([promo]);
            _contextMock.Setup(c => c.Promotions.FindAsync(promo.Id)).ReturnsAsync(promo);

            var newStatus = await _promotionService.ToggleRunningStatus(promo.Id);

            newStatus.ShouldBeFalse();
            promo.Running.ShouldBeFalse();

            _contextMock.Verify(c => c.Promotions.Update(promo), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task ToggleRunningStatus_Should_ThrowArgumentException_When_NotFound()
        {
            var promoId = Guid.NewGuid();
            _contextMock.Setup(c => c.Promotions.FindAsync(promoId)).ReturnsAsync((Promotion?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _promotionService.ToggleRunningStatus(promoId));

            exception.Message.ShouldContain("Promoção não encontrada.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdatePromotion_Should_UpdateFields_ReplaceModifiers_And_SaveChanges()
        {
            var promo = CreateMockPromotion();
            var optionId = promo.BaseProduct.ModifierGroups.First().Options.First().Id;

            SetupPromotionsDbSet([promo]);
            SetupPromotionModifiersDbSet(promo.PromotionModifiers.ToList());

            var updateDto = new PromotionRequestDto
            {
                Name = "Nome Atualizado",
                Description = "Nova Descrição",
                BaseProductId = promo.BaseProductId,
                PromotionalPrice = 20.00m,
                PromotionModifiers =
                [
                    new()
                {
                    ModifierOptionId = optionId,
                    OverridePrice = 1.50m
                }
                ]
            };

            var result = await _promotionService.UpdatePromotion(promo.Id, updateDto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Nome Atualizado");
            result.BaseProduct.PromotionalPrice.ShouldBe(20.00m);
            result.BaseProduct.ModifierGroups.First().Options.First().PromotionalPrice.ShouldBe(1.50m);

            _contextMock.Verify(c => c.PromotionModifiers.RemoveRange(It.IsAny<IEnumerable<PromotionModifier>>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdatePromotion_Should_ThrowArgumentException_When_NotFound()
        {
            SetupPromotionsDbSet([]);

            var dto = new PromotionRequestDto { Name = "Inexistente" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _promotionService.UpdatePromotion(Guid.NewGuid(), dto));

            exception.Message.ShouldContain("Promoção não encontrada.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeletePromotion_Should_RemovePromotion_And_ReturnTrue()
        {
            var promo = CreateMockPromotion();
            SetupPromotionsDbSet([promo]);
            _contextMock.Setup(c => c.Promotions.FindAsync(promo.Id)).ReturnsAsync(promo);

            var result = await _promotionService.DeletePromotion(promo.Id);

            result.ShouldBeTrue();
            _contextMock.Verify(c => c.Promotions.Remove(promo), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeletePromotion_Should_ThrowArgumentException_When_NotFound()
        {
            var promoId = Guid.NewGuid();
            _contextMock.Setup(c => c.Promotions.FindAsync(promoId)).ReturnsAsync((Promotion?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _promotionService.DeletePromotion(promoId));

            exception.Message.ShouldContain("Promoção não encontrada.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }
    }
}