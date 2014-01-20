namespace NServiceBus.Unicast.Transport.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq.Expressions;
    using Licensing;
    using NServiceBus.Config;

    public class Bootstrapper : Configurator
    {
        public override void RegisterTypes()
        {
            LoadConfigurationSettings();

            if (LicenseManager.License.MaxThroughputPerSecond > 0)
            {
                if (maximumThroughput == 0 || LicenseManager.License.MaxThroughputPerSecond < maximumThroughput)
                {
                    maximumThroughput = LicenseManager.License.MaxThroughputPerSecond;
                }
            }
            
            var transactionSettings = new TransactionSettings
                {
                    MaxRetries = maximumNumberOfRetries
                };

            Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<TransportReceiver, object>>, object>
            {
                {t => t.TransactionSettings, transactionSettings},
                {t => t.MaximumConcurrencyLevel, numberOfWorkerThreadsInAppConfig},
                {t => t.MaxThroughputPerSecond, maximumThroughput}
            });
        }

        void LoadConfigurationSettings()
        {
            var transportConfig = GetConfigSection<TransportConfig>();

            if (transportConfig != null)
            {
                maximumNumberOfRetries = transportConfig.MaxRetries;
                maximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;

                numberOfWorkerThreadsInAppConfig = transportConfig.MaximumConcurrencyLevel;
                return;
            }

            if (GetConfigSection<MsmqTransportConfig>() != null)
            {
                throw new ConfigurationErrorsException("'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead. You can use the PowerShell cmdlet 'Add-NServiceBusTransportConfig' in the Package Manager Console to quickly add it for you.");
            }
        }

        int maximumThroughput;
        int maximumNumberOfRetries = 5;
        int numberOfWorkerThreadsInAppConfig = 1;
    }
}
