namespace NServiceBus.Scheduling.Configuration
{
    public class ConfigureScheduledTaskAsSystemMessages : Configurator
    {
        public override void InitializeDefaults()
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}