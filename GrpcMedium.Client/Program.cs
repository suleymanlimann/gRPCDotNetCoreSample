using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using GrpcMedium.Server;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static GrpcMedium.Server.Customer;

namespace GrpcMedium.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Güvenilir olmayan/geçersiz sertifikayla gRPC hizmetini çağırma
            HttpClientHandler httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            //Güvenilir olmayan/geçersiz sertifikayla gRPC hizmetini çağırma

            GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001",
                new GrpcChannelOptions { HttpHandler = httpHandler });

            CustomerClient client = new CustomerClient(channel);

            #region Unary
            CustomerRequest request = new CustomerRequest { CustomerId = 1 };
            var callUnary = await client.GetCustomerUnaryAsync(request);
            Console.WriteLine($"{callUnary.Name} ,{callUnary.Email},{callUnary.Adress},{callUnary.Age}");
            #endregion

            #region Server Streaming
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            using (var callServerStream = client.GetCustomerServerStream(new Empty()))
            {
                while (await callServerStream.ResponseStream.MoveNext(cancellationTokenSource.Token))
                {
                    //herhangi bir yerde iptal tokene gönderebiliriz
                    //cancellationTokenSource.Cancel();

                    CustomerResponse currentCustomer = callServerStream.ResponseStream.Current;
                    Console.WriteLine($"{currentCustomer.Name} ,{currentCustomer.Email},{currentCustomer.Adress},{currentCustomer.Age}");
                }
            }
            #endregion

            #region Client Streaming

            using (var callClientStream = client.GetCustomersClientStream())
            {
                for (int i = 1; i <= 3; i++)
                {
                    await callClientStream.RequestStream.WriteAsync(new CustomerRequest { CustomerId = i });
                }
                await callClientStream.RequestStream.CompleteAsync();// steam'i bitirdik

                CustomersResponse response = await callClientStream;
                foreach (var customer in response.Customer)
                {
                    Console.WriteLine($"{customer.Name} ,{customer.Email},{customer.Adress},{customer.Age}");
                }
            }
            #endregion

            #region Bi-Directional Streaming
            CancellationTokenSource cancellationTokenSource2 = new CancellationTokenSource();
            using (var callBiDirectionalStream = client.GetCustomersBiDirectionalStream())
            {
                Console.WriteLine("Başla");
                var readTask = Task.Run(async () =>
                {
                     while (await callBiDirectionalStream.ResponseStream.MoveNext(cancellationTokenSource.Token))
                    {
                        Console.WriteLine(callBiDirectionalStream.ResponseStream.Current);
                    }
              
                });
              
                for (int i = 1; i <= 3; i++)
                {
                    await callBiDirectionalStream.RequestStream.WriteAsync(new CustomerRequest { CustomerId = i });
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                await callBiDirectionalStream.RequestStream.CompleteAsync();
                await readTask;
                Console.WriteLine("Bitir");
            }
            #endregion

            Console.ReadLine();
        }
    }
}
