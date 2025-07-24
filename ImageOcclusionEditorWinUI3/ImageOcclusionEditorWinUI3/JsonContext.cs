using System.Text.Json.Serialization;

namespace ImageOcclusionEditorWinUI3
{
    [JsonSerializable(typeof(string))]
    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
