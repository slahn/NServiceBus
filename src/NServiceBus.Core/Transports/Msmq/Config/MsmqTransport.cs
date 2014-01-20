namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Config;
    using Logging;
    using Transports;
    using Transports.Msmq;
    using Transports.Msmq.Config;

    public class MsmqTransport : ConfigureTransport<Msmq>
    {
        public override void Initialize()
        {
            Configurator.Register<CorrelationIdMutatorForBackwardsCompatibilityWithV3>(DependencyLifecycle.InstancePerCall);
            Configurator.Register<MsmqUnitOfWork>(DependencyLifecycle.SingleInstance);
            Configurator.Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<MsmqDequeueStrategy, object>>, object>
            {
                {
                    p => p.PurgeOnStartup, Configurator.Bootstrapper["PurgeOnStartup"]
                }
            });

            var cfg = Configurator.GetConfigSection<MsmqMessageQueueConfig>();

            var settings = new MsmqSettings();
            if (cfg != null)
            {
                settings.UseJournalQueue = cfg.UseJournalQueue;
                settings.UseDeadLetterQueue = cfg.UseDeadLetterQueue;

                Logger.Warn(Message);
            }
            else
            {
                var connectionString = Configurator.SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

                if (connectionString != null)
                {
                    settings = new MsmqConnectionStringBuilder(connectionString).RetrieveSettings();
                }
            }

            Configurator.Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<MsmqMessageSender, object>>, object>
            {
                {
                    t => t.Settings, settings
                }
            });
                

            Configurator.Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<MsmqQueueCreator, object>>, object>
            {
                {
                    t => t.Settings, settings
                }
            });
        }

        protected override void InternalConfigure(Configurator config)
        {
            Enable<MsmqTransport>();
            Enable<MessageDrivenSubscriptions>();

            //for backwards compatibility
            config.SettingsHolder.SetDefault("SerializationSettings.WrapSingleMessages", true);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "cacheSendConnection=true;journal=false;deadLetter=true"; }
        }

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureMsmqMessageQueue));

        const string Message =
            @"
MsmqMessageQueueConfig section has been deprecated in favor of using a connectionString instead.
Here is an example of what is required:
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""cacheSendConnection=true;journal=false;deadLetter=true"" />
  </connectionStrings>";
    }
}