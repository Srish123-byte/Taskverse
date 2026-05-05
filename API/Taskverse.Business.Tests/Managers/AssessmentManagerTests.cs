using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taskverse.Business.Managers;

namespace Taskverse.Business.Tests.Managers;

/// <summary>
/// AssessmentManager is stubbed while assessment tables are temporarily
/// removed from the DB context. Tests will be re-enabled when tables are added back.
/// </summary>
[TestClass]
public class AssessmentManagerTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void AssessmentManager_GetById_ThrowsNotImplemented()
    {
        // Arrange
        var manager = new AssessmentManager();

        // Act & Assert
        Assert.ThrowsExceptionAsync<NotImplementedException>(
            () => manager.GetById("any-id"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void AssessmentManager_GetByUserId_ThrowsNotImplemented()
    {
        // Arrange
        var manager = new AssessmentManager();

        // Act & Assert
        Assert.ThrowsExceptionAsync<NotImplementedException>(
            () => manager.GetByUserId("any-id"));
    }
}
