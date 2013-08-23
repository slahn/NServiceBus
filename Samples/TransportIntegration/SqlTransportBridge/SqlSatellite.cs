using System;

namespace SqlTransportBridge
{
    using NServiceBus;
    using NServiceBus.Satellites;
    using NServiceBus.Transports.SQLServer;

    /// <summary>
    /// This satellite will handle all of the events received from MSMQ subscribers and forward
    /// these events over to the Sql endpoint which will inturn publish these events for all of the 
    /// Sql subscribers.
    /// </summary>
    class SqlSatellite : ISatellite
    {
        private SqlServerMessageSender sqlMessageSender;
       
        public bool Disabled { get { return false; }}
            
        public bool Handle(TransportMessage message)
        {
            Console.WriteLine("Received event from MSMQ publisher -- forwarding it to SqlPublisher");
            sqlMessageSender.Send(message, Address.Parse("SqlPublisher"));
            return true;
        }

        public Address InputAddress
        {
            get { return Address.Local; }
        }

        public void Start()
        {
            sqlMessageSender = new SqlServerMessageSender()
            {
                ConnectionString = @"Data Source=.\SELENA;Initial Catalog=NServiceBus;Integrated Security=True"
            };
            Console.WriteLine("SqlBridge has now started -- waiting for events");
     
        }

        public void Stop() {}        
    }
}
