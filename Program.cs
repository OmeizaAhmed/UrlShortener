var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.Services.AddControllers();


var app = builder.Build();

app.MapGet("/", () => "I am root :)");

app.MapControllers();

app.Run();