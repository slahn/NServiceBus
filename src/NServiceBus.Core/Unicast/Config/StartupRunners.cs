namespace NServiceBus.Unicast.Config
{
    using System.Linq;
    using NServiceBus.Config;

    class StartupRunners : Configurator, IWantToRunWhenConfigurationIsComplete
    {
        public override void RegisterTypes()
        {
            TypesToScan
                .Where(
                    t =>
                    typeof(IWantToRunWhenTheBusStarts).IsAssignableFrom(t) && !t.IsInterface)
                .ToList()
                .ForEach(
                    type => Register(type, DependencyLifecycle.InstancePerCall));
        }

        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<UnicastBus>())
                return;

            Configure.Instance.Builder.Build<UnicastBus>().Started +=
                (obj, ev) => Configure.Instance.Builder.BuildAll<IWantToRunWhenTheBusStarts>().ToList()
                                      .ForEach(r => r.Run());
        }
    }
}