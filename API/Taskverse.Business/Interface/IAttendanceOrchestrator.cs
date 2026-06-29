using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IAttendanceOrchestrator
{
    Task<List<AttendanceBatchGroupDto>> GetAttendanceBatches(Guid collegeId, Guid requesterUserId);
    Task<AttendanceRosterDto> GetAttendanceRoster(AttendanceRosterRequestDto dto);
    Task<AttendanceRosterDto> SubmitAttendance(SubmitAttendanceDto dto);
    Task<AttendanceHistoryDto> GetAttendanceHistory(AttendanceHistoryRequestDto dto);
    Task<AttendanceExportDto> ExportAttendance(AttendanceHistoryRequestDto dto);
    Task EmailAttendanceReport(AttendanceEmailReportDto dto);
}
