namespace NServiceBus.Pipeline
{
    class PipelineOverrideConfigurer : Configurator
    {
        public override void RegisterTypes()
        {
            RegisterAllTypes<IPipelineOverride>(DependencyLifecycle.InstancePerCall);
        }
    }
}