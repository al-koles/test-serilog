using System.Diagnostics;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo
    // .Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {CorrelationId}] {Message}{NewLine}{Exception}")
    .Console(new CompactJsonFormatter())
    .Enrich
    .FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationIds);
    var correlationId = correlationIds.FirstOrDefault();
    if (string.IsNullOrEmpty(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = correlationId;
    }

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});
app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();

    await next();

    stopwatch.Stop();
    using var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("RequestLoggingMiddleware");
    using (logger.BeginScope(new Dictionary<string, object>
           {
               ["ElapsedMilliseconds"] = stopwatch.Elapsed.TotalMilliseconds,
           }))
    {
        logger.LogInformation("Request finished is ms FFFFFFFFFFFF");
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

await app.RunAsync();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
