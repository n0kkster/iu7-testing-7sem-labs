using System.Net.Mime;

namespace Analyzer.Application.Interfaces.Providers;

public interface IImageProvider
{
    Task<ProcessedImageResult> CreateWebpAsync(Stream inputStream, 
                                               int width = 500, 
                                               int height = 500);
}

public record ProcessedImageResult(byte[] Data, string ContentType);