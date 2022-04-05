using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sender.Models;

namespace Sender
{
    public class Program
    {
        // chunk size (64KB)
        private const int ChunkSize = 1024 * 64;

        public static async Task Main(string[] args)
        {
            await FileTransferFunc();

            //CreateHostBuilder(args).Build().Run();
        }



        private static void ScriptRunFunc(CallInvoker interceptor, string fileSrc, string address)
        {
            var client = new Installer.PowershellManager.PowershellManagerClient(interceptor);

            // Check Administrator Role
            string script = @"$user = [Security.Principal.WindowsIdentity]::GetCurrent();(New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)";

            Console.WriteLine("Checking Administrator Role");
            var adminCheckRequest = new Installer.PowershellExecuteReq
            {
                Script = script,
            };

            var adminCheckRes = client.ExecutePsScript(adminCheckRequest);

            if (adminCheckRes.Message.Contains("False"))
            {
                Console.WriteLine("Please run as administrator");
                return;
            }

            Console.WriteLine("File installing..");
            var installReq = new Installer.PowershellExecuteReq
            {
                Commmand = "Start-Process"
            };
            installReq.Argument.Add(fileSrc);

            var installRes = client.ExecutePsScript(installReq);

            Console.WriteLine("Response Message : " + installRes.Message);

            Console.WriteLine($"Would you like to delete file in {address}? Y/N: ");
            var res = Console.ReadLine();
            if (res != null && (res.Equals("y", StringComparison.OrdinalIgnoreCase) || res.Equals("yes", StringComparison.OrdinalIgnoreCase)))
            {
                
                Console.WriteLine("File deleting..");
                var request = new Installer.PowershellExecuteReq
                {
                    Commmand = "Remove-Item",
                };

                request.Argument.Add(fileSrc);

                var deleteRes = client.ExecutePsScript(request);
                Console.WriteLine("Response Message : " + deleteRes.Message);
            }

        }

        private static async Task FileTransferFunc()
        {
            Console.WriteLine("Would you like to share file(s)? Y/N: ");
            var result = Console.ReadLine();
            if (result != null && (result.Equals("y", StringComparison.OrdinalIgnoreCase) || result.Equals("yes", StringComparison.OrdinalIgnoreCase)))
            {
                // Instruments Info
                var reader = new XmlSerializer(typeof(InstrumentInfo));
                var file = new StreamReader(@"InstrumentsInfo.xml");
                var overview = (InstrumentInfo)reader.Deserialize(file);
                file.Close();

                // Share Details
                var dataReader = new XmlSerializer(typeof(ShareDetails));
                var fileDetails = new StreamReader(@"ShareDetail.xml");
                var dataOverview = (ShareDetails)dataReader.Deserialize(fileDetails);
                fileDetails.Close();

                try
                {
                    if (dataOverview != null && overview != null)
                        foreach (var instrumentData in overview.Instruments.Instrument)
                        {
                            if (instrumentData.Primary) continue;

                            var clientUrl =
                                $@"{instrumentData.Protocol}://{instrumentData.IpAddress}:{instrumentData.Port}";

                            using var channel = GrpcChannel.ForAddress(clientUrl, new GrpcChannelOptions
                            {
                                HttpHandler = CreateHttpHandler(true)
                            });

                            var interceptor = channel.Intercept(new SenderInterceptor());

                            var client = new Fileshare.FileShare.FileShareClient(interceptor);

                            Console.WriteLine("Call initiated");
                            Console.WriteLine("Serial Number : " + instrumentData.SerialNo);
                            Console.WriteLine("Address : " + clientUrl);

                            var call = client.Transfer();

                            Console.WriteLine("Sending file metadata initiated");
                            var fileName = dataOverview.FileName;

                            await call.RequestStream.WriteAsync(
                                new Fileshare.FileTransferRequest
                                {
                                    Metadata = new Fileshare.MetaData
                                    {
                                        Filename = fileName,
                                        Path = dataOverview.ReceiverFilePath,
                                    }
                                }
                            );

                            var buffer = new byte[ChunkSize];
                            await using var readStream =
                                File.OpenRead(Path.Combine(dataOverview.SenderFilePath, fileName));

                            while (true)
                            {
                                var count = await readStream.ReadAsync(buffer);

                                if (count == 0)
                                {
                                    break;
                                }

                                await call.RequestStream.WriteAsync(
                                    new Fileshare.FileTransferRequest
                                    {
                                        Content = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count))
                                    }
                                );
                            }

                            Console.WriteLine("Request Completed");
                            await call.RequestStream.CompleteAsync();

                            var response = await call;
                            Console.WriteLine("File share status: " + response.Status);
                            Console.WriteLine("File share Message: " + response.Message);

                            Console.WriteLine($"Would you like to install file in {instrumentData.IpAddress}? Y/N: ");
                            var res = Console.ReadLine();
                            if (res != null && (res.Equals("y", StringComparison.OrdinalIgnoreCase) || res.Equals("yes", StringComparison.OrdinalIgnoreCase)))
                            {
                                ScriptRunFunc(interceptor, Path.Combine(dataOverview.ReceiverFilePath, fileName), instrumentData.IpAddress);
                            }
                        }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine("Status Code : " + ex.StatusCode);
                    Console.WriteLine("Status : " + ex.Status.StatusCode);
                    Console.WriteLine("Detail : " + ex.Status.Detail);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error : {e.ToString()}");
                }
            }

            Console.WriteLine("Shutting down");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        private static HttpClientHandler CreateHttpHandler(bool includeClientCertificate)
        {
            var handler = new HttpClientHandler();

            if (!includeClientCertificate) return handler;
            
            // Load client certificate
            // var certPath = Path.Combine("Certs", "certificate.pem");
            // var clientCertificate = new X509Certificate2(certPath, "1234567890");
            // handler.ClientCertificates.Add(clientCertificate);
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            return handler;
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
