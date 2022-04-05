using System;
using System.IO;
using System.Threading.Tasks;
using Fileshare;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Receiver.Services
{
    public class FileShareService : Fileshare.FileShare.FileShareBase
    {

        private readonly ILogger _logger;

        public FileShareService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileShareService>();
        }

        public override async Task<FileTransferResponse> Transfer(
            IAsyncStreamReader<FileTransferRequest> requestStream,
            ServerCallContext context
        )
        {
            Console.WriteLine("Receiver started");

            FileStream writeStream = null;

            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.Metadata != null)
                {
                    Directory.CreateDirectory(message.Metadata.Path);
                    writeStream = File.Create(Path.Combine(message.Metadata.Path, $@"{message.Metadata.Filename}"));
                }
                if (writeStream != null && message.Content != null)
                {
                    await writeStream.WriteAsync(message.Content.Memory);
                }
            }

            writeStream?.Close();

            return new FileTransferResponse
            {
                Status = Fileshare.Status.Success,
                Message = "Received successfully",
            };
        }
    }
}
