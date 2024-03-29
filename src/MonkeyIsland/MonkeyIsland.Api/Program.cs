using System.Text;
using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using MonkeyIsland.Application.Configurations;
using MonkeyIsland.Application.Services;
using MonkeyIsland.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .Build();

builder.Services.Configure<MonkeyIslandConfiguration>(configuration.GetSection("MonkeyIslandConfiguration"));
builder.Services.AddSingleton<IResolveMysteryService, ResolveMysteryService>();
builder.Services.AddSingleton<IRequestClient, RequestClient>();

builder.Services.AddHttpClient<IRequestClient, RequestClient>("request-client");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/", async () =>
    {
        app.Logger.Log(LogLevel.Warning, "Starting mystery resolve.");
        
        var resolveService = app.Services.GetService<IResolveMysteryService>();
        
        var result = await resolveService!.ResolveMystery();
        
        app.Logger.Log(LogLevel.Warning, "Mystery was resolved: " + result.IsSuccess);
        
        return result.IsSuccess;
    })
    .WithOpenApi();

app.MapPost("/send", async (HttpContext context) =>
    {
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            app.Logger.Log(LogLevel.Warning, await reader.ReadToEndAsync());
        }
        
        return Result.Ok(true);
    })
    .WithOpenApi();

app.Run();