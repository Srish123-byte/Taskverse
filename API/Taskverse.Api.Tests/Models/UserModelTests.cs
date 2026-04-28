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
        // Arrange
        var model = new CreateUserRequestModel
        {
            Email = string.Empty,
            FirstName = "John",
            LastName = "Doe",
            Role = "Student",
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
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "Student",
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
        Assert.IsFalse(model.IsActive);
        Assert.AreEqual(string.Empty, model.UserId);
        Assert.AreEqual(string.Empty, model.Email);
        Assert.AreEqual(string.Empty, model.FirstName);
        Assert.AreEqual(string.Empty, model.LastName);
        Assert.AreEqual(string.Empty, model.Role);
    }
}
