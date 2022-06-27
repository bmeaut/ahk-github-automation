using System;
using Newtonsoft.Json;

namespace Ahk.GradeManagement.Data.Helper
{
    /// <summary>
    /// To be placed on derived classes where the base class is using cusom polymorphic deserialization.
    /// Based on https://stackoverflow.com/a/61515268
    /// </summary>
    public class DisabledJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => throw new NotImplementedException();
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
