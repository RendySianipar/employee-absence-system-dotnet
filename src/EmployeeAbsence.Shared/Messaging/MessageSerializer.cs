using System.Text.Json;

namespace EmployeeAbsence.Shared.Messaging;

/// <summary>JSON settings used for every message on the bus (camelCase, case-insensitive read).</summary>
public static class MessageSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static byte[] Serialize<T>(T message) => JsonSerializer.SerializeToUtf8Bytes(message, Options);

    public static T? Deserialize<T>(ReadOnlySpan<byte> body) => JsonSerializer.Deserialize<T>(body, Options);
}
