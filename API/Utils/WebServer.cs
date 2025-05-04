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

        var app = builder.Build();

        Helpers.IsDevelopment = app.Environment.IsDevelopment();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();

        app.UseCors(x => x
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials());

        app.Run();
    }
}
