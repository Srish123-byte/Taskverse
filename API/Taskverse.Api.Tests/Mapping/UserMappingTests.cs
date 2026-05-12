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
            FullName = "Jane Smith",
            Email    = "jane.smith@example.com",
            Phone    = "+919876543210",
            Role     = "Student",
            Password = "SecurePass123!"
        };

        // Act
        CreateUserDto dto = model.ToDto();

        // Assert
        Assert.AreEqual(model.FullName,  dto.FullName);
        Assert.AreEqual(model.Email,     dto.Email);
        Assert.AreEqual(model.Phone,     dto.Phone);
        Assert.AreEqual(model.Role,      dto.Role);
        Assert.AreEqual(model.Password,  dto.Password);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void UpdateUserRequestModel_ToDto_MapsCorrectly()
    {
        // Arrange
        var model = new UpdateUserRequestModel
        {
            FullName = "Updated Name",
            Phone    = "+910000000000",
            Status   = "ACTIVE"
        };

        // Act
        UpdateUserDto dto = model.ToDto();

        // Assert
        Assert.AreEqual(model.FullName, dto.FullName);
        Assert.AreEqual(model.Phone,    dto.Phone);
        Assert.AreEqual(model.Status,   dto.Status);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void UserDto_ToResponseModel_MapsCorrectly()
    {
        // Arrange
        var createdAt  = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var modifiedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        var dto = new UserDto
        {
            UserId     = "user-123",
            FullName   = "John Doe",
            Email      = "john.doe@example.com",
            Phone      = "+911234567890",
            Role       = "Student",
            Status     = "PENDING_APPROVAL",
            CreatedAt  = createdAt,
            ModifiedAt = modifiedAt
        };

        // Act
        UserResponseModel model = dto.ToResponseModel();

        // Assert
        Assert.AreEqual(dto.UserId,     model.UserId);
        Assert.AreEqual(dto.FullName,   model.FullName);
        Assert.AreEqual(dto.Email,      model.Email);
        Assert.AreEqual(dto.Phone,      model.Phone);
        Assert.AreEqual(dto.Role,       model.Role);
        Assert.AreEqual(dto.Status,     model.Status);
        Assert.AreEqual(dto.CreatedAt,  model.CreatedAt);
        Assert.AreEqual(dto.ModifiedAt, model.ModifiedAt);
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
                new UserDto { UserId = "user-123", FullName = "John Doe",   Email = "john@example.com",  Role = "Student", Status = "ACTIVE",           CreatedAt = DateTime.UtcNow },
                new UserDto { UserId = "user-456", FullName = "Jane Smith", Email = "jane@example.com",  Role = "Trainer", Status = "PENDING_APPROVAL",  CreatedAt = DateTime.UtcNow }
            ],
            TotalCount = 2,
            PageNumber = 1,
            PageSize   = 20
        };

        // Act
        PagedUserResponseModel model = pagedDto.ToResponseModel();

        // Assert
        Assert.AreEqual(2,                    model.Items.Count);
        Assert.AreEqual(pagedDto.TotalCount,  model.TotalCount);
        Assert.AreEqual(pagedDto.PageNumber,  model.PageNumber);
        Assert.AreEqual(pagedDto.PageSize,    model.PageSize);
    }
}
