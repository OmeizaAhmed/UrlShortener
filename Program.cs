using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.Services.AddDbContext<UrlShortenerContext>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<TokenServices>();
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
       ClockSkew = TimeSpan.Zero
   };
});

builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", options => options.RequireRole("Admin")).AddPolicy("User", options => options.RequireRole("User"));


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "I am root :)");

app.MapControllers();

app.Run();