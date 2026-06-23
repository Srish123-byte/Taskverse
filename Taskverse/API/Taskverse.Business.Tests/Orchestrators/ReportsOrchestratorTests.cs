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
public class ReportsOrchestratorTests
{
    private readonly Mock<IMicroServiceOrchestrator> _mockMicroServiceOrchestrator;
    private readonly ReportsOrchestrator _orchestrator;

    public ReportsOrchestratorTests()
    {
        _mockMicroServiceOrchestrator = new Mock<IMicroServiceOrchestrator>();
        _orchestrator = new ReportsOrchestrator(_mockMicroServiceOrchestrator.Object);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GenerateReport_ReturnsReportDto_WhenGenerated()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GenerateReport(It.IsAny<GenerateReportRequestModel>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetReportModel()));

        var dto = new GenerateReportDto
        {
            Type = "Performance",
            UserId = TestConstants.UserId,
            AssessmentId = null,
            ExamId = null,
            DateFrom = null,
            DateTo = null
        };

        // Act
        ReportDto result = await _orchestrator.GenerateReport(dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TestConstants.ReportId, result.ReportId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserPerformanceReport_ReturnsDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetUserPerformanceReport(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetUserPerformanceReportModel()));

        // Act
        UserPerformanceReportDto result = await _orchestrator.GetUserPerformanceReport(TestConstants.UserId);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessmentReport_ReturnsDto_WhenFound()
    {
        // Arrange
        _mockMicroServiceOrchestrator
            .Setup(o => o.GetAssessmentReport(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetJsonObjectResult(MockData.GetAssessmentReportModel()));

        // Act
        AssessmentReportDto result = await _orchestrator.GetAssessmentReport(TestConstants.AssessmentId);

        // Assert
        Assert.IsNotNull(result);
    }
}
