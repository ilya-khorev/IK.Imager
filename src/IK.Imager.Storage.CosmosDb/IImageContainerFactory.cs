using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace IK.Imager.Storage.CosmosDb
{
    public interface IImageContainerFactory
    {
        Task<Container> CreateImagesContainerIfNotExists();
    }
}
