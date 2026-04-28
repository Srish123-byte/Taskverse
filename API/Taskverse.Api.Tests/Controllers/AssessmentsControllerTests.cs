using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Taskverse.Api.Controllers;
using Taskverse.Api.Models;
using Taskverse.Api.Tests.Helpers;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Tests;

[TestClass]
public class AssessmentsControllerTests : TestControllerBase
{
    private readonly Mock<IAssessmentOrchestrator> _mockOrchestrator;
    private readonly AssessmentsController _controller;

    public AssessmentsControllerTests()
    {
        _mockOrchestrator = new Mock<IAssessmentOrchestrator>();

        _controller = new AssessmentsController(_mockOrchestrator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = MockHttpContext().Object
            }
        };
    }

    [TestMethod]
    public void AssessmentsController_Constructor_Success()
    {
        Assert.IsNotNull(_controller);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessment_ReturnsOk_WhenFound()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetAssessment(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetAssessmentDto());

        // Act
        IActionResult result = await _controller.GetAssessment("assess-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task CreateAssessment_ReturnsCreated_WhenValid()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.CreateAssessment(It.IsAny<CreateAssessmentDto>()))
            .ReturnsAsync(MockData.GetAssessmentDto());

        var model = new CreateAssessmentRequestModel
        {
            Title = "New Assessment",
            Description = "A new assessment for testing",
            Type = "Exam",
            ExamId = "exam-456",
            ChallengeIds = null,
            AssignedTo = ["user-123"],
            DueDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "admin-001"
        };

        // Act
        IActionResult result = await _controller.CreateAssessment(model);

        // Assert
        Assert.IsInstanceOfType(result, typeof(CreatedResult));
        var created = (CreatedResult)result;
        Assert.IsNotNull(created.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessmentsByUser_ReturnsOk()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetAssessmentsByUser(It.IsAny<string>()))
            .ReturnsAsync(new List<AssessmentDto> { MockData.GetAssessmentDto() });

        // Act
        IActionResult result = await _controller.GetAssessmentsByUser("user-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessmentResult_ReturnsOk()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetAssessmentResult(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(MockData.GetAssessmentResultDto());

        // Act
        IActionResult result = await _controller.GetAssessmentResult("assess-123", "user-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetAssessmentSummary_ReturnsOk()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.GetAssessmentSummary(It.IsAny<string>()))
            .ReturnsAsync(MockData.GetAssessmentSummaryDto());

        // Act
        IActionResult result = await _controller.GetAssessmentSummary("assess-123");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var ok = (OkObjectResult)result;
        Assert.IsNotNull(ok.Value);
    }
}
