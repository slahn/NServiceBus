using Events;

namespace SqlPublisher
{
    using System;
    using NServiceBus;

    class EventFromMsmqHandler : IHandleMessages<SomethingHappened>
    {
        public IBus Bus { get; set; }

        public void Handle(SomethingHappened message)
        {
            Console.WriteLine("Received event from MSMQ Publisher, publishing this event for all SQL Subscribers");
            Bus.Publish(message);
        }
    }
}
