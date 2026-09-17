using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

using Analyzer.Application.Interfaces.Providers;
using System.Net.Mime;

namespace Analyzer.Infrastructure.Providers;

public class ImageSharpImageProvider : IImageProvider
{
    public async Task<ProcessedImageResult> CreateWebpAsync(Stream inputStream, 
                                                            int width = 500, 
                                                            int height = 500)
    {
        using var image = await Image.LoadAsync(inputStream);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));

        using var outputStream = new MemoryStream();
        
        var encoder = new WebpEncoder
        {
            Quality = 80,
            FileFormat = WebpFileFormatType.Lossy
        };

        await image.SaveAsWebpAsync(outputStream, encoder);

        return new ProcessedImageResult(
            Data: outputStream.ToArray(),
            ContentType: MediaTypeNames.Image.Webp
        );
    }
}