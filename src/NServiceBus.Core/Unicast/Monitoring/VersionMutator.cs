namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;

    public class VersionMutator : Configurator, IMutateOutgoingTransportMessages 
    {
        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.NServiceBusVersion] = NServiceBusVersion.Version;
        }
     
        /// <summary>
        /// Initializer
        /// </summary>
        public override void RegisterTypes()
        {
            Register<VersionMutator>(DependencyLifecycle.SingleInstance);
        }
    }
}