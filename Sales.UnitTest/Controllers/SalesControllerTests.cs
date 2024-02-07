using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sales.API.Controllers;
using Sales.API.Data;
using Sales.API.Helpers;
using Sales.Share.DTOs;
using Sales.Share.entities;
using Sales.Share.Enums;
using Sales.Share.Responses;
using System.Security.Claims;

namespace Sales.UnitTest.Controllers
{
    [TestClass]
    public class SalesControllerTests
    {
        private SalesController _salesController = null!;
        private Mock<IOrdersHelper> _mokOrdersHelper = null!;
        private DataContext _mokDbContext = null!;
        private Mock<IUserHelper> _mockUserHelper = null!;

        [TestInitialize]
        public void SetUp()
        {
            _mockUserHelper = new Mock<IUserHelper>();
            _mokOrdersHelper = new Mock<IOrdersHelper>();

            //Setting up InMemory database
            var _options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _mokDbContext = new DataContext(_options);

            _salesController = new SalesController(_mokOrdersHelper.Object, _mokDbContext, _mockUserHelper.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            _salesController.ControllerContext = new ControllerContext();
            _salesController.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mokDbContext.Database.EnsureDeleted();
            _mokDbContext.Dispose();
        }

        [TestMethod]
        public async Task Put_OrderNotFound_ReturnsNotFound()
        {
            // Arrange
            var orderDto = new SaleDTO { Id = 1 };
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = Share.Enums.UserType.Admin };
            _mockUserHelper.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.IsUserInRoleAsync(user, user.UserType.ToString())).ReturnsAsync(true);

            // Act
            var result = await _salesController.Put(orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            // Para verficar que llama ese metodo al menos una vez.
            _mockUserHelper.Verify(x => x.GetUserAsync(userName), Times.Once());
            _mockUserHelper.Verify(x => x.IsUserInRoleAsync(user, user.UserType.ToString()), Times.Once());
        }

        [TestMethod]
        public async Task Put_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var orderDto = new SaleDTO { Id = 1 };

