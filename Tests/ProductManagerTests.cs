using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Interfaces.IRepository;
using Managers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Models.DTOs;
using DataAccess.Entities;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ProductManagerTests
    {
        private Mock<IGenericRepository<Product>> _repoMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<ProductManager>> _loggerMock;
        private  IMemoryCache _memoryCache;
        private ProductManager _manager;
        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IGenericRepository<Product>>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ProductManager>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _manager = new ProductManager(_repoMock.Object, _loggerMock.Object, _mapperMock.Object, _memoryCache);
        }
            [TearDown]
            public void TearDown()
            {
                _memoryCache?.Dispose();       
                _memoryCache = null;
                _manager = null;
            }

        
        [Test]
            public async Task GetAllAsync_WhenCacheIsEmpty_FetchesFromRepositoryAndCaches()
            {
                var products = new List<Product> { new Product { ProductId = 1, Name = "Laptop" } };
                _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
                _mapperMock.Setup(m => m.Map<IList<ProductDto>>(products))
                           .Returns(new List<ProductDto> { new ProductDto { ProductId = 1, Name = "Laptop" } });

                var result = await _manager.GetAllAsync();

                Assert.IsTrue(result.Success);
                Assert.That(result.Data?.Count, Is.EqualTo(1));
                Assert.That(result.Data?[0].Name, Is.EqualTo("Laptop"));

                Assert.IsTrue(_memoryCache.TryGetValue("all_products", out IList<ProductDto> cached));
                Assert.That(cached.Count, Is.EqualTo(1));
            }

            [Test]
            public async Task GetAllAsync_WhenCacheHasData_ReturnsFromCache()
            {
                var cachedDtos = new List<ProductDto> { new ProductDto { ProductId = 99, Name = "CachedProduct" } };
                _memoryCache.Set("all_products", cachedDtos);

                var result = await _manager.GetAllAsync();

                Assert.IsTrue(result.Success);
                Assert.That(result.Data?[0].Name, Is.EqualTo("CachedProduct"));

                _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
            }

            [Test]
            public async Task GetByIdAsync_WhenCacheIsEmpty_FetchesFromRepositoryAndCaches()
            {
                // Arrange
                var product = new Product { ProductId = 1, Name = "Phone" };
                _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
                _mapperMock.Setup(m => m.Map<ProductDto>(product))
                           .Returns(new ProductDto { ProductId = 1, Name = "Phone" });

                var result = await _manager.GetByIdAsync(1);

                Assert.IsTrue(result.Success);
                Assert.That(result.Data?.Name, Is.EqualTo("Phone"));

                Assert.IsTrue(_memoryCache.TryGetValue("product_1", out ProductDto cached));
                Assert.That(cached.Name, Is.EqualTo("Phone"));
            }

            [Test]
            public async Task GetByIdAsync_WhenCacheHasData_ReturnsFromCache()
            {
                var cachedDto = new ProductDto { ProductId = 1, Name = "CachedPhone" };
                _memoryCache.Set("product_1", cachedDto);

                var result = await _manager.GetByIdAsync(1);

                Assert.IsTrue(result.Success);
                Assert.That(result.Data?.Name, Is.EqualTo("CachedPhone"));

                _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            }

            [Test]
            public async Task GetByIdAsync_WhenProductNotFound_ReturnsFailure()
            {
                
                _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Product)null);

                var result = await _manager.GetByIdAsync(1);
                Assert.IsFalse(result.Success);
                Assert.That(result.Message, Is.EqualTo("Product not found"));
            }
        
        [Test]
        public async Task AddAsync_AddsProductSuccessfully()
        {
            var productdto = new CreateProduct { Name = "Car" };
            _mapperMock.Setup(m => m.Map<Product>(productdto)).Returns(new Product { ProductId = 1, Name = "Car" });
            _repoMock.Setup(r => r.AddAsync(new Product { ProductId = 1, Name = "Car" })).Returns(Task.CompletedTask);
            var result = await _manager.AddAsync(productdto);
            Assert.IsTrue(result.Success);
            Assert.That(true, "Product added successfully", result.Message);
        }
        [Test]
        public async Task UpdateAsync_ProductExists_UpdatedSuccessfully()
        {
            var productdto = new UpdateProduct { ProductId = 1, Name = "NewName" };
            var product = new Product { ProductId = 1, Name = "OldName" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            _mapperMock.Setup(m => m.Map(productdto, product));
            _repoMock.Setup(r => r.UpdateAsync(product)).Returns(Task.CompletedTask);

            var result = await _manager.UpdateAsync(productdto);
            Assert.IsTrue(result.Success);
            Assert.That(true, "Product updated successfully", result.Message);

        }
        public async Task DeleteAsync_DeletesProducts()
        {
            var product = new Product { ProductId = 1, Name = "Laptop" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            _repoMock.Setup(r => r.DeleteAsync(product)).Returns(Task.CompletedTask);
            var result = await _manager.DeleteAsync(1);
            Assert.IsTrue(result.Success);
            Assert.That(true,"Product Deleted Successfully",result.Message);
        }
        [Test]
        public async Task DeleteAsync_ProductNotFound_ReturnsFailure()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Product)null);
            var result = await _manager.DeleteAsync(1);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Product not found", result.Message);
        }
    }


}
