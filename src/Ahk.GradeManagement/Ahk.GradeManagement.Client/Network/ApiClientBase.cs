using System.Text.Json;

namespace Ahk.GradeManagement.Client.Network;

public abstract class ApiClientBase
{
    protected static void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}
