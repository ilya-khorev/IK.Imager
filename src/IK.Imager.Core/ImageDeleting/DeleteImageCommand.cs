using MediatR;
// ReSharper disable InconsistentNaming

#pragma warning disable 8632
#pragma warning disable 1591

namespace IK.Imager.Core.ImageDeleting;

public record DeleteImageCommand(string ImageId, string? ImageName, string[] ThumbnailNames) : IRequest
{
    public void Deconstruct(out string ImageId, out string? ImageName, out string[] ThumbnailNames)
    {
        ImageId = this.ImageId;
        ImageName = this.ImageName;
        ThumbnailNames = this.ThumbnailNames;
    }

    public override string ToString()
    {
        return $"ImageId = {ImageId}, ImageName = {ImageName}, ThumbnailNames = {string.Join(",", ThumbnailNames)}";
    }
}
