using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class ModifierServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<ModifierOptionRequestDto>> _validatorMock;
        private readonly ModifierService _modifierService;

        public ModifierServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<ModifierOptionRequestDto>>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _modifierService = new ModifierService(
                _contextMock.Object,
                _validatorMock.Object
            );
        }

        private Mock<DbSet<ModifierGroup>> SetupModifierGroupsDbSet(List<ModifierGroup> groups)
        {
            var mockDbSet = groups.BuildMockDbSet();
            _contextMock.Setup(c => c.ModifierGroups).Returns(mockDbSet.Object);
            return mockDbSet;
        }

        private Mock<DbSet<ModifierOption>> SetupModifierOptionsDbSet(List<ModifierOption> options)
        {
            var mockDbSet = options.BuildMockDbSet();
            _contextMock.Setup(c => c.ModifierOptions).Returns(mockDbSet.Object);
            return mockDbSet;
        }

        private Mock<DbSet<Product>> SetupProductsDbSet(List<Product> products)
        {
            var mockDbSet = products.BuildMockDbSet();
            _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
            return mockDbSet;
        }

        private static ModifierGroup CreateMockGroup(Action<ModifierGroup>? configure = null)
        {
            var group = new ModifierGroup
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Name = "Ponto da Carne",
                MinChoices = 1,
                MaxChoices = 1,
                Priority = 1,
                Options = new List<ModifierOption>()
            };

            configure?.Invoke(group);
            return group;
        }

        private static ModifierOption CreateMockOption(Action<ModifierOption>? configure = null)
        {
            var option = new ModifierOption
            {
                Id = Guid.NewGuid(),
                ModifierGroupId = Guid.NewGuid(),
                Name = "Ao Ponto",
                AdditionalPrice = 0.00m,
                Description = "Carne rosada no centro",
                ImagePath = "https://cdn.radiancia.com/ao-ponto.png"
            };

            configure?.Invoke(option);
            return option;
        }

        [Fact]
        public async Task CreateGroupAsync_Should_PersistGroup_And_ReturnDto_When_ProductExists()
        {
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Burger Artesanal" };

            var mockProductsDbSet = SetupProductsDbSet([product]);
            mockProductsDbSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            var groups = new List<ModifierGroup>();
            SetupModifierGroupsDbSet(groups);

            var dto = new ModifierGroupRequestDto
            {
                ProductId = productId,
                Name = "Adicionais Extras",
                MinChoices = 0,
                MaxChoices = 3,
                Priority = 2
            };

            var result = await _modifierService.CreateGroupAsync(dto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Adicionais Extras");
            result.MinChoices.ShouldBe(0);
            result.MaxChoices.ShouldBe(3);
            result.Priority.ShouldBe(2);

            _contextMock.Verify(c => c.ModifierGroups.Add(It.Is<ModifierGroup>(g =>
                g.ProductId == productId &&
                g.Name == "Adicionais Extras")), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreateGroupAsync_Should_ThrowArgumentException_When_ProductNotFound()
        {
            var productId = Guid.NewGuid();
            var mockProductsDbSet = SetupProductsDbSet([]);
            mockProductsDbSet.Setup(m => m.FindAsync(productId)).ReturnsAsync((Product?)null);

            var dto = new ModifierGroupRequestDto { ProductId = productId, Name = "Molhos" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.CreateGroupAsync(dto));

            exception.Message.ShouldContain("Produto não encontrado.");
            _contextMock.Verify(c => c.ModifierGroups.Add(It.IsAny<ModifierGroup>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task GetGroupsByProductAsync_Should_ReturnGroupsOrderedByPriority_WithIncludedOptions()
        {
            var productId = Guid.NewGuid();

            var option1 = CreateMockOption(o => { o.Name = "Bacon"; o.AdditionalPrice = 4.00m; });
            var group2 = CreateMockGroup(g =>
            {
                g.ProductId = productId;
                g.Priority = 2;
                g.Name = "Adicionais";
                g.Options = [option1];
            });

            var group1 = CreateMockGroup(g =>
            {
                g.ProductId = productId;
                g.Priority = 1;
                g.Name = "Ponto";
                g.Options = [];
            });

            var otherGroup = CreateMockGroup(g => g.ProductId = Guid.NewGuid());

            SetupModifierGroupsDbSet([group2, group1, otherGroup]);

            var result = (await _modifierService.GetGroupsByProductAsync(productId)).ToList();

            result.Count.ShouldBe(2);
            result[0].Name.ShouldBe("Ponto");
            result[0].Priority.ShouldBe(1);
            result[1].Name.ShouldBe("Adicionais");
            result[1].Priority.ShouldBe(2);
            result[1].Options.Count.ShouldBe(1);
            result[1].Options.First().Name.ShouldBe("Bacon");
        }

        [Fact]
        public async Task UpdateGroupAsync_Should_UpdateGroupProperties_And_ReturnMappedDto_When_GroupExists()
        {
            var group = CreateMockGroup(g =>
            {
                g.Name = "Nome Antigo";
                g.MinChoices = 0;
                g.MaxChoices = 1;
                g.Priority = 1;
            });

            var mockDbSet = SetupModifierGroupsDbSet([group]);
            mockDbSet.Setup(m => m.FindAsync(group.Id)).ReturnsAsync(group);

            var updateDto = new ModifierGroupRequestDto
            {
                Name = "Nome Novo",
                MinChoices = 1,
                MaxChoices = 2,
                Priority = 3
            };

            var result = await _modifierService.UpdateGroupAsync(group.Id, updateDto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Nome Novo");
            result.MinChoices.ShouldBe(1);
            result.MaxChoices.ShouldBe(2);
            result.Priority.ShouldBe(3);

            _contextMock.Verify(c => c.ModifierGroups.Update(group), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateGroupAsync_Should_ThrowArgumentException_When_GroupNotFound()
        {
            var groupId = Guid.NewGuid();
            var mockDbSet = SetupModifierGroupsDbSet([]);
            mockDbSet.Setup(m => m.FindAsync(groupId)).ReturnsAsync((ModifierGroup?)null);

            var dto = new ModifierGroupRequestDto { Name = "Inexistente" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.UpdateGroupAsync(groupId, dto));

            exception.Message.ShouldContain("Grupo não encontrado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteGroupAsync_Should_RemoveGroup_And_SaveChanges_When_GroupExists()
        {
            var group = CreateMockGroup();
            var mockDbSet = SetupModifierGroupsDbSet([group]);
            mockDbSet.Setup(m => m.FindAsync(group.Id)).ReturnsAsync(group);

            await _modifierService.DeleteGroupAsync(group.Id);

            _contextMock.Verify(c => c.ModifierGroups.Remove(group), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteGroupAsync_Should_ThrowArgumentException_When_GroupNotFound()
        {
            var groupId = Guid.NewGuid();
            var mockDbSet = SetupModifierGroupsDbSet([]);
            mockDbSet.Setup(m => m.FindAsync(groupId)).ReturnsAsync((ModifierGroup?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.DeleteGroupAsync(groupId));

            exception.Message.ShouldContain("Grupo não encontrado.");
            _contextMock.Verify(c => c.ModifierGroups.Remove(It.IsAny<ModifierGroup>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task AddOptionToGroupAsync_Should_PersistOption_And_ReturnDto_When_GroupExists()
        {
            var group = CreateMockGroup();
            var mockGroupsDbSet = SetupModifierGroupsDbSet([group]);
            mockGroupsDbSet.Setup(m => m.FindAsync(group.Id)).ReturnsAsync(group);

            var options = new List<ModifierOption>();
            SetupModifierOptionsDbSet(options);

            var dto = new ModifierOptionRequestDto
            {
                Name = "Queijo Cheddar",
                AdditionalPrice = 3.50m,
                Description = "Fatia extra",
                ImagePath = "https://cdn.radiancia.com/cheddar.png"
            };

            var result = await _modifierService.AddOptionToGroupAsync(group.Id, dto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Queijo Cheddar");
            result.AdditionalPrice.ShouldBe(3.50m);

            _contextMock.Verify(c => c.ModifierOptions.Add(It.Is<ModifierOption>(o =>
                o.ModifierGroupId == group.Id &&
                o.Name == "Queijo Cheddar" &&
                o.AdditionalPrice == 3.50m)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task AddOptionToGroupAsync_Should_ThrowArgumentException_When_GroupNotFound()
        {
            var groupId = Guid.NewGuid();
            var mockGroupsDbSet = SetupModifierGroupsDbSet([]);
            mockGroupsDbSet.Setup(m => m.FindAsync(groupId)).ReturnsAsync((ModifierGroup?)null);

            var dto = new ModifierOptionRequestDto { Name = "Opção" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.AddOptionToGroupAsync(groupId, dto));

            exception.Message.ShouldContain("Grupo de modificadores não encontrado.");
            _contextMock.Verify(c => c.ModifierOptions.Add(It.IsAny<ModifierOption>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateOptionAsync_Should_UpdateOptionProperties_And_ReturnDto_When_OptionExists()
        {
            var option = CreateMockOption(o =>
            {
                o.Name = "Nome Antigo";
                o.AdditionalPrice = 2.00m;
            });

            var mockDbSet = SetupModifierOptionsDbSet([option]);
            mockDbSet.Setup(m => m.FindAsync(option.Id)).ReturnsAsync(option);

            var updateDto = new ModifierOptionRequestDto
            {
                Name = "Nome Atualizado",
                AdditionalPrice = 4.50m,
                Description = "Nova Descrição",
                ImagePath = "https://cdn.radiancia.com/novo.png"
            };

            var result = await _modifierService.UpdateOptionAsync(option.Id, updateDto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Nome Atualizado");
            result.AdditionalPrice.ShouldBe(4.50m);
            result.Description.ShouldBe("Nova Descrição");
            result.ImagePath.ShouldBe("https://cdn.radiancia.com/novo.png");

            option.Name.ShouldBe("Nome Atualizado");
            option.AdditionalPrice.ShouldBe(4.50m);

            _contextMock.Verify(c => c.ModifierOptions.Update(option), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateOptionAsync_Should_ThrowArgumentException_When_OptionNotFound()
        {
            var optionId = Guid.NewGuid();
            var mockDbSet = SetupModifierOptionsDbSet([]);
            mockDbSet.Setup(m => m.FindAsync(optionId)).ReturnsAsync((ModifierOption?)null);

            var dto = new ModifierOptionRequestDto { Name = "Inexistente" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.UpdateOptionAsync(optionId, dto));

            exception.Message.ShouldContain("Opção não encontrada.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteOptionAsync_Should_RemoveOption_And_SaveChanges_When_OptionExists()
        {
            var option = CreateMockOption();
            var mockDbSet = SetupModifierOptionsDbSet([option]);
            mockDbSet.Setup(m => m.FindAsync(option.Id)).ReturnsAsync(option);

            await _modifierService.DeleteOptionAsync(option.Id);

            _contextMock.Verify(c => c.ModifierOptions.Remove(option), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteOptionAsync_Should_ThrowArgumentException_When_OptionNotFound()
        {
            var optionId = Guid.NewGuid();
            var mockDbSet = SetupModifierOptionsDbSet([]);
            mockDbSet.Setup(m => m.FindAsync(optionId)).ReturnsAsync((ModifierOption?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _modifierService.DeleteOptionAsync(optionId));

            exception.Message.ShouldContain("Opção não encontrada.");
            _contextMock.Verify(c => c.ModifierOptions.Remove(It.IsAny<ModifierOption>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }
    }
}