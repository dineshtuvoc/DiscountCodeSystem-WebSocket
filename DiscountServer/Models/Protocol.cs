using System.Text.Json.Serialization;

namespace DiscountServer.Models;

// Enum to identify message type
public enum MessageType
{
    GenerateRequest,
    GenerateResponse,
    UseCodeRequest,
    UseCodeResponse,
    ErrorResponse
}

// Base class for all messages
public class BaseMessage
{
    public MessageType Type { get; set; }
    public string? Payload { get; set; }
}

public class GenerateRequest
{
    public ushort Count { get; set; }
    public byte Length { get; set; }
}

public class GenerateResponse
{
    public bool Result { get; set; }
}

public class UseCodeRequest
{
    public required string Code { get; set; }
}

public class UseCodeResponse
{
    public UseCodeResult Result { get; set; }
}

// Enum for UseCode response results
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UseCodeResult : byte
{
    Success = 0,
    NotFound = 1,
    AlreadyUsed = 2,
    InvalidFormat = 3
}

// General error response
public class ErrorResponse
{
    public required string Message { get; set; }
}