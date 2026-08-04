using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UrlShortener.Services;
using UrlShortener.Common;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using UAParser;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Set the status code for rejected requests
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10, // Maximum number of requests allowed
            Window = TimeSpan.FromSeconds(10), // Time window for the rate limit
            SegmentsPerWindow = 10, // Number of segments in the time window
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst, // Order of processing queued requests
            QueueLimit = 0 // No queueing; reject requests immediately when limit is reached
        });
    });
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? throw new Exception("REDIS CONNECTION STRING CAN NOT BE EMPTY");
    options.InstanceName = "UrlShortenerCache_";
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<UrlShortenerContext>(options =>
{
    string connectionString = $"Server=localhost;Database=UrlShortenerDB;User=root;Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 46));
    options.UseMySql(connectionString, serverVersion);
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true; // Set to true if you don't want to require special characters
    options.Password.RequiredLength = 8; // Minimum password length
    // lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Lockout duration
    options.Lockout.MaxFailedAccessAttempts = 5; // Maximum failed attempts before lockout
    options.Lockout.AllowedForNewUsers = true; // Allow lockout for new users
}).AddEntityFrameworkStores<UrlShortenerContext>();


builder.Services.AddSingleton(provider => Parser.GetDefault());
builder.Services.AddScoped<TokenServices>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAnalyticService, AnalyticService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuer = true,
       ValidateAudience = true,
       ValidateLifetime = true,
       ValidateIssuerSigningKey = true,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new Exception("JWT SECRET KEY CAN NOT BE EMPTY"))),
       ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
       ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
       NameClaimType = ClaimTypes.NameIdentifier,
       RoleClaimType = ClaimTypes.Role,
       ClockSkew = TimeSpan.Zero
   };
});

builder.Services.AddAuthorizationBuilder()
.SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build())
.AddPolicy("Admin", new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .RequireRole("Admin")
    .Build())
.AddPolicy("User", new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .RequireRole("User")
    .Build());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define the Bearer security scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token only."
    });

    // Apply the security scheme globally to all endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});




var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => ApiResponse<string>.SuccessResponse("Welcome to the URL Shortener API", "API is running successfully"));

app.MapControllers();

app.Run();

internal class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.FailureResponse("Internal Server Error", "Internal server error occurred. Please try again later."));
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}


