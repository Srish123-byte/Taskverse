using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IBulkStudentUploadService
{
    Task<BulkStudentUploadResultDto> UploadAsync(BulkStudentUploadRequestDto request, CancellationToken cancellationToken = default);
}

