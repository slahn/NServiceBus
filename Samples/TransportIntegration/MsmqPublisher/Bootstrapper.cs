using System;
using Events;

namespace MsmqPublisher
{
    using NServiceBus;

    /// <summary>
    /// Class to facilitate events to be published in order to verify that they are received by the Sql endpoint
    /// </summary>
    public class Bootstrapper : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }
        public void Start()
        {
            Console.WriteLine("Press Enter to publish the SomethingHappened Event");
            while (Console.ReadLine() != null)
            {
                Console.WriteLine("Event published");
                Bus.Publish(new SomethingHappened());
            }
        }

        public void Stop()
        {
        }
    }
}
