using System;

namespace AzureQueueTest.Contracts
{
    public class Message
    {
        public string From { get; set; }
        public Guid Sender { get; set; }
        public string Payload { get; set; }
    }
}