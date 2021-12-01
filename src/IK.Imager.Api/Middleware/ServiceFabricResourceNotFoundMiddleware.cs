using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Middleware
{
    /// <summary>
    /// Middleware, using which a special HTTP header is added to a response in case of 404 status code.
    ///
    /// https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reverseproxy
    /// HTTP 404 response can have two distinct meanings:
    /// Case #1: The service address is correct, but the resource that the user requested does not exist.
    /// Case #2: The service address is incorrect, and the resource that the user requested might exist on a different node.
    ///    
    /// By default, the reverse proxy assumes case #2 and attempts to resolve and issue the request again
    ///    
    /// To indicate case #1 to the reverse proxy, the service should return the following HTTP response header:
    /// X-ServiceFabric : ResourceNotFound
    /// </summary>
    public class ServiceFabricResourceNotFoundMiddleware
    {
        private readonly RequestDelegate _next;

        private const string ServiceFabricHeader = "X-ServiceFabric";
        private const string ServiceFabricNotFound = "ResourceNotFound";

        /// <summary>
        /// Middleware using which a special header is added to response in case of 404 status code.
        /// </summary>
        /// <param name="next"></param>
        public ServiceFabricResourceNotFoundMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Checking for a response code 404 and adding a special X-ServiceFabric header in this case
        /// </summary>
        /// <param name="context"></param>
        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;
                if (httpContext.Response.StatusCode == 404 && !httpContext.Response.Headers.ContainsKey(ServiceFabricHeader))
                    httpContext.Response.Headers.Add(ServiceFabricHeader, ServiceFabricNotFound);

                return Task.CompletedTask;
            }, context);
            
            await _next.Invoke(context);
        }
    }
}