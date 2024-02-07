using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sales.API.Controllers;
using Sales.API.Data;
using Sales.Share.DTOs;
using Sales.Share.entities;

namespace Sales.UnitTest.Controllers
{
    [TestClass]
    public class CategoriesControllerTests
    {
        private readonly DbContextOptions<DataContext> _options;
        public CategoriesControllerTests()
        {
            _options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [TestMethod]
        public async Task GetAsync_ReturnOkResult()
        {
            // Arrange
            using var context = new DataContext(_options);
            var controller = new CategoriesController(context);
            var pagination = new PaginationDTO() { Filter = "Some " };

            // Act
            var result = await controller.GetAsync(pagination) as OkObjectResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task GetPagesAsync_ReturnsOkResult()
        {
            // Arrange
            using var context = new DataContext(_options);
            var controller = new CategoriesController(context);
            var pagination = new PaginationDTO() { Filter = "Some " };

            // Act
            var result = await controller.GetPagesAsync(pagination) as OkObjectResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task GetAsync_ReturnsNotFoundWhenCategoryNotFound()
        {
            // Arrange
            using var context = new DataContext(_options);
            var controller = new CategoriesController(context);

            // Act
            var result = await controller.GetAsync(1) as NotFoundResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task GetAsync_ReturnsRecord()
        {
            // Arrange
            using var context = new DataContext(_options);
            var category = new Category() { Id = 1, Name = "juan"};
            await context.Categories.AddAsync(category);
            await context.SaveChangesAsync();

            var controller = new CategoriesController(context);
            // Act
            var result = await controller.GetAsync(category.Id) as OkObjectResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);


            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task PostAsync_ReturnsOkResult()
        {
            // Arrange
            using var context = new DataContext(_options);
            var category = new Category() { Id = 1, Name = "juan" };

            var controller = new CategoriesController(context);
            // Act
            var result = await controller.PostAsync(category) as OkObjectResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            var CategoryAdded = result.Value as Category;
            Assert.AreEqual(category.Name, CategoryAdded!.Name);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task PostAsync_ReturnsBadRequest()
        {
            // Arrange
            using var context = new DataContext(_options);
            var category = new Category() { Id = 1, Name = "juan" };
            await context.Categories.AddAsync(category);
            await context.SaveChangesAsync();

            var controller = new CategoriesController(context);
            // Act
            var result = await controller.PostAsync(category) as BadRequestObjectResult;
            var msgResponse = result!.Value as string;

            // Assert 
            Assert.IsNotNull(result);
            Assert.IsNotNull(msgResponse);
            Assert.AreEqual(400, result.StatusCode);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task PutAsync_ReturnsBadRequest()
        {
            // Arrange
            using var context = new DataContext(_options);
            var category = new Category() { Id = 1, Name = "juan" };
            var categoryForUpdate = new Category() { Id = 2, Name = "juan" };
            await context.Categories.AddAsync(category);
            await context.SaveChangesAsync();
            var controller = new CategoriesController(context);

            // Act
            var result = await controller.PutAsync(categoryForUpdate) as BadRequestObjectResult;
            var msgResponse = result!.Value as string;

            // Assert 
            Assert.IsNotNull(result);
            Assert.IsNotNull(msgResponse);
            Assert.AreEqual(400, result.StatusCode);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        [TestMethod]
        public async Task PutAsync_ReturnsOkResult()
        {
            // Arrange
            using var context = new DataContext(_options);
            var category = new Category() { Id = 1, Name = "juan" };
            var categoryForUpdate = new Category() { Id = 1, Name = "juan" };

            await context.Categories.AddAsync(category);
            await context.SaveChangesAsync();

            var controller = new CategoriesController(context);

            // Act
            var result = await controller.PutAsync(categoryForUpdate) as OkObjectResult;

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            var CategoryUpdated = result.Value as Category;
            Assert.AreEqual(categoryForUpdate.Name, CategoryUpdated!.Name);

            // Clean Up (if needed)
            context.Database.EnsureDeleted();
            context.Dispose();
        }
    }
}
