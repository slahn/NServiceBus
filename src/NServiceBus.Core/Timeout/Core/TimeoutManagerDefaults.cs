namespace NServiceBus.Timeout.Core
{
    using Config;

    public class TimeoutManagerDefaults : Configurator
    {
        public override void InitializeDefaults()
        {
            InfrastructureServices.SetDefaultFor<IManageTimeouts>(typeof(DefaultTimeoutManager), DependencyLifecycle.SingleInstance);
        }
    }
}