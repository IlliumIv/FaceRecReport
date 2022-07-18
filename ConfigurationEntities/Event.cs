using Macroscop_FaceRecReport.Enums;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Macroscop_FaceRecReport.ConfigurationEntities
{
    public class Event
    {
        private readonly dynamic _event;
        public DateTime Timestamp => DateTime.Parse((string)_event.Timestamp);
        public string ChannelId => _event.ChannelId;
        public string ChannelName => _event.ChannelName;
        public bool IsIdentified => _event.IsIdentified;
        public string LastName => _event.lastName;
        public string FirstName => _event.firstName;
        public string Patronymic => _event.patronymic;
        public string[] Groups => ((string)_event.groups).Replace(" ", "").Split(',');
        public string AdditionalInfo => _event.additionalInfo;
        public double Similarity => double.Parse((string)_event.Similarity);
        public int Age => _event.Age;
        public Gender Gender => _event.Gender;
        public string ImageBytes => _event.ImageBytes;
        public Emotion Emotion => Enum.Parse<Emotion>(string.Join("\n", (_event.Emotion).ToObject<string[]>()));
        public double EmotionConfidence => double.Parse((string)_event.EmotionConfidence);

        public Event(string jsonBody)
        {
            var parsedBody = JsonConvert.DeserializeObject<dynamic>(jsonBody);
            _event = parsedBody ?? "";
        }

        public (Image Image, IImageFormat Format) GetImageWithFormat()
        {
            byte[] imagebytes = Convert.FromBase64String((string)_event.ImageBytes);
            var image = Image.Load(imagebytes, out IImageFormat format);
            return (image, format);
        }

        public (Image Image, IImageFormat Format) GetImageWithFormat(int width, int height, bool keepAspectRatio = true)
        {
            if (keepAspectRatio)
            {
                width = width < height ? 0 : width;
                height = height < width ? 0 : height;
            }

            var imageWithFormat = GetImageWithFormat();
            imageWithFormat.Image.Mutate(i => i.Resize(width, height));
            return (imageWithFormat.Image, imageWithFormat.Format);
        }
    }
}
