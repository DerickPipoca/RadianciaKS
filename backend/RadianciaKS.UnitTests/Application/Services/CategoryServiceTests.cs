using FluentValidation;
using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<CategoryRequestDto>> _validatorMock;
        private readonly Mock<IImageStorageService> _imageStorageServiceMock;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<CategoryRequestDto>>();
            _imageStorageServiceMock = new Mock<IImageStorageService>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _categoryService = new CategoryService(
                _contextMock.Object,
                _validatorMock.Object,
                _imageStorageServiceMock.Object
            );
        }

        private void SetupCategoriesDbSet(List<Category> categories)
        {
            var mockDbSet = categories.BuildMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);
        }

        private static Category CreateMockCategory(Action<Category>? configure = null)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Bebidas",
                ImagePath = "https://cdn.radiancia.com/bebidas.png",
                Priority = 1,
                Active = true
            };

            configure?.Invoke(category);
            return category;
        }

        [Fact]
        public async Task GetCategoryById_Should_ReturnCategoryResponseDto_When_CategoryExists()
        {
            var category = CreateMockCategory();
            SetupCategoriesDbSet([category]);
            _contextMock.Setup(c => c.Categories.FindAsync(category.Id)).ReturnsAsync(category);

            var result = await _categoryService.GetCategoryById(category.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(category.Id);
            result.Name.ShouldBe("Bebidas");
            result.Priority.ShouldBe(1);
        }

        [Fact]
        public async Task GetCategoryById_Should_ThrowArgumentException_When_CategoryNotFound()
        {
            var id = Guid.NewGuid();
            SetupCategoriesDbSet([]);
            _contextMock.Setup(c => c.Categories.FindAsync(id)).ReturnsAsync((Category?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _categoryService.GetCategoryById(id));

            exception.Message.ShouldContain("A categoria informada não existe.");
        }

        [Fact]
        public async Task GetAllCategories_Should_ReturnAllCategoriesMapped()
        {
            var cat1 = CreateMockCategory(c => { c.Name = "Lanches"; c.Priority = 1; });
            var cat2 = CreateMockCategory(c => { c.Name = "Sobremesas"; c.Priority = 2; });

            SetupCategoriesDbSet([cat1, cat2]);

            var result = (await _categoryService.GetAllCategories()).ToList();

            result.Count.ShouldBe(2);
            result.ShouldContain(c => c.Name == "Lanches");
            result.ShouldContain(c => c.Name == "Sobremesas");
        }

        [Fact]
        public async Task CreateCategory_Should_PersistCategory_And_ReturnDto_When_Valid()
        {
            SetupCategoriesDbSet([]);

            var dto = new CategoryRequestDto
            {
                Name = "Pizzas",
                ImagePath = "https://cdn.radiancia.com/pizzas.png",
                Priority = 3
            };

            var result = await _categoryService.CreateCategory(dto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Pizzas");
            result.Priority.ShouldBe(3);

            _contextMock.Verify(c => c.Categories.Add(It.Is<Category>(cat =>
                cat.Name == "Pizzas" &&
                cat.Priority == 3)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_Should_UpdateProperties_And_SaveChanges_When_Valid()
        {
            var category = CreateMockCategory(c =>
            {
                c.Name = "Nome Antigo";
                c.Priority = 1;
            });

            SetupCategoriesDbSet([category]);
            _contextMock.Setup(c => c.Categories.FindAsync(category.Id)).ReturnsAsync(category);

            var updateDto = new CategoryRequestDto
            {
                Name = "Nome Atualizado",
                ImagePath = "https://cdn.radiancia.com/novo.png",
                Priority = 5
            };

            var result = await _categoryService.UpdateCategory(category.Id, updateDto);

            result.ShouldNotBeNull();
            category.Name.ShouldBe("Nome Atualizado");
            category.Priority.ShouldBe(5);
            category.ImagePath.ShouldBe("https://cdn.radiancia.com/novo.png");

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_Should_ThrowArgumentException_When_CategoryNotFound()
        {
            var categoryId = Guid.NewGuid();
            _contextMock.Setup(c => c.Categories.FindAsync(categoryId)).ReturnsAsync((Category?)null);

            var updateDto = new CategoryRequestDto { Name = "Teste", Priority = 1 };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _categoryService.UpdateCategory(categoryId, updateDto));

            exception.Message.ShouldContain("A categoria informada não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteCategory_Should_SetSoftDeleteActiveFalse_And_SaveChanges()
        {
            var category = CreateMockCategory(c => c.Active = true);
            SetupCategoriesDbSet([category]);
            _contextMock.Setup(c => c.Categories.FindAsync(category.Id)).ReturnsAsync(category);

            await _categoryService.DeleteCategory(category.Id);

            category.Active.ShouldBeFalse();
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteCategory_Should_ThrowArgumentException_When_CategoryNotFound()
        {
            var categoryId = Guid.NewGuid();
            _contextMock.Setup(c => c.Categories.FindAsync(categoryId)).ReturnsAsync((Category?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _categoryService.DeleteCategory(categoryId));

            exception.Message.ShouldContain("A categoria informada não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UploadImage_Should_CallImageStorageService_And_ReturnUrl()
        {
            var fileMock = new Mock<IFormFile>();
            const string expectedUrl = "https://cdn.radiancia.com/categories/categoria-teste.webp";

            _imageStorageServiceMock
                .Setup(s => s.UploadImageAsync(fileMock.Object, "categories"))
                .ReturnsAsync(expectedUrl);

            var result = await _categoryService.UploadImage(fileMock.Object);

            result.ShouldBe(expectedUrl);
            _imageStorageServiceMock.Verify(s => s.UploadImageAsync(fileMock.Object, "categories"), Times.Once);
        }
    }
}