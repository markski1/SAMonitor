using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SAMonitor.Utils;

public static class WebServer
{
    public static async Task<WebApplication> InitializeAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers().AddApplicationPart(typeof(SAMonitor.Controllers.ApiController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = 40;
                limiterOptions.Window = TimeSpan.FromSeconds(60);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });
        });

        var app = builder.Build();

        Helpers.IsDevelopment = app.Environment.IsDevelopment();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRateLimiter();

        app.MapControllers();

        app.UseCors(x => x
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true));

        try
        {
            await app.StartAsync();
            Console.WriteLine("HTTP API started. Server refresh will run in the background.");
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }
}
