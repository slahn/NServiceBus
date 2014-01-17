namespace NServiceBus.Scheduling.Configuration
{
    public class SchedulerConfiguration : Configurator
    {
        public override void RegisterTypes()
        {
            RegisterInstance<IScheduledTaskStorage>(new InMemoryScheduledTaskStorage(), DependencyLifecycle.SingleInstance);
            Register<DefaultScheduler>(DependencyLifecycle.InstancePerCall);
        }
    }
}