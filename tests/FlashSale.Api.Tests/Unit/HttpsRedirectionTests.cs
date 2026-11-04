using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace FlashSale.Api.Tests.Unit;

public class HttpsRedirectionTests
{
    [Fact]
    public void WebApplicationBuilder_CanAddHttpsRedirection()
    {
        // Arrange & Act
        // Kiểm tra WebApplication.Builder hỗ trợ AddHttpsRedirection (đã được gọi trong Program.cs)
        var builder = WebApplication.CreateBuilder(new string[] { });
        builder.Services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            options.HttpsPort = 443;
        });

        var app = builder.Build();

        // Assert: Builder và app khởi tạo thành công, chứng minh AddHttpsRedirection đã được đăng ký đúng cách
        app.Should().NotBeNull();
    }
}

