using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SAMonitor.Utils;

public static class WebServer
{
    private static string[] _args = [];
    private static Thread? _webServerThread;

    public static void Initialize(string[] args)
    {
        _args = args;
        _webServerThread = new Thread(RunWebServer);
        _webServerThread.Start();
    }

    private static void RunWebServer() {
        var builder = WebApplication.CreateBuilder(_args);

        builder.Services.AddControllers();
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

        app.Run();
    }
}
