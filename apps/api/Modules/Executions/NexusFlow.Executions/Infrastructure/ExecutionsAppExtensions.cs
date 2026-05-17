using System.Net;
using System.Text;
using System.Text.Json;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Workflows.Abstractions;

namespace NexusFlow.Executions.Infrastructure;

public static class ExecutionsAppExtensions
{
    public static IApplicationBuilder UseExecutionsDashboard(this WebApplication app, string path = "/hangfire")
    {
        return app.UseHangfireDashboard(path, new DashboardOptions
        {
            Authorization = [new DashboardAuthorizationFilter()],
        });
    }

    public static async Task ReregisterSchedulesAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var workflows = scope.ServiceProvider.GetRequiredService<IWorkflowReader>();
        var registrar = scope.ServiceProvider.GetRequiredService<IScheduleRegistrar>();

        var schedules = await workflows.GetActiveSchedulesAsync(ct).ConfigureAwait(false);
        foreach (var wf in schedules)
        {
            if (!TryReadCron(wf.TriggerConfigJson, out var cron)) continue;
            registrar.RegisterSchedule(wf.Id, cron);
        }
    }

    private static bool TryReadCron(string json, out string cron)
    {
        cron = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            if (!doc.RootElement.TryGetProperty("cron", out var value)) return false;
            if (value.ValueKind != JsonValueKind.String) return false;
            var s = value.GetString();
            if (string.IsNullOrWhiteSpace(s)) return false;
            cron = s;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

internal sealed class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();

        if (env.IsDevelopment()) return true;

        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        var expectedUser = config["Hangfire:DashboardUser"];
        var expectedPassword = config["Hangfire:DashboardPassword"];

        if (string.IsNullOrEmpty(expectedUser) || string.IsNullOrEmpty(expectedPassword))
        {
            return false;
        }

        var auth = http.Request.Headers["Authorization"].ToString();
        if (!auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return ChallengeAndDeny(http);
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Substring(6).Trim()));
            var colon = raw.IndexOf(':');
            if (colon < 0) return ChallengeAndDeny(http);

            var user = raw[..colon];
            var password = raw[(colon + 1)..];

            if (FixedTimeEquals(user, expectedUser) && FixedTimeEquals(password, expectedPassword))
            {
                return true;
            }
        }
        catch (FormatException)
        {
            // fall through to deny
        }

        return ChallengeAndDeny(http);
    }

    private static bool ChallengeAndDeny(HttpContext http)
    {
        http.Response.Headers["WWW-Authenticate"] = "Basic realm=\"NexusFlow.Hangfire\"";
        http.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return false;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
