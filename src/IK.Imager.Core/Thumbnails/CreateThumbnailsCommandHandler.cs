using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Thumbnails;
using MediatR;

namespace IK.Imager.Core.Thumbnails;

public class CreateThumbnailsCommandHandler: IRequestHandler<CreateThumbnailsCommand>
{
    public Task<Unit> Handle(CreateThumbnailsCommand request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}