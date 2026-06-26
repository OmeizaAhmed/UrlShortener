var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();


var app = builder.Build();

app.MapGet("/", () => "I am root :)");
app.MapControllers();

app.Run();