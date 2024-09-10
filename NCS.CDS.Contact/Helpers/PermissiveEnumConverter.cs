using Newtonsoft.Json;

namespace NCS.DSS.Contact.Helpers
{
    class PermissiveEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            Type type = IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;
            return type.IsEnum;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = IsNullableType(objectType);
            Type enumType = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;

            string[] names = Enum.GetNames(enumType);
            string enumText = reader.Value?.ToString();

            if (string.IsNullOrEmpty(enumText))
            {
                return GetDefaultValue(enumType, names);
            }

            if (int.TryParse(enumText, out int enumValue))
            {
                int[] values = (int[])Enum.GetValues(enumType);
                if (values.Contains(enumValue))
                {
                    return Enum.Parse(enumType, enumValue.ToString());
                }
            }

            string matchedName = names
                .Where(n => string.Equals(n, enumText, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (matchedName != null)
            {
                return Enum.Parse(enumType, matchedName);
            }

            return GetDefaultValue(enumType, names);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }

        private bool IsNullableType(Type t)
        {
            return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static object GetDefaultValue(Type enumType, string[] names)
        {
            string defaultName = names
                            .Where(n => string.Equals(n, "Unknown", StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault();

            if (defaultName == null)
            {
                return new JsonException();
            }

            return Enum.Parse(enumType, defaultName);
        }
    }
}