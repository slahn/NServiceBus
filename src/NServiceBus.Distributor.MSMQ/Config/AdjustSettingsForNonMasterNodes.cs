namespace NServiceBus.Distributor.MSMQ.Config
{
    using Settings;

    class AdjustSettingsForNonMasterNodes : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            if (!Configure.Instance.HasMasterNode())
            {
                return;
            }

            SettingsHolder.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", Configure.Instance.GetMasterNodeAddress().SubScope("Retries"));
        }
    }
}