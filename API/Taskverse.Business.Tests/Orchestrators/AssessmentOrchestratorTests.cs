using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Orchestrators;
using Taskverse.Business.Tests.Helpers;

namespace Taskverse.Business.Tests.Orchestrators;

[TestClass]
public class AssessmentOrchestratorTests
{
    private readonly Mock<IMicroServiceOrchestrator> _mockMicroServiceOrchestrator;
    private readonly AssessmentOrchestrator _orchestrator;

    public AssessmentOrchestratorTests()
    {
        _mockMicroServiceOrchestrator = new Mock<IMicroServiceOrchestrator>();
        _orchestrator = new AssessmentOrchestrator(_mockMicroServiceOrchestrator.Object);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessment_ReturnsAssessmentDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetAssessment(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetAssessmentModel()));

        // Act
        AssessmentDto result = await _orchestrator.GetAssessment(TestConstants.AssessmentId);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateAssessment_ReturnsAssessmentDto_WhenCreated()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.CreateAssessment(It.IsAny<CreateAssessmentModel>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetAssessmentModel()));

        var dto = new CreateAssessmentDto
        {
            Title = "New Assessment",
            Description = "A new assessment for testing",
            Type = "Exam",
            ExamId = TestConstants.ExamId,
            ChallengeIds = null,
            AssignedTo = [TestConstants.UserId],
            DueDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "admin-001"
        };

        // Act
        AssessmentDto result = await _orchestrator.CreateAssessment(dto);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessmentSummary_ReturnsSummaryDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetAssessmentSummary(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetAssessmentSummaryModel()));

        // Act
        AssessmentSummaryDto result = await _orchestrator.GetAssessmentSummary(TestConstants.AssessmentId);

        // Assert
        Assert.IsNotNull(result);
    }
}
