using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using NexusFlow.Auth;
using NexusFlow.Auth.Infrastructure;
using NexusFlow.Executions;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Integrations;
using NexusFlow.Integrations.Infrastructure;
using NexusFlow.Shared.Time;
using NexusFlow.Workflows;
using NexusFlow.Workflows.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console());

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddProblemDetails();

var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services
    .AddDataProtection()
    .SetApplicationName("nexusflow")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Trust X-Forwarded-* headers from Railway/Fly/Vercel proxies so request scheme/host is correct.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddNexusFlowAuthentication(builder.Configuration);

builder.Services
    .AddAuthModule(builder.Configuration)
    .AddWorkflowsModule(builder.Configuration)
    .AddExecutionsModule(builder.Configuration, builder.Environment)
    .AddIntegrationsModule(builder.Configuration);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    healthChecks.AddNpgSql(postgresConnection, name: "postgres", tags: ["ready"]);
}

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapGet("/", () => Results.Ok(new { name = "NexusFlow.Api", version = "0.1.0", status = "ok" }))
   .AllowAnonymous();

app
    .MapAuthEndpoints()
    .MapWorkflowsEndpoints()
    .MapExecutionsEndpoints()
    .MapIntegrationsEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.MigrateAuthAsync().ConfigureAwait(false);
    await app.MigrateWorkflowsAsync().ConfigureAwait(false);
    await app.MigrateExecutionsAsync().ConfigureAwait(false);
    await app.MigrateIntegrationsAsync().ConfigureAwait(false);

    app.UseExecutionsDashboard();
    await app.ReregisterSchedulesAsync().ConfigureAwait(false);
}

app.Run();

public partial class Program;
