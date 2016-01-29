using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureQueueTest.Contracts;
using MassTransit;
using MassTransit.AzureServiceBusTransport;
using MassTransit.Log4NetIntegration;

namespace AzureQueueTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var instanceId = Guid.NewGuid();
            var bus = InitializeBus(instanceId);
            try
            {
                var userName = GetUserName();
                bus.Publish(new Join
                {
                    Guid = instanceId,
                    Name = userName
                });
                MessageLoop(instanceId, userName, bus);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                bus.Stop();
            }
        }

        private static void MessageLoop(Guid instanceId, string userName, IBus bus) {
            Console.WriteLine("/quit to exit");
            Console.WriteLine("_____________________________________________");

            while (true)
            {
                var line = Console.ReadLine();
                if (string.Equals(line, "/quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }
                bus.Publish(new Message
                {
                    Sender = instanceId,
                    From = userName,
                    Payload = line
                });
            }
        }

        private static string GetUserName()
        {
            string userName = null;
            while (string.IsNullOrEmpty(userName))
            {
                Console.Write("Name: ");
                userName = Console.ReadLine();
            }
            return userName.Trim();
        }

        private static IBusControl InitializeBus(Guid instanceId)
        {
            var bus = MassTransit.Bus.Factory.CreateUsingAzureServiceBus(sbc =>
            {
                sbc.UseLog4Net();

                var queueConnectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
                if(string.IsNullOrEmpty(queueConnectionString))
                    throw new ArgumentNullException(nameof(queueConnectionString));
                
                var host =
                    sbc.Host(queueConnectionString,
                             h => {});
                sbc.UseRetry(Retry.None);
                
                sbc.ReceiveEndpoint(host, $"AzureQueueTest_{instanceId}", ep =>
                {
                    ep.AutoDeleteOnIdle = TimeSpan.FromHours(1);
                    ep.Handler<Message>(mc =>
                    {
                        if (mc.Message.Sender != instanceId)
                        {
                            Console.WriteLine($"{DateTime.Now}: {mc.Message.From}: {mc.Message.Payload}");
                        }
                        return Task.FromResult(false);
                    });
                    ep.Handler<Join>(mc =>
                    {
                        if (mc.Message.Guid != instanceId)
                        {
                            Console.WriteLine($"{DateTime.Now}: {mc.Message.Name} has joined.");
                        }
                        return Task.FromResult(false);
                    });
                });
            });
            bus.Start();
            return bus;
        }
    }
}
