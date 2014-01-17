namespace NServiceBus.Config
{
    using Satellites;

    public class SatelliteConfigurer : Configurator
    {
        public override void RegisterTypes()
        {
            RegisterAllTypes<ISatellite>(DependencyLifecycle.SingleInstance);
        }
    }
}