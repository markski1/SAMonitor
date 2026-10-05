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
            options.OnRejected = async (context, _) =>
            {
                if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);

                await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests.").ExecuteAsync(context.HttpContext);
            };
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

        app.UseCors(x => x
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("Location", "Retry-After")
                    .SetIsOriginAllowed(_ => true));

        app.UseRateLimiter();

        app.MapControllers();

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
