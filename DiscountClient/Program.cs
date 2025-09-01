using DiscountClient.Models;
using Serilog;
using Serilog.Configuration;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly ClientWebSocket Ws = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };


    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Information()
             .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "logs", "log-.txt"), rollingInterval: RollingInterval.Day)
             .CreateLogger();

        Log.Logger.Information("Discount Client Started....");

        Console.Title = "Discount Code Client";
        var serverUri = new Uri("ws://localhost:8080");

        try
        {
            await Ws.ConnectAsync(serverUri, CancellationToken.None);
            Console.WriteLine($"Connected to {serverUri}");

            // Start a task to listen for messages from the server
            var receiveTask = Task.Run(ReceiveMessages);

            await RunUserInterface();

            await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
            await receiveTask;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, string.Empty);
        }
        finally
        {
            Ws.Dispose();
            Log.Logger.Information("Connection closed. Press any key to exit.");
            Console.ReadKey();
        }
    }

    private static async Task RunUserInterface()
    {
        while (Ws.State == WebSocketState.Open)
        {
            Console.WriteLine("\n--- Main Menu ---");
            Console.WriteLine("1. Generate new codes");
            Console.WriteLine("2. Use a code");
            Console.WriteLine("3. Exit");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    await HandleGenerateCodes();
                    break;
                case "2":
                    await HandleUseCode();
                    break;
                case "3":
                    return; // Exit the loop and close connection
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private static async Task HandleGenerateCodes()
    {
        Console.Write("How many codes to generate? (max 2000): ");
        if (!ushort.TryParse(Console.ReadLine(), out ushort count) || count > 2000)
        {
            Console.WriteLine("Invalid count.");
            return;
        }

        Console.Write("Code length (7 or 8): ");
        if (!byte.TryParse(Console.ReadLine(), out byte length) || (length != 7 && length != 8))
        {
            Console.WriteLine("Invalid length.");
            return;
        }

        var request = new GenerateRequest { Count = count, Length = length };
        await SendMessageAsync(MessageType.GenerateRequest, request);
        Console.WriteLine("Generation request sent. Waiting for response...");
    }

    private static async Task HandleUseCode()
    {
        Console.Write("Enter code to use: ");
        string? code = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Code cannot be empty.");
            return;
        }

        var request = new UseCodeRequest { Code = code };
        await SendMessageAsync(MessageType.UseCodeRequest, request);
        Console.WriteLine("Use code request sent. Waiting for response...");
    }

    private static async Task SendMessageAsync<T>(MessageType type, T payload)
    {
        var message = new BaseMessage
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload, JsonOptions)
        };
        var jsonMessage = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(jsonMessage);
        await Ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 4];
        while (Ws.State == WebSocketState.Open)
        {
            try
            {
                var result = await Ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleServerResponse(message);
                }
            }
            catch (WebSocketException)
            {
                // Connection closed unexpectedly
                break;
            }
        }
    }

    private static void HandleServerResponse(string jsonMessage)
    {
        try
        {
            var baseMessage = JsonSerializer.Deserialize<BaseMessage>(jsonMessage, JsonOptions);
            if (baseMessage?.Payload == null) return;

            Console.WriteLine("\n--- Server Response ---");
            switch (baseMessage.Type)
            {
                case MessageType.GenerateResponse:
                    var genResponse = JsonSerializer.Deserialize<GenerateResponse>(baseMessage.Payload, JsonOptions);
                    Console.WriteLine(genResponse!.Result ? "Codes generated successfully." : "Code generation failed.");
                    break;

                case MessageType.UseCodeResponse:
                    var useResponse = JsonSerializer.Deserialize<UseCodeResponse>(baseMessage.Payload, JsonOptions);
                    Console.WriteLine($"Code usage result: {useResponse!.Result}");
                    break;

                case MessageType.ErrorResponse:
                    var errResponse = JsonSerializer.Deserialize<ErrorResponse>(baseMessage.Payload, JsonOptions);
                    Console.WriteLine($"Server Error: {errResponse!.Message}");
                    break;

                default:
                    Console.WriteLine($"Received unknown message type: {baseMessage.Type}");
                    break;
            }
            Console.Write("-----------------------\nSelect an option: ");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing server message: {ex.Message}");
        }
    }
}