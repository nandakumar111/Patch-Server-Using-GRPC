using System.IdentityModel.Tokens.Jwt;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Receiver
{
    public class ReceiverInterceptor : Interceptor
    {

        private readonly ILogger<ReceiverInterceptor> _logger;

        public ReceiverInterceptor(ILogger<ReceiverInterceptor> logger)
        {
            _logger = logger;
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            InterceptorHandler<TRequest, TResponse>(MethodType.ClientStreaming, context);
            return base.ClientStreamingServerHandler(requestStream, context, continuation);
        }
        
        private void InterceptorHandler<TRequest, TResponse>(MethodType methodType, ServerCallContext context)
            where TRequest : class
            where TResponse : class
        {
            _logger.LogInformation($"Starting call. Type: {methodType}. Request: {typeof(TRequest)}. Response: {typeof(TResponse)}");
            
            if(!CheckMetadata(context.RequestHeaders, "authorization"))
            {
                context.GetHttpContext().Response.StatusCode = StatusCodes.Status401Unauthorized;
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Authorization header not found"), new Metadata());
            }

            bool CheckMetadata(Metadata headers, string key)
            {
                var headerValue = headers.SingleOrDefault(h => h.Key == key)?.Value;
                _logger.LogInformation($"{key}: {headerValue ?? "(unknown)"}");

                return !string.IsNullOrEmpty(headerValue) && ValidateToken(headerValue);
            }
        }
        
        private bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            
            var handler = new JwtSecurityTokenHandler();
            var decodedValue = handler.ReadJwtToken(token);

            var data = decodedValue.Claims.FirstOrDefault(x => x.Type.Equals("appId"));
            return data is {Value: "b2ec1188-2182-4789-a49e-1edc2a69c98b"};
        } 

    }
}
