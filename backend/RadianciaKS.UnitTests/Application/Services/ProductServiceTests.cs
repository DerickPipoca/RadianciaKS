using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;
using Xunit;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<ProductRequestDto>> _validatorMock;
        private readonly Mock<IImageStorageService> _imageStorageServiceMock;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<ProductRequestDto>>();
            _imageStorageServiceMock = new Mock<IImageStorageService>();

            // Configuração padrão do validador para passar em fluxos válidos
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _productService = new ProductService(
                _contextMock.Object,
                _validatorMock.Object,
                _imageStorageServiceMock.Object
            );
        }

        private void SetupProductsDbSet(List<Product> products)
        {
            var mockDbSet = products.BuildMockDbSet();
            _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
        }

        private void SetupCategoriesDbSet(List<Category> categories)
        {
            var mockDbSet = categories.BuildMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);
        }

        private static Product CreateMockProduct(Action<Product>? configure = null)
        {
            var categoryId = Guid.NewGuid();
            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "X-Salada Especial",
                Description = "Pão brioche, hambúrguer 160g, queijo e salada",
                Price = 28.50m,
                ImagePath = "https://cdn.radiancia.com/x-salada.png",
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "Lanches" },
                Active = true,
                ModifierGroups = new List<ModifierGroup>()
            };

            configure?.Invoke(product);
            return product;
        }

        private static void SetCreatedAt(object entity, DateTime dateTime)
        {
            var type = entity.GetType();
            while (type != null)
            {
                var prop = type.GetProperty("CreatedAt",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly);

                if (prop != null)
                {
                    var setter = prop.GetSetMethod(true);
                    if (setter != null)
                    {
                        setter.Invoke(entity, [dateTime]);
                        return;
                    }
                }
                type = type.BaseType;
            }
        }

        [Fact]
        public async Task GetProductById_Should_ReturnProductResponseDto_When_ProductExists()
        {
            // Arrange
            var product = CreateMockProduct();
            SetupProductsDbSet([product]);

            // Act
            var result = await _productService.GetProductById(product.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(product.Id);
            result.Name.ShouldBe("X-Salada Especial");
            result.Price.ShouldBe(28.50m);
        }

        [Fact]
        public async Task GetProductById_Should_ThrowArgumentException_When_ProductNotFound()
        {
            // Arrange
            SetupProductsDbSet([]);

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _productService.GetProductById(Guid.NewGuid()));

            exception.Message.ShouldContain("O Produto informado não existe.");
        }

        [Fact]
        public async Task GetAllProducts_Should_FilterByCategoryId_And_SearchTerm()
        {
            // Arrange
            var targetCategoryId = Guid.NewGuid();
            var otherCategoryId = Guid.NewGuid();

            var prod1 = CreateMockProduct(p =>
            {
                p.Name = "Suco de Laranja";
                p.CategoryId = targetCategoryId;
            });
            var prod2 = CreateMockProduct(p =>
            {
                p.Name = "Suco de Uva";
                p.CategoryId = targetCategoryId;
            });
            var prod3 = CreateMockProduct(p =>
            {
                p.Name = "Hambúrguer Suculento";
                p.CategoryId = otherCategoryId;
            });

            SetupProductsDbSet([prod1, prod2, prod3]);

            var query = new ProductQueryParameters
            {
                CategoryId = targetCategoryId,
                SearchTerm = "laranja",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _productService.GetAllProducts(query);

            // Assert
            result.ShouldNotBeNull();
            result.TotalRecords.ShouldBe(1);
            result.Data.First().Name.ShouldBe("Suco de Laranja");
        }

        [Fact]
        public async Task GetAllProducts_Should_SortByPrice_And_Paginate()
        {
            // Arrange
            var p1 = CreateMockProduct(p => { p.Name = "Item Barato"; p.Price = 10.00m; });
            var p2 = CreateMockProduct(p => { p.Name = "Item Médio"; p.Price = 25.00m; });
            var p3 = CreateMockProduct(p => { p.Name = "Item Caro"; p.Price = 50.00m; });

            SetupProductsDbSet([p1, p2, p3]);

            var query = new ProductQueryParameters
            {
                SortBy = "price",
                IsDescending = true,
                PageNumber = 1,
                PageSize = 2
            };

            var result = await _productService.GetAllProducts(query);

            result.TotalRecords.ShouldBe(3);
            result.Data.Count().ShouldBe(2);
            result.Data.First().Price.ShouldBe(50.00m);
            result.Data.Skip(1).First().Price.ShouldBe(25.00m);
        }

        [Fact]
        public async Task CreateProduct_Should_PersistProduct_And_ReturnDto_When_Valid()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = new Category { Id = categoryId, Name = "Bebidas" };

            SetupProductsDbSet([]);
            SetupCategoriesDbSet([category]);
            _contextMock.Setup(c => c.Categories.FindAsync(categoryId)).ReturnsAsync(category);

            var dto = new ProductRequestDto
            {
                Name = "Refrigerante Cola",
                Description = "Lata 350ml",
                Price = 6.50m,
                CategoryId = categoryId,
                ImagePath = "https://cdn.radiancia.com/coca.png"
            };

            // Act
            var result = await _productService.CreateProduct(dto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe("Refrigerante Cola");
            result.Price.ShouldBe(6.50m);

            _contextMock.Verify(c => c.Products.Add(It.Is<Product>(p =>
                p.Name == "Refrigerante Cola" &&
                p.CategoryId == categoryId)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreateProduct_Should_ThrowArgumentException_When_CategoryDoesNotExist()
        {
            var invalidCategoryId = Guid.NewGuid();
            SetupCategoriesDbSet([]);
            _contextMock.Setup(c => c.Categories.FindAsync(invalidCategoryId)).ReturnsAsync((Category?)null);

            var dto = new ProductRequestDto
            {
                Name = "Produto Sem Categoria",
                Price = 15.00m,
                CategoryId = invalidCategoryId
            };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _productService.CreateProduct(dto));

            exception.Message.ShouldContain("A Categoria informada não existe.");
            _contextMock.Verify(c => c.Products.Add(It.IsAny<Product>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateProduct_Should_UpdateProperties_And_SaveChanges_When_Valid()
        {
            var product = CreateMockProduct(p =>
            {
                p.Name = "Nome Antigo";
                p.Price = 20.00m;
            });

            var newCategoryId = Guid.NewGuid();
            var newCategory = new Category { Id = newCategoryId, Name = "Sobremesas" };

            SetupProductsDbSet([product]);
            _contextMock.Setup(c => c.Products.FindAsync(product.Id)).ReturnsAsync(product);
            _contextMock.Setup(c => c.Categories.FindAsync(newCategoryId)).ReturnsAsync(newCategory);

            var updateDto = new ProductRequestDto
            {
                Name = "Nome Atualizado",
                Description = "Nova descrição",
                Price = 35.00m,
                CategoryId = newCategoryId,
                ImagePath = "https://cdn.radiancia.com/novo.png"
            };

            var result = await _productService.UpdateProduct(product.Id, updateDto);

            result.ShouldNotBeNull();
            product.Name.ShouldBe("Nome Atualizado");
            product.Price.ShouldBe(35.00m);
            product.CategoryId.ShouldBe(newCategoryId);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateProduct_Should_ThrowArgumentException_When_ProductNotFound()
        {
            var productId = Guid.NewGuid();
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync((Product?)null);

            var updateDto = new ProductRequestDto { Name = "Teste", CategoryId = Guid.NewGuid() };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _productService.UpdateProduct(productId, updateDto));

            exception.Message.ShouldContain("O Produto informado não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteProduct_Should_SetSoftDeleteActiveFalse_And_SaveChanges()
        {
            var product = CreateMockProduct(p => p.Active = true);
            SetupProductsDbSet([product]);
            _contextMock.Setup(c => c.Products.FindAsync(product.Id)).ReturnsAsync(product);

            await _productService.DeleteProduct(product.Id);

            product.Active.ShouldBeFalse();
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteProduct_Should_ThrowArgumentException_When_ProductNotFound()
        {
            var productId = Guid.NewGuid();
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync((Product?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _productService.DeleteProduct(productId));

            exception.Message.ShouldContain("O Produto informado não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DuplicateProduct_Should_CloneProductWithModifierGroupsAndOptions()
        {
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var originalProduct = CreateMockProduct(p =>
            {
                p.TenantId = tenantId;
                p.Name = "Combo Burger";
                p.Price = 45.00m;
                p.CategoryId = categoryId;
                p.ModifierGroups =
                [
                    new ModifierGroup
                {
                    Name = "Escolha seu Molho",
                    MinChoices = 0,
                    MaxChoices = 2,
                    TenantId = tenantId,
                    Active = true,
                    Options =
                    [
                        new ModifierOption
                        {
                            Name = "Maionese Verde",
                            AdditionalPrice = 3.00m,
                            TenantId = tenantId,
                            Active = true
                        }
                    ]
                }
                ];
            });

            SetupProductsDbSet([originalProduct]);

            var result = await _productService.DuplicateProduct(originalProduct.Id);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Combo Burger - Cópia");

            _contextMock.Verify(c => c.Products.Add(It.Is<Product>(p =>
                p.Name == "Combo Burger - Cópia" &&
                p.Price == 45.00m &&
                p.CategoryId == categoryId &&
                p.ModifierGroups.Count == 1 &&
                p.ModifierGroups.First().Options.Count == 1 &&
                p.ModifierGroups.First().Options.First().Name == "Maionese Verde")), Times.Once);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DuplicateProduct_Should_ThrowArgumentException_When_ProductNotFound()
        {
            SetupProductsDbSet([]);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _productService.DuplicateProduct(Guid.NewGuid()));

            exception.Message.ShouldContain("Produto não encontrado.");
            _contextMock.Verify(c => c.Products.Add(It.IsAny<Product>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UploadImage_Should_CallImageStorageService_And_ReturnUrl()
        {
            var fileMock = new Mock<IFormFile>();
            const string expectedUrl = "https://cdn.radiancia.com/products/burger.webp";

            _imageStorageServiceMock
                .Setup(s => s.UploadImageAsync(fileMock.Object, "products"))
                .ReturnsAsync(expectedUrl);

            var result = await _productService.UploadImage(fileMock.Object);

            result.ShouldBe(expectedUrl);
            _imageStorageServiceMock.Verify(s => s.UploadImageAsync(fileMock.Object, "products"), Times.Once);
        }
    }
}