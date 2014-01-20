namespace NServiceBus
{
    using System;
    using Gateway.Deduplication;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;

    class ConfigureGateway : Configurator
    {
        public override void RegisterTypes()
        {
            if (Bootstrapper["gateway.persistence"] != null)
            {
                Register((Type) Bootstrapper["gateway.persistence"], DependencyLifecycle.SingleInstance);
            }

            if (Bootstrapper["gateway.deduplication"] != null)
            {
                Register((Type) Bootstrapper["gateway.deduplication"], DependencyLifecycle.SingleInstance);
            }
        }
    }

    public abstract partial class NServiceBusBootstrapper
    {
        /// <summary>
        ///     The Gateway is turned on by default for the Master role. Call DisableGateway method to turn the Gateway off.
        /// </summary>
        public void DisableGateway()
        {
            Features.Disable<Features.Gateway>();
        }

        /// <summary>
        ///     Configuring to run the Gateway. By default Gateway will use RavenPersistence.
        /// </summary>
        public void RunGateway()
        {
            Features.Enable<Features.Gateway>();
        }

        public void RunGatewayWithInMemoryPersistence()
        {
            RunGateway<InMemoryPersistence>();
        }

        public void RunGatewayWithRavenPersistence()
        {
            RunGateway<RavenDbPersistence>();
        }

        public void RunGateway<TPersistence>() where TPersistence : IPersistMessages
        {
            data["gateway.persistence"] = typeof(TPersistence);

            RunGateway();
        }

        /// <summary>
        ///     Use the in memory messages persistence by the gateway.
        /// </summary>
        public void UseInMemoryGatewayPersister()
        {
            data["gateway.persistence"] = typeof(InMemoryPersistence);
        }

        /// <summary>
        ///     Use in-memory message deduplication for the gateway.
        /// </summary>
        public void UseInMemoryGatewayDeduplication()
        {
            data["gateway.deduplication"] = typeof(InMemoryDeduplication);
        }

        /// <summary>
        ///     Use RavenDB messages persistence by the gateway.
        /// </summary>
        public void UseRavenGatewayPersister()
        {
            data["gateway.persistence"] = typeof(RavenDbPersistence);
        }

        /// <summary>
        ///     Use RavenDB for message deduplication by the gateway.
        /// </summary>
        public void UseRavenGatewayDeduplication()
        {
            data["gateway.deduplication"] = typeof(RavenDBDeduplication);
        }
    }
}