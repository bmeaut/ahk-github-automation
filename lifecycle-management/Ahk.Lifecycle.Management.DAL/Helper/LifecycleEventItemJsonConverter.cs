using Ahk.Lifecycle.Management.DAL.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ahk.Lifecycle.Management.DAL.Helper
{
    public class LifecycleEventItemJsonConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(LifecycleEvent);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var typeName = obj["$type"]?.Value<string>();
            return typeName switch
            {
                nameof(RepositoryCreateEvent) => obj.ToObject<RepositoryCreateEvent>(serializer),
                nameof(BranchCreateEvent) => obj.ToObject<BranchCreateEvent>(serializer),
                nameof(PullRequestEvent) => obj.ToObject<PullRequestEvent>(serializer),
                nameof(WorkflowRunEvent) => obj.ToObject<WorkflowRunEvent>(serializer),
                nameof(SetGradeEvent) => obj.ToObject<SetGradeEvent>(serializer),
                _ => throw new InvalidOperationException($"Unknown type name '{typeName}'"),
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException("This converter handles only deserialization, not serialization.");
    }
}
