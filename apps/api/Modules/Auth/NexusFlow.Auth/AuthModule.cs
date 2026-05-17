using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Auth.Application;
using NexusFlow.Auth.Application.Security;
using NexusFlow.Auth.Endpoints;
using NexusFlow.Auth.Infrastructure;
using NexusFlow.Shared.Web;

namespace NexusFlow.Auth;

public static class AuthModule
{
    public const string Name = "auth";

    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AuthDbContext>((sp, opt) =>
        {
            var cs = configuration.GetConnectionString("Postgres")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");
            opt.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "auth"));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<AuthService>();

        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<RefreshRequest>, RefreshRequestValidator>();
        services.AddSingleton<IValidator<LogoutRequest>, LogoutRequestValidator>();

        services.AddScoped<ValidationFilter<RegisterRequest>>();
        services.AddScoped<ValidationFilter<LoginRequest>>();
        services.AddScoped<ValidationFilter<RefreshRequest>>();
        services.AddScoped<ValidationFilter<LogoutRequest>>();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuth();
        return app;
    }
}
