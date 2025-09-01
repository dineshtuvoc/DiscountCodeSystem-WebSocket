using DiscountServer.Handlers;
using DiscountServer.Services;
using Fleck;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;


var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();

Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "logs", "log-.txt"), rollingInterval: RollingInterval.Day)
               .CreateLogger();


try
{
    Log.Logger.Information("Application Starting...");

    var dbService = new DatabaseService(config);
    var codeGenService = new CodeGenerationService();
    var messageHandler = new MessageHandler(codeGenService, dbService);

    var server = new WebSocketServer("ws://0.0.0.0:8080");
    FleckLog.Level = LogLevel.Info;

    server.Start(socket =>
    {
        socket.OnOpen = () => Console.WriteLine($"Client connected: {socket.ConnectionInfo.ClientIpAddress}");
        socket.OnClose = () => Console.WriteLine($"Client disconnected: {socket.ConnectionInfo.ClientIpAddress}");

        // Fleck handles each client on a separate thread, and our handlers are async.
        socket.OnMessage = async message =>
        {
            Console.WriteLine($"Received: {message}");
            string response = await messageHandler.HandleMessageAsync(message);
            Console.WriteLine($"Sending: {response}");
            await socket.Send(response);
        };
    });

    Console.WriteLine("Discount Code Server started on ws://0.0.0.0:8080");
    Console.WriteLine("Press Enter to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Log.Logger.Error(ex, string.Empty);
}
