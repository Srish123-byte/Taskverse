using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Taskverse.Api.Controllers;
using Taskverse.Api.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Tests;

[TestClass]
public class AuthControllerTests
{
    private readonly Mock<IAuthOrchestrator> _mockOrchestrator;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockOrchestrator = new Mock<IAuthOrchestrator>();
        _controller = new AuthController(_mockOrchestrator.Object);
    }

    [TestMethod]
    public void AuthController_Constructor_Success()
    {
        Assert.IsNotNull(_controller);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Login_ReturnsOk_WhenCredentialsValid()
    {
        // Arrange
        var loginResponse = new LoginResponseDto(
            AccessToken: "token",
            RefreshToken: "refresh-token",
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            UserId: "user-123",
            Email: "john.doe@example.com",
            FirstName: "John",
            LastName: "Doe",
            Roles: ["Student"],
            Status: "APPROVED");

        _mockOrchestrator
            .Setup(o => o.Login(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(loginResponse);

        var model = new LoginRequestModel
        {
            Email = "john.doe@example.com",
            Password = "SecurePass123!"
        };

        // Act
        IActionResult result = await _controller.Login(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Login_ReturnsUnauthorized_WhenResultNull()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.Login(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((LoginResponseDto?)null);

        var model = new LoginRequestModel
        {
            Email = "unknown@example.com",
            Password = "WrongPassword"
        };

        // Act
        IActionResult result = await _controller.Login(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task RefreshToken_ReturnsOk_WhenTokenValid()
    {
        // Arrange
        var refreshResponse = new RefreshLoginResponseDto(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            ExpiresAt: DateTime.UtcNow.AddHours(1));

        _mockOrchestrator
            .Setup(o => o.RefreshToken(It.IsAny<RefreshTokenRequestDto>()))
            .ReturnsAsync(refreshResponse);

        var model = new RefreshTokenRequestModel
        {
            RefreshToken = "valid-refresh-token"
        };

        // Act
        IActionResult result = await _controller.RefreshToken(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Logout_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.Logout(It.IsAny<LogoutRequestDto>()))
            .Returns(Task.CompletedTask);

        var model = new LogoutRequestModel
        {
            UserId = "user-123",
            RefreshToken = "refresh-token"
        };

        // Act
        IActionResult result = await _controller.Logout(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ValidateToken_ReturnsOk_WithResult()
    {
        // Arrange
        var validateResponse = new ValidateTokenResponseDto(
            IsValid: true,
            UserId: "user-123",
            Roles: ["Student"],
            ExpiresAt: DateTime.UtcNow.AddHours(1));

        _mockOrchestrator
            .Setup(o => o.ValidateToken(It.IsAny<ValidateTokenRequestDto>()))
            .ReturnsAsync(validateResponse);

        var model = new ValidateTokenRequestModel
        {
            Token = "valid-jwt-token"
        };

        // Act
        IActionResult result = await _controller.ValidateToken(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }
}
