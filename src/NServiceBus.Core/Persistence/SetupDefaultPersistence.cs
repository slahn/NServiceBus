namespace NServiceBus.Persistence
{
    public class SetupDefaultPersistence : Configurator
    {
        public override void InitializeDefaults()
        {
            ConfigureRavenPersistence.RegisterDefaults();
        }
    }
}