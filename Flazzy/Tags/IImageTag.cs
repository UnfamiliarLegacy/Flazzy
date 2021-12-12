using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Flazzy.Tags;

public interface IImageTag
{
    /// <summary>
    ///     Sets the tag to the given image. Method does not dispose the image.
    /// </summary>
    /// <param name="image">Image to set.</param>
    void SetImage(Image<Rgba32> image);
    
    /// <summary>
    ///     Returns the image in this tag as a <see cref="Image"/>.
    ///     You are supposed to dispose the instance when you are finished with it.
    /// </summary>
    /// <returns>The image inside this tag.</returns>
    Image<Rgba32> GetImage();
}