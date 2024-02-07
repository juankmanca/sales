using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sales.API.Controllers;
using Sales.API.Data;
using Sales.API.Helpers;
using Sales.Share.DTOs;
using Sales.Share.entities;
using Sales.Share.Enums;
using System.Security.Claims;

namespace Sales.UnitTest.Controllers
{
    [TestClass]
    public class TemporalSaleControllerTests
    {
        private DataContext _mockDbContext = null!;
        private TemporalSalesController _controller = null!;
        private Mock<IUserHelper> _userHelperMock = null!;

        [TestInitialize]
        public void Init()
        {
            _userHelperMock = new Mock<IUserHelper>();

            //Setting up InMemory database
            var _options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _mockDbContext = new DataContext(_options);
            _controller = new TemporalSalesController(_mockDbContext, _userHelperMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mockDbContext.Database.EnsureDeleted();
            _mockDbContext.Dispose();
        }


        [TestMethod]
        public async Task Post_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Post(new TemporalSaleDTO());

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));

        }

        [TestMethod]
        public async Task Post_ReturnsUserNotFound()
        {
            //Assert
            var product = new Product()
            {
                Id = 1,
                Name = "Test",
                Description = "Test",
                Price = 12,
                Stock = 20,
            };
            _mockDbContext.Products.Add(product);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Post(new TemporalSaleDTO() { Id = 1, ProductId = 1 });

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));

        }


        [TestMethod]
        public async Task Post_ReturnsOk()
        {
            //Arrange

            //User Config
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            var product = new Product()
            {
                Id = 1,
                Name = "Test",
                Description = "Test",
                Price = 12,
                Stock = 20,
            };
            var temporalSale = new TemporalSaleDTO()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test"
            };

            _mockDbContext.Products.Add(product);
            await _mockDbContext.SaveChangesAsync();
            _userHelperMock.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);


            // Act
            var result = await _controller.Post(temporalSale);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var response = result as OkObjectResult;
            Assert.IsNotNull(response);
            var tmpSale = response.Value! as TemporalSaleDTO;
            Assert.IsNotNull(tmpSale);
            Assert.AreEqual(temporalSale.ProductId, tmpSale.ProductId);
            _userHelperMock.Verify(x => x.GetUserAsync(userName), Times.Once());

        }

        [TestMethod]
        public async Task Get_ReturnsOkResult_CountRecords()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var userName = "test@example.com";
            var user = new User { Id = id, UserName = userName, Email= userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            _mockDbContext.Users.Add(user);
            await _mockDbContext.SaveChangesAsync();

            var product = new Product()
            {
                Id = 1,
                Name = "Test",
                Description = "Test",
                Price = 12,
                Stock = 20,
            };

            _mockDbContext.Products.Add(product);
            await _mockDbContext.SaveChangesAsync();

            var temporalSale = new TemporalSale()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test",
                UserId = user.Id,
            };

            _mockDbContext.TemporalSales.Add(temporalSale);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var response = result as OkObjectResult;
            Assert.IsNotNull(response);
            var tmpSales = response.Value! as List<TemporalSale>;
            Assert.IsNotNull(tmpSales);
            Assert.AreEqual(1, tmpSales.Count());
        }

        [TestMethod]
        public async Task Get_ReturnsOkResult_ById()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var userName = "test@example.com";
            var user = new User { Id = id, UserName = userName, Email = userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            _mockDbContext.Users.Add(user);
            await _mockDbContext.SaveChangesAsync();

            var product = new Product()
            {
                Id = 1,
                Name = "Test",
                Description = "Test",
                Price = 12,
                Stock = 20,
            };

            _mockDbContext.Products.Add(product);
            await _mockDbContext.SaveChangesAsync();

            var temporalSale = new TemporalSale()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test",
                UserId = user.Id,
            };

            _mockDbContext.TemporalSales.Add(temporalSale);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Get(temporalSale.Id);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var response = result as OkObjectResult;
            Assert.IsNotNull(response);
            var tmpSales = response.Value! as TemporalSale;
            Assert.IsNotNull(tmpSales);
            Assert.IsInstanceOfType(tmpSales, typeof(TemporalSale));

        }

        [TestMethod]
        public async Task GetCount_ReturnsOkResult_CountRecords()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var userName = "test@example.com";
            var user = new User { Id = id, UserName = userName, Email = userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            _mockDbContext.Users.Add(user);
            await _mockDbContext.SaveChangesAsync();

            var temporalSale = new TemporalSale()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test",
                UserId = user.Id,
            };

            _mockDbContext.TemporalSales.Add(temporalSale);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetCount();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var response = result as OkObjectResult;
            Assert.IsNotNull(response);
            var tmpSalesCount = (float)response.Value;
            Assert.IsNotNull(tmpSalesCount);
            Assert.AreEqual(temporalSale.Quantity, tmpSalesCount);
        }

        [TestMethod]
        public async Task Put_RetunsNotFound()
        {
            // Act
            var result = await _controller.Put(new TemporalSaleDTO() { Id = 1});

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));

        }

        [TestMethod]
        public async Task Put_ReturnsOk()
        {
            //Arrange 
            var userName = "test@example.com";
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = userName, Email = userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            _mockDbContext.Users.Add(user);
            await _mockDbContext.SaveChangesAsync();

            var temporalSale = new TemporalSale()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test",
                UserId = user.Id,
            };

            _mockDbContext.TemporalSales.Add(temporalSale);
            await _mockDbContext.SaveChangesAsync();

            var temporalSaleUpdated = new TemporalSaleDTO()
            {
                Id = 1,
                ProductId = 1,
                Quantity = 10,
                Remarks = "test2"
            };

            // Act
            var result = await _controller.Put(temporalSaleUpdated);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var response = result as OkObjectResult;
            Assert.IsNotNull(response);
            var tmpSalesResponse = response.Value as TemporalSaleDTO;
            Assert.IsNotNull(tmpSalesResponse);
            Assert.AreEqual(tmpSalesResponse, temporalSaleUpdated);
        }

        [TestMethod]
        public async Task DeleteAsync_ReturnsNotFound()
        {
            // Act 
            var result = await _controller.DeleteAsync(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task DeleteAsync_ReturnsOk()
        {
            //Arrange 
            var userName = "test@example.com";
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = userName, Email = userName, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo" };

            _mockDbContext.Users.Add(user);
            await _mockDbContext.SaveChangesAsync();

            var temporalSale = new TemporalSale()
            {
                ProductId = 1,
                Quantity = 20,
                Remarks = "test",
                UserId = user.Id,
            };

            await _mockDbContext.TemporalSales.AddAsync(temporalSale);
            await _mockDbContext.SaveChangesAsync();

            // Act 
            var result = await _controller.DeleteAsync(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

    }
}
