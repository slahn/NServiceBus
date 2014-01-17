namespace NServiceBus.Encryption
{
    class Bootstrapper : Configurator
    {
        public override void RegisterTypes()
        {
            Register<EncryptionMessageMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}
