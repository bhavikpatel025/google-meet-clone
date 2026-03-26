using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Infrastructure.Configuration;
using VideoCallApp.Infrastructure.Services;

namespace VideoCallApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT Configuration
        var jwtSettings = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSettings);
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        var jwtSettingsValue = jwtSettings.Get<JwtSettings>();
        var key = Encoding.UTF8.GetBytes(jwtSettingsValue?.Secret ?? "");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettingsValue?.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettingsValue?.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // For SignalR authentication
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IMeetingInvitationEmailService, MeetingInvitationEmailService>();

        return services;
    }
}
