using Taskverse.API.College.Service.Models;

namespace Taskverse.API.College.Service.Services;

internal static class CollegeStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Rejected = "Rejected";
}

internal static class ApprovalStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

internal static class CollegeStore
{
    private static readonly List<CollegeRecord> _colleges =
    [
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Northwind Institute of Technology",
            "Aarav Mehta",
            "Bengaluru",
            "Karnataka",
            CollegeStatuses.Active,
            ApprovalStatuses.Approved,
            true,
            DateTime.UtcNow.AddDays(-30),
            "registrar@northwind.edu",
            DateTime.UtcNow.AddDays(-28),
            "platform@taskverse.ai",
            "Initial rollout college"),
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Riverdale Engineering College",
            "Saanvi Reddy",
            "Hyderabad",
            "Telangana",
            CollegeStatuses.Active,
            ApprovalStatuses.Pending,
            false,
            DateTime.UtcNow.AddDays(-2),
            "admin@riverdale.edu",
            null,
            null,
            "Awaiting compliance review"),
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Summit Medical Academy",
            "Ishaan Kulkarni",
            "Pune",
            "Maharashtra",
            CollegeStatuses.Inactive,
            ApprovalStatuses.Approved,
            false,
            DateTime.UtcNow.AddDays(-75),
            "director@summitmed.edu",
            DateTime.UtcNow.AddDays(-70),
            "platform@taskverse.ai",
            "Temporarily suspended during renewal")
    ];

    public static IReadOnlyList<CollegeRecord> Colleges => _colleges;

    public static void Replace(CollegeRecord updated)
    {
        var index = _colleges.FindIndex(item => item.CollegeId == updated.CollegeId);
        if (index >= 0)
        {
            _colleges[index] = updated;
        }
    }
}
