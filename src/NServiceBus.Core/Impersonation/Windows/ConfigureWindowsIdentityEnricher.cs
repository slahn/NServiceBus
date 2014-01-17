namespace NServiceBus.Impersonation.Windows
{
    class ConfigureWindowsIdentityEnricher : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            Register<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance);
        }

    }
}