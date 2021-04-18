using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcMedium.Server.Services
{
    public class CustomerService : Customer.CustomerBase
    {
        private readonly ILogger<CustomerService> _logger;
        private List<CustomerResponse> customerList;
        public CustomerService(ILogger<CustomerService> logger)
        {
            _logger = logger;
            this.SetCustomerList();
        }

        public override async Task<CustomerResponse> GetCustomerUnary(CustomerRequest request, ServerCallContext context)
        {
            CustomerResponse customerResponce = customerList.FirstOrDefault(customer => customer.Id == request.CustomerId);

            return await Task.FromResult(customerResponce);
        }

        public override async Task GetCustomerServerStream(Empty request, IServerStreamWriter<CustomerResponse> responseStream, ServerCallContext context)
        {
            foreach (var customer in customerList)
            {
                await responseStream.WriteAsync(customer);
                await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);
                //ServerCallContext içinde CancellationToken var.
                //Clientin işlemi iptal edip etmediğini anlamamıza yarayan bir token.5 saniye arayla stream ediyoruz
                //Dikkat return yok
                
            }
        }

        public override async Task<CustomersResponse> GetCustomersClientStream(IAsyncStreamReader<CustomerRequest> requestStream, ServerCallContext context)
        {
            CustomersResponse customersResponse = new CustomersResponse();

            await foreach (var customerRequest in requestStream.ReadAllAsync())
            {
                customersResponse.Customer.Add(customerList.FirstOrDefault(customer => customer.Id == customerRequest.CustomerId));
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            return customersResponse;
        }

        public override async Task GetCustomersBiDirectionalStream(IAsyncStreamReader<CustomerRequest> requestStream, IServerStreamWriter<CustomerResponse> responseStream, ServerCallContext context)
        {
            Console.WriteLine("Başladı");
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                CustomerRequest request = requestStream.Current;
                CustomerResponse customer= customerList.FirstOrDefault(customer => customer.Id == request.CustomerId);
                await responseStream.WriteAsync(customer);
                await Task.Delay(TimeSpan.FromSeconds(5));
                Console.WriteLine("Devam ediyor");
                //Dikkat return yok
            }
            Console.WriteLine("Bitti");
        }

        private void SetCustomerList()
        {
            customerList = new List<CustomerResponse>
            {
                new CustomerResponse { Id=1,Name="Süleyman Liman", Email="suleymanliman0@gmail.com",Adress="İstanbul",Age=28,IsActive=true },
                new CustomerResponse { Id=2,Name="Yunus Dönmez", Email="yunusdonmez@gmail.com",Adress="İstanbul",Age=26,IsActive=false },
                new CustomerResponse { Id=3,Name="Oğuzhan Mevsim", Email="oguzhanmevsim1@gmail.com",Adress="Kars",Age=25,IsActive=true },
            };
        }
    }
}
