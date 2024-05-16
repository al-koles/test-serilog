using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .WriteTo
    .Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {CorrelationId}] {Message}{NewLine}{Exception}")
    .Enrich
    .FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole(opt =>
// {
//     opt.IncludeScopes = true;
//     opt.JsonWriterOptions = new JsonWriterOptions()
//     {
//         Indented = true
//     };
// });
builder.Logging.AddSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationIds);
    var correlationId = correlationIds.FirstOrDefault();
    if (string.IsNullOrEmpty(correlationId))
        correlationId = Guid.NewGuid().ToString();

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
};

app.MapGet("/weatherforecast", (ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("TestLogger");
        logger.LogInformation("Hahah hello world");

        var forecast =  Enumerable.Range(1, 5)
            .Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
