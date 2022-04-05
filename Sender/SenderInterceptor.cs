using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Sender
{
    public class SenderInterceptor : Interceptor
    {

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            InterceptorHandler(context.Method);
            AddMetadata(ref context);

            return continuation(context);
        }

        private void InterceptorHandler<TRequest, TResponse>(Method<TRequest, TResponse> method)
            where TRequest : class
            where TResponse : class
        {
            var initialColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Starting call. Type: {method.Type}. Request: {typeof(TRequest)}. Response: {typeof(TResponse)}");
            Console.ForegroundColor = initialColor;
        }

        private void AddMetadata<TRequest, TResponse>(ref ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class
            where TResponse : class
        {
            var headers = context.Options.Headers;

            // Call doesn't have a headers collection to add to.
            // Need to create a new context with headers for the call.
            if (headers == null)
            {
                headers = new Metadata();
                var options = context.Options.WithHeaders(headers);
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            // Add caller metadata to call headers
            headers.Add("authorization", $"{GenerateJwtWebToken()}");
        }

        private string GenerateJwtWebToken(string appId = "b2ec1188-2182-4789-a49e-1edc2a69c98b")
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("B23813DA7F066BE253E3BDFA41F87E010B585FF970FF54E428FDCC34B0AD1E50"));
            var cred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("appId", appId),
                new Claim("generatedAt", DateTime.UtcNow.ToString())
            };

            var token = new JwtSecurityToken("Sender", "Receiver", claims, DateTime.UtcNow, 
                DateTime.UtcNow.AddMinutes(30), cred);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
