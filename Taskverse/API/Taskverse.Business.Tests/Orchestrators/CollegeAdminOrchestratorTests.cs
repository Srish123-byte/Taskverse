using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Orchestrators;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Tests.Orchestrators;

[TestClass]
public class CollegeAdminOrchestratorTests
{
    private readonly Mock<IMicroServiceOrchestrator> _mockMicroServiceOrchestrator;
    private readonly Mock<IDbContextFactory<TaskverseContext>> _mockDbContextFactory;
    private readonly CollegeAdminOrchestrator _orchestrator;

    public CollegeAdminOrchestratorTests()
    {
        _mockMicroServiceOrchestrator = new Mock<IMicroServiceOrchestrator>();
        _mockDbContextFactory = new Mock<IDbContextFactory<TaskverseContext>>();
        _orchestrator = new CollegeAdminOrchestrator(
            _mockMicroServiceOrchestrator.Object,
            _mockDbContextFactory.Object);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateClass_ThrowsDownstreamMessage_WhenMicroserviceReturnsBadRequestJson()
    {
        // Arrange
        const string expectedMessage = "A class named 'B.Tech' already exists for academic year '2024'.";

        _mockMicroServiceOrchestrator
            .Setup(o => o.CreateCollegeClass(It.IsAny<string>(), It.IsAny<CreateCollegeClassModel>()))
            .ReturnsAsync(new ObjectResult($"{{\"message\":\"{expectedMessage}\"}}") { StatusCode = 400 });

        var dto = new CreateCollegeClassDto
        {
            Name = "B.Tech",
            AcademicYear = "2024",
            Department = "CSE"
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            _orchestrator.CreateClass(Guid.NewGuid(), dto));

        // Assert
        Assert.AreEqual(expectedMessage, ex.Message);
    }
}
