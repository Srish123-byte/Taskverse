using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Tests.Mapping;

[TestClass]
public class UserMappingTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void CreateUserRequestModel_ToDto_MapsCorrectly()
    {
        // Arrange
        var model = new CreateUserRequestModel
        {
            Email = "jane.smith@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Role = "Student",
            Password = "SecurePass123!"
        };

        // Act
        CreateUserDto dto = model.ToDto();

        // Assert
        Assert.AreEqual(model.Email, dto.Email);
        Assert.AreEqual(model.FirstName, dto.FirstName);
        Assert.AreEqual(model.LastName, dto.LastName);
        Assert.AreEqual(model.Role, dto.Role);
        Assert.AreEqual(model.Password, dto.Password);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void UpdateUserRequestModel_ToDto_MapsCorrectly()
    {
        // Arrange
        var model = new UpdateUserRequestModel
        {
            FirstName = "Updated",
            LastName = "Name",
            IsActive = false
        };

        // Act
        UpdateUserDto dto = model.ToDto();

        // Assert
        Assert.AreEqual(model.FirstName, dto.FirstName);
        Assert.AreEqual(model.LastName, dto.LastName);
        Assert.AreEqual(model.IsActive, dto.IsActive);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void UserDto_ToResponseModel_MapsCorrectly()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        var dto = new UserDto
        {
            UserId = "user-123",
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "Student",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        UserResponseModel model = dto.ToResponseModel();

        // Assert
        Assert.AreEqual(dto.UserId, model.UserId);
        Assert.AreEqual(dto.Email, model.Email);
        Assert.AreEqual(dto.FirstName, model.FirstName);
        Assert.AreEqual(dto.LastName, model.LastName);
        Assert.AreEqual(dto.Role, model.Role);
        Assert.AreEqual(dto.IsActive, model.IsActive);
        Assert.AreEqual(dto.CreatedAt, model.CreatedAt);
        Assert.AreEqual(dto.UpdatedAt, model.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void PagedUserDto_ToResponseModel_MapsCorrectly()
    {
        // Arrange
        var pagedDto = new PagedUserDto
        {
            Items =
            [
                new UserDto
                {
                    UserId = "user-123",
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserDto
                {
                    UserId = "user-456",
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Role = "Trainer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            ],
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        PagedUserResponseModel model = pagedDto.ToResponseModel();

        // Assert
        Assert.AreEqual(2, model.Items.Count);
        Assert.AreEqual(pagedDto.TotalCount, model.TotalCount);
        Assert.AreEqual(pagedDto.PageNumber, model.PageNumber);
        Assert.AreEqual(pagedDto.PageSize, model.PageSize);
    }
}
