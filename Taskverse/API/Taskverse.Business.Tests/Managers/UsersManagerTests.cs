using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taskverse.Business.Managers;
using Taskverse.Data;

namespace Taskverse.Business.Tests.Managers;

[TestClass]
public class UsersManagerTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void UsersManager_Constructor_ThrowsOnNullContext()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new UsersManager(null!));
    }
}
