namespace NServiceBus.Gateway.Channels.Http
{
    public class SetDefaultResponder : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            if (!IsRegistered<IHttpResponder>())
            {
                Register<DefaultResponder>(DependencyLifecycle.InstancePerCall);
            }
        }
    }
}