namespace NServiceBus.Gateway
{
    using Settings;

    /// <summary>
    ///     Sets the default input address for the gateway
    /// </summary>
    public class DefaultInputAddress : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            var gatewayInputAddress = Address.Parse(Configure.EndpointName).SubScope("gateway");

            SettingsHolder.SetDefault("Gateway.InputAddress", gatewayInputAddress);
        }
    }
}