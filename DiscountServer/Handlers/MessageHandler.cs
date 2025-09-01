using DiscountServer.Models;
using DiscountServer.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscountServer.Handlers;

public class MessageHandler
{
    private readonly CodeGenerationService _codeGenerationService;
    private readonly DatabaseService _databaseService;
    private readonly JsonSerializerOptions _jsonOptions;

    public MessageHandler(CodeGenerationService codeGenerationService, DatabaseService databaseService)
    {
        _codeGenerationService = codeGenerationService;
        _databaseService = databaseService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<string> HandleMessageAsync(string jsonMessage)
    {
        try
        {
            var baseMessage = JsonSerializer.Deserialize<BaseMessage>(jsonMessage, _jsonOptions);
            if (baseMessage?.Payload == null)
            {
                return CreateErrorResponse("Invalid message format.");
            }

            return baseMessage.Type switch
            {
                MessageType.GenerateRequest => await HandleGenerateRequestAsync(baseMessage.Payload),
                MessageType.UseCodeRequest => await HandleUseCodeRequestAsync(baseMessage.Payload),
                _ => CreateErrorResponse("Unknown message type.")
            };
        }
        catch (JsonException)
        {
            return CreateErrorResponse("Invalid JSON format.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            return CreateErrorResponse("An internal server error occurred.");
        }
    }

    private async Task<string> HandleGenerateRequestAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<GenerateRequest>(payload, _jsonOptions);

        if (request == null || (request.Length != 7 && request.Length != 8) || request.Count == 0 || request.Count > 2000)
        {
            var response = new GenerateResponse { Result = false };
            return CreateResponse(MessageType.GenerateResponse, response);
        }

        int totalInserted = 0;
        int requestedCount = request.Count;
        int maxAttempts = 5; // A safety break to prevent a potential infinite loop
        int attempt = 0;

        // Loop until we have successfully inserted the number of codes the user requested.
        while (totalInserted < requestedCount && attempt < maxAttempts)
        {
            // Calculate how many more codes we need to generate.
            int remainingNeeded = requestedCount - totalInserted;

            // Generate a new batch of potentially unique codes.
            var newBatch = _codeGenerationService.GenerateUniqueCodes((ushort)remainingNeeded, request.Length);

            // Try to insert them and find out how many were actually new.
            int insertedInThisBatch = await _databaseService.InsertNewCodesAsync(newBatch);

            // Add the count of newly inserted codes to our total.
            totalInserted += insertedInThisBatch;
            attempt++;
        }

        // The operation is only successful if we ended up with the exact number of codes requested.
        var finalResponse = new GenerateResponse { Result = (totalInserted == requestedCount) };
        return CreateResponse(MessageType.GenerateResponse, finalResponse);
    }

    private async Task<string> HandleUseCodeRequestAsync(string payload)
    {
        var request = JsonSerializer.Deserialize<UseCodeRequest>(payload, _jsonOptions);
        var response = new UseCodeResponse();

        if (string.IsNullOrWhiteSpace(request?.Code) || (request.Code.Length != 7 && request.Code.Length != 8))
        {
            response.Result = UseCodeResult.InvalidFormat;
        }
        else
        {
            var (exists, isUsed) = await _databaseService.GetCodeStatusAsync(request.Code);
            if (!exists)
            {
                response.Result = UseCodeResult.NotFound;
            }
            else if (isUsed)
            {
                response.Result = UseCodeResult.AlreadyUsed;
            }
            else
            {
                bool updated = await _databaseService.MarkCodeAsUsedAsync(request.Code);
                response.Result = updated ? UseCodeResult.Success : UseCodeResult.AlreadyUsed;
            }
        }

        return CreateResponse(MessageType.UseCodeResponse, response);
    }

    private string CreateResponse<T>(MessageType type, T payload)
    {
        var baseMessage = new BaseMessage
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload, _jsonOptions)
        };
        return JsonSerializer.Serialize(baseMessage, _jsonOptions);
    }

    private string CreateErrorResponse(string message)
    {
        var errorPayload = new ErrorResponse { Message = message };
        return CreateResponse(MessageType.ErrorResponse, errorPayload);
    }
}
