using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Taskverse.Api.Controllers;
using Taskverse.Api.Models;
using Taskverse.Api.Tests.Helpers;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Tests;

[TestClass]
public class UsersControllerTests : TestControllerBase
{
    private readonly Mock<IUsersOrchestrator> _mockOrchestrator;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockOrchestrator = new Mock<IUsersOrchestrator>();

        _controller = new UsersController(_mockOrchestrator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = MockHttpContext().Object
            }
        };
    }

    [TestMethod]
    public void UsersController_Constructor_Success()
    {
        Assert.IsNotNull(_controller);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUser_ReturnsOk_WhenUserFound()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetUser(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetUserDto());

        // Act
        IActionResult result = await _controller.GetUser("user-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SearchUsers_ReturnsOk_WithResults()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.SearchUsers(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(MockData.GetPagedUserDto());

        var model = new UserSearchRequestModel
        {
            Email      = null,
            Role       = "Student",
            IsActive   = true,
            PageNumber = 1,
            PageSize   = 20
        };

        // Act
        IActionResult result = await _controller.SearchUsers(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateUser_ReturnsCreated_WhenValid()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.CreateUser(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(MockData.GetUserDto());

        var model = new CreateUserRequestModel
        {
            FullName = "Jane Smith",
            Email    = "jane.smith@example.com",
            Phone    = "+919876543210",
            Role     = "Student",
            Password = "SecurePass123!"
        };

        // Act
        IActionResult result = await _controller.CreateUser(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatedResult));
        var created = (CreatedResult)result;
        Assert.IsNotNull(created.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdateUser_ReturnsOk_WhenUserFound()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.UpdateUser(It.IsAny<string>(), It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(MockData.GetUserDto());

        var model = new UpdateUserRequestModel
        {
            FullName = "Updated Name",
            Phone    = null,
            Status   = null
        };

        // Act
        IActionResult result = await _controller.UpdateUser("user-123", model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task DeleteUser_ReturnsNoContent_WhenDeleted()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.DeleteUser(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult result = await _controller.DeleteUser("user-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserRoles_ReturnsOk_WithRoles()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetUserRoles(It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "Student" });

        // Act
        IActionResult result = await _controller.GetUserRoles("user-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }
}
