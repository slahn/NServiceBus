namespace NServiceBus.Unicast.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutomaticSubscriptions;
    using Behaviors;
    using Logging;
    using Messages;
    using NServiceBus.Config;
    using Routing;
    using Settings;

    internal class FinalizeUnicastBusConfiguration : Configurator
    {
        public override void FinalizeConfiguration()
        {
            var knownMessages = TypesToScan
                .Where(MessageConventionExtensions.IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(knownMessages);

            ConfigureMessageRegistry(knownMessages);

            if (Settings.SettingsHolder.GetOrDefault<bool>("UnicastBus.AutoSubscribe"))
            {
                InfrastructureServices.Enable<IAutoSubscriptionStrategy>();
            }
        }

        void RegisterMessageOwnersAndBusAddress(IEnumerable<Type> knownMessages)
        {
            var unicastConfig = GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);
            var key = typeof(DefaultAutoSubscriptionStrategy).FullName + ".SubscribePlainMessages";

            if (Settings.SettingsHolder.HasSetting(key))
            {
                router.SubscribeToPlainMessages = Settings.SettingsHolder.Get<bool>(key);
            }

            RegisterInstance(router, DependencyLifecycle.SingleInstance);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                Configure.Instance.Configurer.ConfigureProperty<UnicastBus>(b => b.ForwardReceivedMessagesTo,
                    forwardAddress);
                Configure.Instance.Configurer.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }
            Configure.Instance.Configurer.ConfigureProperty<UnicastBus>(b => b.TimeToBeReceivedOnForwardedMessages,
                unicastConfig.TimeToBeReceivedOnForwardedMessages);
            Configure.Instance.Configurer.ConfigureProperty<ForwardBehavior>(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);
            
            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m => m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                {
                    if (!(MessageConventionExtensions.IsMessageType(messageType) || MessageConventionExtensions.IsEventType(messageType) || MessageConventionExtensions.IsCommandType(messageType)))
                    {
                        return;
                    }

                    if (MessageConventionExtensions.IsEventType(messageType))
                    {
                        router.RegisterEventRoute(messageType, address);
                        return;
                    }

                    router.RegisterMessageRoute(messageType, address);
                });
            }
        }

        void ConfigureMessageRegistry(List<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry
            {
                DefaultToNonPersistentMessages = !Settings.SettingsHolder.Get<bool>("Endpoint.DurableMessages")
            };

            knownMessages.ForEach(messageRegistry.RegisterMessageType);

            RegisterInstance<MessageMetadataRegistry>(messageRegistry, DependencyLifecycle.SingleInstance);
            Register<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.InfoFormat("Number of messages found: {0}", messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
            {
                return;
            }

            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FinalizeUnicastBusConfiguration));
    }
}