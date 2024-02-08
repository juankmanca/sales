using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Sales.API.Controllers;
using Sales.API.Data;
using Sales.API.Helpers;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sales.Share.DTOs;
using Sales.Share.entities;
using Sales.Share.Enums;
using Sales.Share.Responses;
using Microsoft.AspNetCore.Identity;

namespace Sales.UnitTest.Controllers
{
    [TestClass]
    public class AccountsControllerTest
    {
        private Mock<IUserHelper> _mockUserHelper = null!;
        private Mock<IMailHelper> _mockMailHelper = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IFileStorage> _mockFileStorage = null!;
        private AccountsController _controller = null!;
        private DataContext _context = null!;
        private const string _container = "userphotos";
        private const string _string64base = "U29tZVZhbGlkQmFzZTY0U3RyaW5n";
        private const string EMAIL = "test@example.com";

        [TestInitialize]
        public void Initialize()
        {
            _mockUserHelper = new Mock<IUserHelper>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockFileStorage = new Mock<IFileStorage>();
            _mockMailHelper = new Mock<IMailHelper>();

            _mockConfiguration
                .SetupGet(x => x["Url Frontend"])
                .Returns("http://frontend-url.com");
            _mockConfiguration
                .SetupGet(x => x["jwtKey"])
                .Returns("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz");

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://generated-link.com");

            var _options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(_options);

            _controller = new AccountsController(
                _mockUserHelper.Object,
                _mockConfiguration.Object,
                _mockFileStorage.Object,
                _mockMailHelper.Object,
                _context)
            {
                Url = mockUrlHelper.Object
            };

            var mockHttpContext = new Mock<HttpContext>();
            var mockHttpRequest = new Mock<HttpRequest>();

            mockHttpRequest.Setup(req => req.Scheme)
                .Returns("http");
            mockHttpContext.Setup(ctx => ctx.Request)
                .Returns(mockHttpRequest.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, EMAIL),
            }, "mock"));
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = user };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task AddCity()
        {
            var Country = new Country()
            {
                Name = "test"
            };

            await _context.AddAsync(Country);
            await _context.SaveChangesAsync();

            var State = new State()
            {
                CountryId = 1,
                Name = "test"
            };

            await _context.AddAsync(State);
            await _context.SaveChangesAsync();

            var City = new City()
            {
                Id = 1,
                Name = "test",
                StateId = 1
            };

            await _context.AddAsync(City);
            await _context.SaveChangesAsync();
        }

        private async Task AddUser()
        {
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = EMAIL, Email = EMAIL, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo", CityId = 1 };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetAll_ReturnsOkListOfUsers()
        {
            // Arrange
            await AddCity();
            await AddUser();

            var pagination = new PaginationDTO()
            {
                Filter = "Juan"
            };

            // Act
            var result = await _controller.GetAll(pagination);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(okResult);
            var users = okResult.Value as List<User>;
            Assert.IsNotNull(users);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(1, users.Count());
            Assert.AreEqual(pagination.Filter, users[0].FirstName);
        }

        [TestMethod]
        public async Task GetPages_ReturnsOkCountPages()
        {
            // Arrange
            await AddCity();
            await AddUser();

            var pagination = new PaginationDTO()
            {
                Filter = "Juan"
            };

            // Act
            var result = await _controller.GetPages(pagination);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(okResult);
            var totalPages = (double)okResult.Value!;
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(1.0, totalPages);
        }

        [TestMethod]
        public async Task RecoverPassword_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var email = new EmailDTO() { Email = "test" };

            // Act 
            var result = await _controller.RecoverPassword(email);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(email.Email), Times.Once());
        }

        [TestMethod]
        public async Task RecoverPassword_EmailSentSuccessfully_ReturnsNoContent()
        {
            var emailDTO = new EmailDTO() { Email = EMAIL };
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(It.IsAny<string>());

            var response = new Response
            {
                IsSuccess = true,
            };
            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response);

            // Act
            var resulst = await _controller.RecoverPassword(emailDTO);

            // Assert
            Assert.IsInstanceOfType(resulst, typeof(NoContentResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once());
            _mockMailHelper.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task RecoverPassword_ErrorSendEmail_ReturnsBadRequest()
        {
            var emailDTO = new EmailDTO() { Email = EMAIL };
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(It.IsAny<string>());

            var response = new Response
            {
                IsSuccess = false,
            };
            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response);

            // Act
            var resulst = await _controller.RecoverPassword(emailDTO);

            // Assert
            Assert.IsInstanceOfType(resulst, typeof(BadRequestObjectResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once());
            _mockMailHelper.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task ResetPassword_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var email = new ResetPasswordDTO() { Email = "test" };

            // Act 
            var result = await _controller.ResetPassword(email);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(email.Email), Times.Once());
        }

        [TestMethod]
        public async Task ResetPassword_ResetPasswordSuccessfully_ReturnsNoContent()
        {
            var user = new User() { Email = EMAIL };
            var mockIdentityReuslt = IdentityResult.Success;
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockIdentityReuslt);

            // Act
            var result = await _controller.ResetPassword(new ResetPasswordDTO() { Email = EMAIL });

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task ResetPassword_ErroResetPassword_ReturnsBadRequest()
        {
            string description = "Test error";
            var user = new User() { Email = EMAIL };
            var mockIdentityErrors = new List<IdentityError>()
            {
                new IdentityError
                {
                    Description = description
                }
            };

            var mockIdentityResult = IdentityResult.Failed(mockIdentityErrors.ToArray());
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.ResetPassword(new ResetPasswordDTO() { Email = EMAIL });

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value;
            Assert.AreEqual(description, errorDescription);
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task ChangePasswordAsync_ModelInvalid_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("testError", "Error");

            // Act 
            var result = await _controller.ChangePasswordAsync(new ChangePasswordDTO());

            // Assert 
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task ChangePasswordAsync_UserNotFound_ReturnsNotFound()
        {
            // Act 
            var result = await _controller.ChangePasswordAsync(new ChangePasswordDTO());

            // Assert 
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task ChangePasswordAsync_ValidChange_ReturnsNoContent()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            var identityResult = IdentityResult.Success;
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(identityResult);

            // Act 
            var result = await _controller.ChangePasswordAsync(new ChangePasswordDTO());

            // Assert 
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }


        [TestMethod]
        public async Task ChangePasswordAsync_ErrorChangePassword_ReturnsBadRequest()
        {
            // Arrange
            string description = "Test error";
            var user = new User() { Email = EMAIL };
            var mockIdentityErrors = new List<IdentityError>()
            {
                new IdentityError
                {
                    Description = description
                }
            };

            var mockIdentityResult = IdentityResult.Failed(mockIdentityErrors.ToArray());
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mockIdentityResult);

            // Act 
            var result = await _controller.ChangePasswordAsync(new ChangePasswordDTO());

            // Assert 
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value;
            Assert.AreEqual(description, errorDescription);
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task PutAsync_UserNotFound_ReturnsNotFound()
        {
            // Act 
            var result = await _controller.PutAsync(new User());

            // Assert 
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task PutAsync_ExceptionTryCatch_ReturnsBadRequest()
        {
            // Act 
            string description = "Test error";
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).Throws(new Exception(description));

            var result = await _controller.PutAsync(new User());
            var response = result as BadRequestObjectResult;

            // Assert 
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsNotNull(response);
            Assert.AreEqual(description, response.Value);
        }

        [TestMethod]
        public async Task PutAsync_UserPhtoNotEmpty_UpdatePhoto()
        {
            // Arrange
           await AddCity();

            var user = new User
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = _string64base,
                CityId = 1
            };
            var currentUser = new User
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = "oldPhoto",
                CityId = 1
            };

            var newPhotoUrl = "newPhotoUrl";
            var mockIdentityResult = IdentityResult.Success;
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(currentUser);
            _mockFileStorage.Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), ".jpg", _container)).ReturnsAsync(newPhotoUrl);
            _mockUserHelper.Setup(x => x.UpdateUserAsync(currentUser)).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.PutAsync(user);
            var okResult = result as OkObjectResult;
            var token = okResult?.Value as TokenDTO;

            // Assert
            Assert.IsNotNull(token!.Token);
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.UpdateUserAsync(currentUser), Times.Once());
        }

        [TestMethod]
        public async Task PutAsync_UpdatedNotSuccessfully_Exception()
        {
            // Arrange
            await AddCity();

            var user = new User
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = _string64base,
                CityId = 1
            };

            var currentUser = new User
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = "oldPhoto",
                CityId = 1
            };

            string description = "Test error";
            var mockIdentityErrors = new List<IdentityError>()
            {
                new IdentityError
                {
                    Description = description
                }
            };

            var mockIdentityResult = IdentityResult.Failed(mockIdentityErrors.ToArray());

            var newPhotoUrl = "newPhotoUrl";
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(currentUser);
            _mockFileStorage.Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), ".jpg", _container)).ReturnsAsync(newPhotoUrl);
            _mockUserHelper.Setup(x => x.UpdateUserAsync(currentUser)).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.PutAsync(user);
            var okResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value as IdentityError;
            Assert.IsNotNull(errorDescription);
            Assert.AreEqual(description, errorDescription.Description);
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
            _mockUserHelper.Verify(x => x.UpdateUserAsync(currentUser), Times.Once());
        }

        [TestMethod]
        public async Task Get_ReturnsOkResult()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);

            // Act 
            var result =  await _controller.Get();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task ConfirmEmailAsync_UserNotFound_ReturnsNotFound()
        {
            // Act 
            var result = await _controller.ConfirmEmailAsync(Guid.NewGuid().ToString(), "string");

            // Asssert 
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task ConfirmEmailAsync_ErrorConfirmEmail_ReturnsBadRequest()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            var GUID = Guid.NewGuid();
            var token = "token";
            _mockUserHelper.Setup(x => x.GetUserAsync(GUID)).ReturnsAsync(user);

            string description = "Test error";
            var mockIdentityErrors = new List<IdentityError>()
            {
                new IdentityError
                {
                    Description = description
                }
            };

            var mockIdentityResult = IdentityResult.Failed(mockIdentityErrors.ToArray());

            _mockUserHelper.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.ConfirmEmailAsync(GUID.ToString(), token);

            // Assert
            _mockUserHelper.Verify(x => x.GetUserAsync(GUID), Times.Once());
            _mockUserHelper.Verify(x => x.ConfirmEmailAsync(user, token), Times.Once());
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value as IdentityError;
            Assert.IsNotNull(errorDescription);
            Assert.AreEqual(description, errorDescription.Description);

        }

        [TestMethod]
        public async Task ConfirmEmailAsync_ConfirmEmailSuccessfully_ReturnsOkResponse()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            var GUID = Guid.NewGuid();
            var token = "token";
            _mockUserHelper.Setup(x => x.GetUserAsync(GUID)).ReturnsAsync(user);

            var mockIdentityResult = IdentityResult.Success;

            _mockUserHelper.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.ConfirmEmailAsync(GUID.ToString(), token);

            // Assert
            _mockUserHelper.Verify(x => x.ConfirmEmailAsync(user, token), Times.Once());
            _mockUserHelper.Verify(x => x.GetUserAsync(GUID), Times.Once());
            Assert.IsInstanceOfType(result, typeof(NoContentResult));

        }

        [TestMethod]
        public async Task ResetToken_UserNotFound_ReturnsNotFound()
        {
            // Act 
            var result = await _controller.ResetToken(new EmailDTO());

            // Asssert 
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task ResetToken_ErrorSendEmail_ReturnsBadRequest()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

            var response = new Response()
            {
                IsSuccess = false,
                Message = "Error"
            };

            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response);

            // Act
            var result = await _controller.ResetToken(new EmailDTO() { Email = user.Email});

            // Assert 
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badResponse = result as BadRequestObjectResult;
            Assert.IsNotNull(badResponse);
            var badRequestMessage = badResponse.Value!;
            Assert.AreEqual(response.Message, badRequestMessage);
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
        }

        [TestMethod]
        public async Task ResetToken_RecoverPasswordSuccessfully_ReturnsOkNoContent()
        {
            // Arrange
            var user = new User() { Email = EMAIL };
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);
            _mockUserHelper.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

            var response = new Response()
            {
                IsSuccess = true,
            };

            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response);

            // Act
            var result = await _controller.ResetToken(new EmailDTO() { Email = user.Email });

            // Assert 
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());
        }

        [TestMethod]
        public async Task Login_LoginSuccessfully_ReturnsOkResponse()
        {
            // Arrange
            var LoginDTO = new LoginDTO()
            {
                Email = EMAIL,
                Password = "test"
            };
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = EMAIL, Email = EMAIL, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo", CityId = 1 };


            var singInResult = Microsoft.AspNetCore.Identity.SignInResult.Success;

            _mockUserHelper.Setup(x => x.LoginAsync(LoginDTO)).ReturnsAsync(singInResult);
            _mockUserHelper.Setup(x => x.GetUserAsync(user.Email)).ReturnsAsync(user);

            // Act
            var result = await _controller.Login(LoginDTO);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            _mockUserHelper.Verify(x => x.LoginAsync(LoginDTO), Times.Once());
            _mockUserHelper.Verify(x => x.GetUserAsync(user.Email), Times.Once());

        }

        [TestMethod]
        public async Task Login_Locked_ReturnsBadRequest()
        {
            // Arrange
            var LoginDTO = new LoginDTO()
            {
                Email = EMAIL,
                Password = "test"
            };
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = EMAIL, Email = EMAIL, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo", CityId = 1 };


            var singInResult = Microsoft.AspNetCore.Identity.SignInResult.LockedOut;

            _mockUserHelper.Setup(x => x.LoginAsync(LoginDTO)).ReturnsAsync(singInResult);

            // Act
            var result = await _controller.Login(LoginDTO);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _mockUserHelper.Verify(x => x.LoginAsync(LoginDTO), Times.Once());

        }

        [TestMethod]
        public async Task Login_NotAllowed_ReturnsBadRequest()
        {
            // Arrange
            var LoginDTO = new LoginDTO()
            {
                Email = EMAIL,
                Password = "test"
            };
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = EMAIL, Email = EMAIL, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo", CityId = 1 };


            var singInResult = Microsoft.AspNetCore.Identity.SignInResult.NotAllowed;

            _mockUserHelper.Setup(x => x.LoginAsync(LoginDTO)).ReturnsAsync(singInResult);

            // Act
            var result = await _controller.Login(LoginDTO);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _mockUserHelper.Verify(x => x.LoginAsync(LoginDTO), Times.Once());

        }

        [TestMethod]
        public async Task Login_Failed_ReturnsBadRequest()
        {
            // Arrange
            var LoginDTO = new LoginDTO()
            {
                Email = EMAIL,
                Password = "test"
            };
            var id = Guid.NewGuid().ToString();
            var user = new User { Id = id, UserName = EMAIL, Email = EMAIL, UserType = UserType.User, Address = "test", Document = "123", FirstName = "Juan", LastName = "Ocampo", CityId = 1 };


            var singInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed;

            _mockUserHelper.Setup(x => x.LoginAsync(LoginDTO)).ReturnsAsync(singInResult);

            // Act
            var result = await _controller.Login(LoginDTO);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _mockUserHelper.Verify(x => x.LoginAsync(LoginDTO), Times.Once());
        }

        [TestMethod]
        public async Task CreateUser_CreateSuccessfully_ReturnsNoContent()
        {
            // Arrange
            await AddCity();
            var token = "token";
            var user = new UserDTO
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = _string64base,
                CityId = 1
            };

            var mailResponse = new Response
            {
                IsSuccess = true
            };

            var _IdentityResult = IdentityResult.Success;
            var newPhotoUrl = "newPhotoURL";
            _mockUserHelper.Setup(x => x.AddUserAsync(user, user.Password)).ReturnsAsync(_IdentityResult);
            _mockFileStorage.Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), ".jpg", _container)).ReturnsAsync(newPhotoUrl);
            //_mockUserHelper.Setup(x => x.AddUserToRoleAsync(user, UserType.User.ToString())); // Opcional?
            _mockUserHelper.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(token);
            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mailResponse);

            // Act
            var result = await _controller.CreateUser(user);

            //Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
            _mockUserHelper.Verify(x => x.AddUserAsync(user, user.Password), Times.Once());
            _mockUserHelper.Verify(x => x.AddUserToRoleAsync(user, UserType.User.ToString()), Times.Once());
            _mockUserHelper.Verify(x => x.GenerateEmailConfirmationTokenAsync(user), Times.Once());
            _mockMailHelper.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());

        }

        [TestMethod]
        public async Task CreateUser_ErrorSendEmail_ReturnsBadRequest()
        {
            // Arrange
            await AddCity();
            var token = "token";
            var user = new UserDTO
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = null,
                CityId = 1
            };

            var mailResponse = new Response
            {
                IsSuccess = false,
                Message = "error",
            };

            var _IdentityResult = IdentityResult.Success;
            var newPhotoUrl = "newPhotoURL";
            _mockUserHelper.Setup(x => x.AddUserAsync(user, user.Password)).ReturnsAsync(_IdentityResult);
            _mockUserHelper.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(token);
            _mockMailHelper.Setup(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mailResponse);

            // Act
            var result = await _controller.CreateUser(user);

            //Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value;
            Assert.AreEqual(mailResponse.Message, errorDescription);
            _mockUserHelper.Verify(x => x.AddUserAsync(user, user.Password), Times.Once());
            _mockUserHelper.Verify(x => x.AddUserToRoleAsync(user, UserType.User.ToString()), Times.Once());
            _mockUserHelper.Verify(x => x.GenerateEmailConfirmationTokenAsync(user), Times.Once());
            _mockMailHelper.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());

        }

        [TestMethod]
        public async Task CreateUser_ErrorAddUser_ReturnsBadRequest()
        {
            // Arrange
            await AddCity();
            var user = new UserDTO
            {
                Email = EMAIL,
                UserType = UserType.User,
                Document = "123",
                FirstName = "John",
                LastName = "Doe",
                Address = "Any",
                Photo = null,
                CityId = 1
            };

            string description = "Test error";
            var mockIdentityErrors = new List<IdentityError>()
            {
                new IdentityError
                {
                    Description = description
                }
            };

            var mockIdentityResult = IdentityResult.Failed(mockIdentityErrors.ToArray());

            _mockUserHelper.Setup(x => x.AddUserAsync(user, user.Password)).ReturnsAsync(mockIdentityResult);

            // Act
            var result = await _controller.CreateUser(user);

            //Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            var errorDescription = badRequestResult.Value as IdentityError;
            Assert.AreEqual(description, errorDescription.Description);
            _mockUserHelper.Verify(x => x.AddUserAsync(user, user.Password), Times.Once());
        }
    }
}
