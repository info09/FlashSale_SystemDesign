using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class GrafanaDashboardEndToEndTest : IAsyncLifetime
{
    private readonly IContainer _grafanaContainer;
    private readonly HttpClient _httpClient = new();

    public GrafanaDashboardEndToEndTest()
    {
        var repoRoot = FindRepoRoot();
        var provisioningPath = Path.Combine(repoRoot, "monitoring", "grafana", "provisioning");
        var dashboardsPath = Path.Combine(repoRoot, "monitoring", "grafana", "dashboards");

        _grafanaContainer = new ContainerBuilder()
            .WithImage("grafana/grafana:latest")
            .WithPortBinding(3000, true)
            .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
            .WithBindMount(provisioningPath, "/etc/grafana/provisioning")
            .WithBindMount(dashboardsPath, "/var/lib/grafana/dashboards")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(3000).ForPath("/api/health")))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _grafanaContainer.StartAsync();

        var port = _grafanaContainer.GetMappedPublicPort(3000);
        _httpClient.BaseAddress = new Uri($"http://localhost:{port}");
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:admin"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _grafanaContainer.StopAsync();
        await _grafanaContainer.DisposeAsync();
    }

    [Fact]
    public async Task Grafana_AfterDeploy_ShouldProvisionDatasourceAndFlashSaleDashboardWithAllMetrics()
    {
        // Datasource provisioned
        var datasourcesResponse = await _httpClient.GetAsync("/api/datasources");
        datasourcesResponse.IsSuccessStatusCode.Should().BeTrue();
        var datasourcesJson = await datasourcesResponse.Content.ReadAsStringAsync();
        using (var datasourcesDoc = JsonDocument.Parse(datasourcesJson))
        {
            var datasources = datasourcesDoc.RootElement;
            datasources.ValueKind.Should().Be(JsonValueKind.Array);
            datasources.EnumerateArray().Any(ds =>
                ds.GetProperty("uid").GetString() == "prometheus" &&
                ds.GetProperty("name").GetString() == "Prometheus" &&
                ds.GetProperty("type").GetString() == "prometheus" &&
                ds.GetProperty("url").GetString() == "http://flashsale-prometheus:9090"
            ).Should().BeTrue("Prometheus datasource should be provisioned");
        }

        // Dashboard discoverable
        var searchResponse = await _httpClient.GetAsync("/api/search?type=dash-db&query=FlashSale%20Overview");
        searchResponse.IsSuccessStatusCode.Should().BeTrue();
        var searchJson = await searchResponse.Content.ReadAsStringAsync();
        using (var searchDoc = JsonDocument.Parse(searchJson))
        {
            searchDoc.RootElement.EnumerateArray().Any(d =>
                d.GetProperty("uid").GetString() == "flashsale-overview" &&
                d.GetProperty("title").GetString() == "FlashSale Overview"
            ).Should().BeTrue("FlashSale Overview dashboard should be provisioned");
        }

        // Dashboard contains all required metrics and jobs
        var dashboardResponse = await _httpClient.GetAsync("/api/dashboards/uid/flashsale-overview");
        dashboardResponse.IsSuccessStatusCode.Should().BeTrue();
        var dashboardJson = await dashboardResponse.Content.ReadAsStringAsync();
        dashboardJson.Should().Contain("flash_sale_p99latency");
        dashboardJson.Should().Contain("flash_sale_errorrate");
        dashboardJson.Should().Contain("flash_sale_queuedepth");
        dashboardJson.Should().Contain("flash_sale_dlqsize");
        dashboardJson.Should().Contain("flash_sale_inventoryremaining");
        dashboardJson.Should().Contain("flashsale-api");
        dashboardJson.Should().Contain("flashsale-worker");

        using (var dashboardDoc = JsonDocument.Parse(dashboardJson))
        {
            var panels = dashboardDoc.RootElement.GetProperty("dashboard").GetProperty("panels");
            panels.GetArrayLength().Should().BeGreaterThanOrEqualTo(5, "dashboard should display all 5 metrics");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var dashboardPath = Path.Combine(dir.FullName, "monitoring", "grafana", "dashboards", "flashsale-overview.json");
            var slnPath = Path.Combine(dir.FullName, "FlashSale.sln");
            if (File.Exists(dashboardPath) && File.Exists(slnPath))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing monitoring/grafana dashboard files.");
    }
}
