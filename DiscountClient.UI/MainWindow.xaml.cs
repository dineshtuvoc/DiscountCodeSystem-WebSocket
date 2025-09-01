using DiscountClient.UI.Models;
using DiscountClient.UI.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace DiscountClient.UI;

public partial class MainWindow : Window
{
    private readonly WebSocketClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MainWindow()
    {
        InitializeComponent();
        _client = new WebSocketClient();
        _client.MessageReceived += OnMessageReceived;

        // Automatically connect when the window is loaded
        this.Loaded += MainWindow_Loaded;
        // Ensure disconnection when the window is closed
        this.Closing += MainWindow_Closing;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _client.ConnectAsync("ws://localhost:8080");
            Log("Connected to server.");
        }
        catch (Exception ex)
        {
            Log($"Failed to connect: {ex.Message}");
        }
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        await _client.DisconnectAsync();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ushort.TryParse(CountTextBox.Text, out ushort count) || count == 0 || count > 2000)
        {
            Log("Error: Invalid count. Must be a number between 1 and 2000.");
            return;
        }

        if (!byte.TryParse(LengthTextBox.Text, out byte length) || (length != 7 && length != 8))
        {
            Log("Error: Invalid length. Must be 7 or 8.");
            return;
        }

        var request = new GenerateRequest { Count = count, Length = length };
        await _client.SendMessageAsync(MessageType.GenerateRequest, request);
        Log("Sent generate request...");
    }

    private async void UseCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var code = CodeToUseTextBox.Text;
        if (string.IsNullOrWhiteSpace(code))
        {
            Log("Error: Code cannot be empty.");
            return;
        }

        var request = new UseCodeRequest { Code = code };
        await _client.SendMessageAsync(MessageType.UseCodeRequest, request);
        Log($"Sent request to use code: {code}...");
    }

    // This method is called when a message is received from the server
    private void OnMessageReceived(string message)
    {
        // We need to update the UI from the UI thread
        Dispatcher.Invoke(() =>
        {
            try
            {
                var baseMessage = JsonSerializer.Deserialize<BaseMessage>(message, _jsonOptions);
                if (baseMessage?.Payload == null)
                {
                    Log($"Received raw message: {message}");
                    return;
                }

                string formattedResponse = $"Received {baseMessage.Type}:" + Environment.NewLine;
                switch (baseMessage.Type)
                {
                    case MessageType.GenerateResponse:
                        var genResponse = JsonSerializer.Deserialize<GenerateResponse>(baseMessage.Payload, _jsonOptions);
                        formattedResponse += $"  -> Success: {genResponse?.Result}";
                        break;
                    case MessageType.UseCodeResponse:
                        var useResponse = JsonSerializer.Deserialize<UseCodeResponse>(baseMessage.Payload, _jsonOptions);
                        formattedResponse += $"  -> Result: {useResponse?.Result}";
                        break;
                    case MessageType.ErrorResponse:
                        var errResponse = JsonSerializer.Deserialize<ErrorResponse>(baseMessage.Payload, _jsonOptions);
                        formattedResponse += $"  -> Error: {errResponse?.Message}";
                        break;
                    default:
                        formattedResponse += message;
                        break;
                }
                Log(formattedResponse);
            }
            catch (JsonException)
            {
                // Handle non-JSON messages or connection status messages
                Log(message);
            }
        });
    }

    private void Log(string message)
    {
        // Append new message at the top
        ResponseTextBox.Text = $"{DateTime.Now:T}: {message}\n{ResponseTextBox.Text}";
    }
}