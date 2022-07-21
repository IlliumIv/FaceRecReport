using System.Reflection;

namespace Macroscop_FaceRecReport.Enums
{
    public enum Gender
    {
        [StringValue("Неопределён")]
        Unknown,
        [StringValue("Женский")]
        Woman,
        [StringValue("Мужской")]
        Man
    }

    public enum Emotion
    {
        [StringValue("Негативные")]
        Sadness,
        [StringValue("Нейтральные")]
        Neutral,
        [StringValue("Позитивные")]
        Happiness
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StringValue : Attribute
    {
        private readonly string _value;
        public StringValue(string value) { _value = value; }
        public string Value { get { return _value; } }
    }

    public static class StringEnum
    {
        public static string? GetStringValue(Enum value)
        {
            string? output = null;
            Type type = value.GetType();
            FieldInfo? fi = type.GetField(value.ToString());
            if (fi == null) return null;
            if (fi.GetCustomAttributes(typeof(StringValue), false) is not StringValue[] attrs) return null;
            if (attrs.Length > 0) { output = attrs[0].Value; }
            return output;
        }
    }
}
