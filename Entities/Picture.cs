using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace FaceRecReport.Entities;

public class Picture(byte[] imageBytes)
{
    public Image Image { get; private set; } = Image.Load(imageBytes);

    public IImageFormat Format { get; private set; } = Image.DetectFormat(imageBytes);

    public string Base64
    {
        get => Image.ToBase64String(Format).Split(',')[1];
        private set { }
    }
}
