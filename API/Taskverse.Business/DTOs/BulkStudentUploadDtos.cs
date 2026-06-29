namespace Taskverse.Business.DTOs;

public class BulkStudentUploadRowDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string CollegeName { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public class BulkStudentUploadRequestDto
{
    public Guid UploadedByUserId { get; set; }
    public string UploadedByEmail { get; set; } = string.Empty;
    public string UploadedByDisplayName { get; set; } = string.Empty;
    public Guid? RestrictedCollegeId { get; set; }
    public List<BulkStudentUploadRowDto> Rows { get; set; } = [];
}

public class BulkStudentUploadCreatedUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class BulkStudentUploadRowIssueDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class BulkStudentUploadResultDto
{
    public int CreatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public List<BulkStudentUploadCreatedUserDto> CreatedUsers { get; set; } = [];
    public List<BulkStudentUploadRowIssueDto> DuplicateRows { get; set; } = [];
    public List<BulkStudentUploadRowIssueDto> InvalidRows { get; set; } = [];
}
