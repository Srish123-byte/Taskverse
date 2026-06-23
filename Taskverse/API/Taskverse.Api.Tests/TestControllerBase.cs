using Microsoft.AspNetCore.Http;
using Moq;

namespace Taskverse.Api.Tests;

public class TestControllerBase
{
    protected static Mock<HttpContext> MockHttpContext()
    {
        var headers = new HeaderDictionary
        {
            { "UserId", "user-123" },
            { "UserRole", "Student" },
            { "TenantId", "tenant-1" }
        };

        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Headers).Returns(headers);

        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.Request).Returns(mockRequest.Object);

        return mockContext;
    }
}
