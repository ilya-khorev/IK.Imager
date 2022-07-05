using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace IK.Imager.Core
{
    /// <summary>
    /// </summary>
    public class ImageDownloadClient
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// </summary>
        /// <param name="httpClient"></param>
        public ImageDownloadClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Returns image memory stream by a given url.
        /// Returns null when the system was not able to download the image.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<MemoryStream> GetMemoryStream(string url)
        {
            MemoryStream imageStream = new MemoryStream();
            try
            {
                await using var stream = await _httpClient.GetStreamAsync(url);
                await stream.CopyToAsync(imageStream);
                imageStream.Position = 0;
            }
            catch (Exception)
            {
                return null;
            }

            return imageStream;
        }
    }
}