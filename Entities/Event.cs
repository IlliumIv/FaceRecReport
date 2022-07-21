using Newtonsoft.Json;
using Macroscop_FaceRecReport.Enums;

namespace Macroscop_FaceRecReport.Entities
{
    public class Event
    {
        private readonly dynamic _event;
        public DateTime Timestamp => (string)_event.Timestamp != string.Empty ? DateTime.Parse((string)_event.Timestamp) : DateTime.MinValue;
        public string ExternalId => _event.ExternalId;
        public string ChannelId => _event.ChannelId;
        public string ChannelName => _event.ChannelName;
        public bool IsIdentified => _event.IsIdentified;
        public string LastName => _event.lastName;
        public string FirstName => _event.firstName;
        public string Patronymic => _event.patronymic;
        public string[] Groups => (string)_event.groups != string.Empty ? ((string)_event.groups).Replace(" ", "").Split(',') : Array.Empty<string>();
        public string AdditionalInfo => _event.additionalInfo;
        public double Similarity => (string)_event.Similarity != string.Empty ? double.Parse((string)_event.Similarity) : double.MinValue;
        public int Age => _event.Age;
        public Gender Gender => _event.Gender;
        public string ImageBytes => _event.ImageBytes;
        public Emotion Emotion => Enum.Parse<Emotion>(string.Join("\n", (_event.Emotion).ToObject<string[]>()));
        public double EmotionConfidence => (string)_event.EmotionConfidence != string.Empty ? double.Parse((string)_event.EmotionConfidence) : double.MinValue;

        public Event(string jsonBody)
        {
            var parsedBody = JsonConvert.DeserializeObject<dynamic>(jsonBody);
            _event = parsedBody ?? "";
        }
    }
}
