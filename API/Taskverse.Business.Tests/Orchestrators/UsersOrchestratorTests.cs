using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Business.Orchestrators;
using Taskverse.Business.Tests.Helpers;

namespace Taskverse.Business.Tests.Orchestrators;

[TestClass]
public class UsersOrchestratorTests
{
    private readonly Mock<IMicroServiceOrchestrator> _mockMicroServiceOrchestrator;
    private readonly Mock<IUsersManager> _mockUsersManager;
    private readonly UsersOrchestrator _orchestrator;

    public UsersOrchestratorTests()
    {
        _mockMicroServiceOrchestrator = new Mock<IMicroServiceOrchestrator>();
        _mockUsersManager = new Mock<IUsersManager>();
        _orchestrator = new UsersOrchestrator(
            _mockMicroServiceOrchestrator.Object,
            _mockUsersManager.Object);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUser_ReturnsUserDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetUser(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetUserModel()));

        // Act
        UserDto result = await _orchestrator.GetUser(TestConstants.UserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TestConstants.UserId, result.UserId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateUser_ReturnsUserDto_WhenCreated()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.CreateUser(It.IsAny<CreateUserModel>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetUserModel()));

        var dto = new CreateUserDto
        {
            FullName = "John Doe",
            Email    = "john.doe@example.com",
            Phone    = "+911234567890",
            CollegeName = "Horizon Institute of Tech",
            Role     = "Student",
            Password = "SecurePass123!"
        };

        // Act
        UserDto result = await _orchestrator.CreateUser(dto);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task DeleteUser_Completes_WhenSuccessful()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.DeleteUser(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetObjectResult<object>(null!, 204));

        // Act
        await _orchestrator.DeleteUser(TestConstants.UserId);

        // Assert
        _mockMicroServiceOrchestrator.Verify(o => o.DeleteUser(TestConstants.UserId), Times.Once);
    }
}
