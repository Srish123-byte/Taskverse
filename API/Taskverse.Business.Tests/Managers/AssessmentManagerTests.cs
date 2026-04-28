using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taskverse.Business.Managers;
using Taskverse.Data;

namespace Taskverse.Business.Tests.Managers;

[TestClass]
public class AssessmentManagerTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void AssessmentManager_Constructor_ThrowsOnNullContext()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new AssessmentManager(null!));
    }
}
