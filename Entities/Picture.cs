using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Macroscop_FaceRecReport.Entities
{
    public class Picture
    {
        public Image Image { get; private set; }
        public IImageFormat Format { get; private set; }
        public string Base64
        {
            get { return Image.ToBase64String(Format).Split(',')[1]; }
            private set { }
        }
        public Picture(byte[] imageBytes)
        {
            Image = Image.Load(imageBytes, out var format);
            Format = format;
        }
    }
}
