using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public interface ICosmosDbClient
    {
        Task<Container> CreateImagesContainerIfNotExists();
    }
}