            // Act
            var result = await _salesController.Put(orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));

        }

        [TestMethod]
        public async Task Put_NoAdmin_ReturnsBadRequest()
        {
            // Arrange
            var orderDto = new SaleDTO { Id = 1, OrderStatus = OrderStatus.Enviado };
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = Share.Enums.UserType.User };
            _mockUserHelper.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.IsUserInRoleAsync(user, user.UserType.ToString())).ReturnsAsync(false);

            // Act
            var result = await _salesController.Put(orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(userName), Times.Once());
            _mockUserHelper.Verify(x => x.IsUserInRoleAsync(user, UserType.Admin.ToString()), Times.Once());
        }
        [TestMethod]
        public async Task Put_ValidParameters_ReturnsOk()
        {
            // Arrange
            var orderDto = new SaleDTO { Id = 1, OrderStatus = OrderStatus.Cancelado };
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = Share.Enums.UserType.Admin };
            _mockUserHelper.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.IsUserInRoleAsync(user, user.UserType.ToString())).ReturnsAsync(true);

            _mokDbContext.Products.Add(new Product
            {
                Id = 1,
                Name = "Nome",
                Description = "Smoe"
            });
            _mokDbContext.Sales.Add(new Sale
            {
                Id = orderDto.Id,
                SaleDetails = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 1 }
                }
            });
            await _mokDbContext.SaveChangesAsync();

            // Act
            var result = await _salesController.Put(orderDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(userName), Times.Once());
            _mockUserHelper.Verify(x => x.IsUserInRoleAsync(user, UserType.Admin.ToString()), Times.Once());
        }

        [TestMethod]
        public async Task Get_ReturnsNotFound()
        {
            //Act
            var result = await _salesController.Get(1);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Get_OrderFound_ReturnsOkResult()
        {
            // Arrange
            var orderDto = new SaleDTO { Id = 1, OrderStatus = OrderStatus.Cancelado };
            _mokDbContext.Products.Add(new Product
            {
                Id = 1,
                Name = "Nome",
                Description = "Smoe"
            });
            _mokDbContext.Sales.Add(new Sale
            {
                Id = orderDto.Id,
                SaleDetails = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 1 }
                }
            });
            await _mokDbContext.SaveChangesAsync();

            // Act
            var result = await _salesController.Get(orderDto.Id);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task GetPages_ReturnsUserNotFound()
        {
            // Arrange 
            var pagination = new PaginationDTO();

            //Act
            var result = await _salesController.GetPages(pagination);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        }

        [TestMethod]
        public async Task GetPages_ReturnsOkResponse()
        {
            // Arrange 
            var pagination = new PaginationDTO();

            //Confign User
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = UserType.User };
            _mockUserHelper.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.IsUserInRoleAsync(user, user.UserType.ToString())).ReturnsAsync(false);

            // Add Sales
            var orderDto = new SaleDTO { Id = 1, OrderStatus = OrderStatus.Cancelado };
            _mokDbContext.Products.Add(new Product
            {
                Id = 1,
                Name = "Nome",
                Description = "Smoe"
            });
            _mokDbContext.Sales.Add(new Sale
            {
                Id = orderDto.Id,
                SaleDetails = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 1 }
                }
            });
            await _mokDbContext.SaveChangesAsync();

            //Act
            var result = await _salesController.GetPages(pagination);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            double totalPages = (double)okResult.Value!;
            Assert.AreEqual(0, totalPages);
            _mockUserHelper.Verify(x => x.GetUserAsync(userName), Times.Once());
            _mockUserHelper.Verify(x => x.IsUserInRoleAsync(user, UserType.Admin.ToString()), Times.Once());
        }

        [TestMethod]
        public async Task Get_ReturnsUserNotFound()
        {
            // Arrange 
            var pagination = new PaginationDTO();

            //Act
            var result = await _salesController.Get(pagination);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        }

        [TestMethod]
        public async Task Get_ReturnsOkResponse()
        {
            // Arrange 
            var pagination = new PaginationDTO();

            //Confign User
            var userName = "test@example.com";
            var user = new User { UserName = userName, UserType = UserType.User };
            _mockUserHelper.Setup(x => x.GetUserAsync(userName)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.IsUserInRoleAsync(user, user.UserType.ToString())).ReturnsAsync(false);

            // Add Sales
            var orderDto = new SaleDTO { Id = 1, OrderStatus = OrderStatus.Cancelado };
            _mokDbContext.Products.Add(new Product
            {
                Id = 1,
                Name = "Nome",
                Description = "Smoe"
            });
            _mokDbContext.Sales.Add(new Sale
            {
                Id = orderDto.Id,
                SaleDetails = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 1 }
                }
            });
            await _mokDbContext.SaveChangesAsync();

            //Act
            var result = await _salesController.Get(pagination);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(userName), Times.Once());
            _mockUserHelper.Verify(x => x.IsUserInRoleAsync(user, UserType.Admin.ToString()), Times.Once());
        }

        [TestMethod]
        public async Task Post_ReturnsNoContent()
        {
            //Arrange 
            var response = new Response()
            {
                IsSuccess = true,
            };
            _mokOrdersHelper.Setup(x => x.ProcessOrderAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            //Act 
            var result = await _salesController.Post(new SaleDTO());

            //Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));

        }

        [TestMethod]
        public async Task Post_ReturnsBadRequest()
        {
            //Arrange 
            string email = "test@example.com";
            var sale = new SaleDTO
            {
                Remarks = "test"
            };

            var response = new Response()
            {
                IsSuccess = false,
            };
            _mokOrdersHelper.Setup(x => x.ProcessOrderAsync(email, sale.Remarks)).ReturnsAsync(response);

            //Act 
            var result = await _salesController.Post(sale);

            //Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));

        }
    }
}
