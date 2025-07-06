using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationService.Enums;

namespace NotificationService.Converters;

public class NotificationTypeConverter : JsonConverter<NotificationType>
{
    public override NotificationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            if (Enum.IsDefined(typeof(NotificationType), value))
            {
                return (NotificationType)value;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<NotificationType>(value, true, out var result))
            {
                return result;
            }
        }

        throw new JsonException($"Невозможно преобразовать значение к {nameof(NotificationType)}");
    }

    public override void Write(Utf8JsonWriter writer, NotificationType value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
} 