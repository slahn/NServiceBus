namespace NServiceBus.Unicast.Monitoring
{
    using IdGeneration;
    using MessageMutator;

    /// <summary>
    /// Mutator to set the related to header
    /// </summary>
    public class CausationMutator : Configurator, IMutateOutgoingTransportMessages 
    {
        /// <summary>
        /// The bus is needed to get access to the current message id
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(Headers.ConversationId))
                return;

            var conversationId = CombGuid.Generate().ToString();

            if (Bus.CurrentMessageContext != null)
            {
                transportMessage.Headers[Headers.RelatedTo] = Bus.CurrentMessageContext.Id;

                if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.ConversationId))
                    conversationId = Bus.CurrentMessageContext.Headers[Headers.ConversationId];
            }

            transportMessage.Headers[Headers.ConversationId] = conversationId;
        }

        /// <summary>
        /// Initializes 
        /// </summary>
        public override void RegisterTypes()
        {
            Register<CausationMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}