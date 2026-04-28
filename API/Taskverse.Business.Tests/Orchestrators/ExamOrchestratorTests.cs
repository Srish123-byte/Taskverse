using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using Moq;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Orchestrators;
using Taskverse.Business.Tests.Helpers;

namespace Taskverse.Business.Tests.Orchestrators;

[TestClass]
public class ExamOrchestratorTests
{
    private readonly Mock<IMicroServiceOrchestrator> _mockMicroServiceOrchestrator;
    private readonly Mock<ILog> _mockLog;
    private readonly ExamOrchestrator _orchestrator;

    public ExamOrchestratorTests()
    {
        _mockMicroServiceOrchestrator = new Mock<IMicroServiceOrchestrator>();
        _mockLog = new Mock<ILog>();
        _orchestrator = new ExamOrchestrator(_mockMicroServiceOrchestrator.Object, _mockLog.Object);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetExam_ReturnsExamDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetExam(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetExamModel()));

        // Act
        ExamDto result = await _orchestrator.GetExam(TestConstants.ExamId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TestConstants.ExamId, result.ExamId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateExam_ReturnsExamDto_WhenCreated()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.CreateExam(It.IsAny<CreateExamModel>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetExamModel()));

        var dto = new CreateExamDto
        {
            Title = "Mid-Term Exam",
            Description = "Covers chapters 1 through 5",
            DurationMinutes = 90,
            TotalMarks = 100,
            PassingMarks = 60,
            CreatedBy = "admin-001"
        };

        // Act
        ExamDto result = await _orchestrator.CreateExam(dto);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SubmitExam_ReturnsExamResultDto_WhenSubmitted()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.SubmitExam(It.IsAny<ExamSubmissionModel>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetExamResultModel()));

        var dto = new ExamSubmissionDto
        {
            ExamId = TestConstants.ExamId,
            UserId = TestConstants.UserId,
            Answers = new List<AnswerDto>(),
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        ExamResultDto result = await _orchestrator.SubmitExam(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsPassed);
    }
}
