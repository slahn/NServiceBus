using System.Security.Cryptography;
using System.Text;
using NServiceBus.Faults;
using NServiceBus.Transports.SQLServer;

namespace SqlPublisher
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Unicast.Transport;

    class Bootstrapper : IWantToRunWhenBusStartsAndStops
    {
        public IPublishMessages Publisher { get; set; }
        public IManageMessageFailures FailureManager { get; set; }
        
        private MsmqDequeueStrategy receiver;
        public void Start()
        {
            receiver = new MsmqDequeueStrategy();
            var transactionSettings = new TransactionSettings()
            {
                IsTransactional = true,
                TransactionTimeout = TimeSpan.FromSeconds(30),
                IsolationLevel = IsolationLevel.ReadCommitted,
                MaxRetries = 1,
                DontUseDistributedTransactions = false,
                DoNotWrapHandlersExecutionInATransactionScope = false
            };

            receiver.Init(Address.Parse(ConfigurationManager.AppSettings["SqlBridgeAddress"]), transactionSettings, TryProcess, EndProcess);
            receiver.Start(1); // pass in the maximum concurrency level

        }

        public void Stop()
        {
        }

        bool TryProcess(TransportMessage message)
        {
            var eventTypes = new Type[] { Type.GetType(message.Headers["NServiceBus.EnclosedMessageTypes"]) };
            // Set the Id to a deterministic guid, as Sql message Ids are Guids and Msmq message ids are guid\nnnn
            var msmqId = message.Headers["NServiceBus.MessageId"];
            message.Headers["NServiceBus.MessageId"] = BuildDeterministicGuid(msmqId).ToString();
            Publisher.Publish(message, eventTypes);
            return true;
        }

        Guid BuildDeterministicGuid(string msmqMessageId)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                byte[] inputBytes = Encoding.Default.GetBytes(msmqMessageId);
                byte[] hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }

        void EndProcess(TransportMessage message, Exception ex)
        {
            if (ex == null) return;

            if (ex is AggregateException)
            {
                ex = ex.GetBaseException();
            }
            Console.WriteLine("Failed to process message due to Exception: {0}", ex.ToString());
            FailureManager.ProcessingAlwaysFailsForMessage(message, ex);
        }
    }
}
