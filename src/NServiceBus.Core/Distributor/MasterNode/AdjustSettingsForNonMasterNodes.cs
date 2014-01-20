namespace NServiceBus.MasterNode
{
    using Settings;

    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    class AdjustSettingsForNonMasterNodes : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            if (!Configure.Instance.HasMasterNode())
                return;

            Settings.SettingsHolder.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", Configure.Instance.GetMasterNodeAddress().SubScope("Retries"));
        }
    }
}