namespace NServiceBus
{
    using System;
    using System.Linq;
    using Transports;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public abstract partial class NServiceBusBootstrapper
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TransportDefinition"/> to be configured.</typeparam>
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param> 
        /// <returns>The configuration object.</returns>
        public void UseTransport<T>(string connectionStringName = null) where T : TransportDefinition
        {
            UseTransport(typeof(T), connectionStringName);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TransportDefinition"/> to be configured.</typeparam>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connection string to use</param>
        /// <returns>The configuration object.</returns>
        public void UseTransport<T>(Func<string> definesConnectionString) where T : TransportDefinition
        {
            UseTransport(typeof(T), definesConnectionString);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <param name="transportDefinitionType">Type of <see cref="TransportDefinition"/> to be configured.</param>
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param>
        /// <returns>The configuration object.</returns>
        public void UseTransport(Type transportDefinitionType, string connectionStringName = null)
        {
            data["transport.typedefinition"] = transportDefinitionType;

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                data["transport.connectionStringName"] = connectionStringName;
            }
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <param name="transportDefinitionType">Type of <see cref="TransportDefinition"/> to be configured.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connection string to use</param>
        /// <returns>The configuration object.</returns>
        public void UseTransport(Type transportDefinitionType, Func<string> definesConnectionString)
        {
            data["transport.typedefinition"] = transportDefinitionType;
            data["transport.definesConnectionString"] = definesConnectionString;
        }
    }

    class transportReceiverConfig : Configurator
    {
        public override void InitializeDefaults()
        {
            var transportConfigurer = CreateTransportConfigurer((Type) Bootstrapper["transport.typedefinition"]);
            transportConfigurer.Configure(this);
        }

        private static IConfigureTransport CreateTransportConfigurer(Type transportDefinitionType)
        {
            var transportConfigurerType =
                Configure.TypesToScan.SingleOrDefault(
                    t => typeof(IConfigureTransport<>).MakeGenericType(transportDefinitionType).IsAssignableFrom(t));

            if (transportConfigurerType == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigureTransport implementation for your selected transport: " +
                    transportDefinitionType.Name);

            var transportConfigurer = (IConfigureTransport)Activator.CreateInstance(transportConfigurerType);
            return transportConfigurer;
        }
    }
}