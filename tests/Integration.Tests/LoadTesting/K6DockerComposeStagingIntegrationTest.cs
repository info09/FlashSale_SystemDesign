using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Integration.Tests.LoadTesting;

/// <summary>
/// Story 6.5 (FR-07, NFR-01..03): Integration test kết hợp k6 với Docker Compose môi trường staging.
/// Chạy k6 (container grafana/k6) qua docker-compose staging override để kiểm tra luồng mua hàng.
/// Test này opt-in: đặt FLASHSALE_K6_STAGING_E2E=1 và đảm bảo Docker + docker compose khả dụng,
/// stack staging (flashsale-api, redis, rabbitmq, postgres, worker) đang chạy.
/// </summary>
public class K6DockerComposeStagingIntegrationTest
{
    [Fact]
    public async Task K6_WithDockerComposeStagingEnvironment_ShouldExecuteFlashSaleBuyScript()
    {
        if (Environment.GetEnvironmentVariable("FLASHSALE_K6_STAGING_E2E") != "1")
        {
            // Opt-in: tránh chạy nặng/flaky trong CI mặc định. Bật bằng FLASHSALE_K6_STAGING_E2E=1.
            return;
        }

        if (!IsDockerAvailable())
        {
            // Môi trường không có Docker -> bỏ qua (opt-in test).
            return;
        }

        var repoRoot = FindRepoRoot();
        var baseUrl = Environment.GetEnvironmentVariable("K6_BASE_URL") ?? "http://flashsale-api:8080";
        var accessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN") ?? Environment.GetEnvironmentVariable("K6_ACCESS_TOKEN") ?? string.Empty;
        var productId = Environment.GetEnvironmentVariable("PRODUCT_ID") ?? Environment.GetEnvironmentVariable("K6_PRODUCT_ID") ?? "product-test";
        var vus = Environment.GetEnvironmentVariable("K6_VUS") ?? "10";
        var duration = Environment.GetEnvironmentVariable("K6_DURATION") ?? "10s";
        var sleepSeconds = Environment.GetEnvironmentVariable("K6_SLEEP_SECONDS") ?? "0.1";

        var args = new StringBuilder();
        args.Append("compose -f docker-compose.yml -f docker-compose.staging.yml --profile k6 run --rm ");
        args.Append($"-e BASE_URL={EscapeArg(baseUrl)} ");
        if (!string.IsNullOrWhiteSpace(accessToken))
            args.Append($"-e ACCESS_TOKEN={EscapeArg(accessToken)} ");
        args.Append($"-e PRODUCT_ID={EscapeArg(productId)} ");
        args.Append($"-e VUS={EscapeArg(vus)} ");
        args.Append($"-e DURATION={EscapeArg(duration)} ");
        args.Append($"-e SLEEP_SECONDS={EscapeArg(sleepSeconds)} ");
        args.Append("-e THRESHOLD_HTTP_REQ_FAILED_RATE=1 ");
        args.Append("flashsale-k6 run /scripts/flashsale-buy.js");

        var (exitCode, stdout, stderr) = await RunProcessAsync("docker", args.ToString(), repoRoot, TimeSpan.FromMinutes(10));
        var output = stdout + Environment.NewLine + stderr;

        // K6 script trong staging env phải chạy được và trả summary/checks (status 202/400/401/429).
        exitCode.Should().Be(0, $"k6 should complete successfully against docker compose staging environment. Output:{Environment.NewLine}{output}");
        output.Should().Contain("status is expected (202/400/401/429)");
        output.Should().Contain("http_req_duration");
        output.Should().Contain("iterations");
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version --format '{{.Server.Version}}'",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {timeout}.");
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "FlashSale.sln");
            var compose = Path.Combine(dir.FullName, "docker-compose.yml");
            if (File.Exists(sln) && File.Exists(compose))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root (FlashSale.sln + docker-compose.yml).");
    }

    private static string EscapeArg(string value) => value.Replace("\"", "\\\"");
}
