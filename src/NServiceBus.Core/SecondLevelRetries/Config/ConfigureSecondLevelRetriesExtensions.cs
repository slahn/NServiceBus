namespace NServiceBus
{
    using Features;

    public static class ConfigureSecondLevelRetriesExtensions
    {
        [ObsoleteEx(Replacement = "Configure.Features.Disable<SecondLevelRetries>()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableSecondLevelRetries(this Configure config)
        {
            Feature.Disable<Features.SecondLevelRetries>();

            return config;
        }
    }
}