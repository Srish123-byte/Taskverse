using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using Taskverse.Api.Models;

namespace Taskverse.Api.Tests.Models;

[TestClass]
public class UserModelTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void CreateUserRequestModel_RequiredFields_AreValidated()
    {
        // Arrange — email deliberately empty to trigger validation failure
        var model = new CreateUserRequestModel
        {
            Email    = string.Empty,
            FullName = "John Doe",
            Role     = "Student",
            Password = "SecurePass123!"
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CreateUserRequestModel_AllFieldsSet_PassesValidation()
    {
        // Arrange
        var model = new CreateUserRequestModel
        {
            FullName = "John Doe",
            Email    = "john.doe@example.com",
            Phone    = "+911234567890",
            Role     = "Student",
            Password = "SecurePass123!"
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void UserResponseModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var model = new UserResponseModel();

        // Assert
        Assert.AreEqual(string.Empty, model.UserId);
        Assert.AreEqual(string.Empty, model.FullName);
        Assert.AreEqual(string.Empty, model.Email);
        Assert.AreEqual(string.Empty, model.Role);
        Assert.AreEqual(string.Empty, model.Status);
        Assert.IsNull(model.Phone);
        Assert.IsNull(model.ModifiedAt);
    }
}
