namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security.Principal;
    using Autofac;
    using Autofac.Core;
    using Config.ConfigurationSource;
    using Config.Conventions;
    using Faults;
    using Faults.Forwarder;
    using Licensing;
    using Unicast;
    using Unicast.Transport;
    using Utils.Reflection;

    public abstract class Configurator
    {
        IContainer container;

        internal void SetContainer(IContainer container)
        {
            this.container = container;
        }

        public virtual void InitializeDefaults() { }
        public virtual void RegisterTypes() { }
        public virtual void BeforeFinalizingConfiguration() { }
        public virtual void AfterConfigurationIsFinalized() { }
        public virtual void FinalizeConfiguration() { }

        //TODO:extension method
        public void Register<T>(DependencyLifecycle lifecycle, IDictionary<Expression<Func<T, object>>, object> properties = null) where T : class
        {
            if (properties == null)
            {
                Register(typeof(T), lifecycle);
                return;
            }

            var dictionary = new Dictionary<string, object>(properties.Count);
            
            foreach (var prop in properties)
            {
                var propertyInfo = Reflect<T>.GetProperty(prop.Key);

                dictionary.Add(propertyInfo.Name, prop.Value);
            }

            Register(typeof(T), lifecycle, dictionary);
        }

        public void Register(Type type, DependencyLifecycle lifecycle, IDictionary<string, object> properties = null)
        {
            var builder = new ContainerBuilder();
            var registrationBuilder = builder.RegisterType(type).PropertiesAutowired();

            if(lifecycle == DependencyLifecycle.SingleInstance)
            {
                registrationBuilder.SingleInstance();
            }

            if (properties != null)
            {
                var parameters = new List<Parameter>(properties.Count);
                
                parameters.AddRange(properties.Select(prop => new NamedParameter(prop.Key, prop.Value)));
                registrationBuilder.WithProperties(parameters);
            }

            builder.Update(container.ComponentRegistry);
        }

        public IList<Type> TypesToScan { get; internal set; }

        //TODO: Extension method
        public void RegisterAllTypes<T>(DependencyLifecycle lifecycle) where T : class
        {
            TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
              .ToList().ForEach(t => Register(t, lifecycle));
        }

        public bool IsRegistered<T>()
        {
            return container.IsRegistered(typeof(T));
        }

        //TODO: Remove
        public void MessageForwardingInCaseOfFault()
        {
            throw new NotImplementedException();
        }

        public void ForAllTypes<T>(Action<Type> action) where T : class
        {
            TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList().ForEach(action);
        }

        public void RegisterInstance<TService>(TService instance, DependencyLifecycle lifecycle) where TService: class
        {
            var builder = new ContainerBuilder();
            var registrationBuilder = builder.RegisterInstance(instance).As<TService>().PropertiesAutowired();

            if (lifecycle == DependencyLifecycle.SingleInstance)
            {
                registrationBuilder.SingleInstance();
            }

            builder.Update(container.ComponentRegistry);
        }

        internal IConfigurationSource ConfigurationSource { get; set; }

        public T GetConfigSection<T>() where T : class, new()
        {
            if (TypesToScan == null)
            {
                return ConfigurationSource.GetConfiguration<T>();
            }

            var sectionOverrideType = TypesToScan.FirstOrDefault(t => typeof(IProvideConfiguration<T>).IsAssignableFrom(t));

            if (sectionOverrideType == null)
            {
                return ConfigurationSource.GetConfiguration<T>();
            }

            var sectionOverride = (IProvideConfiguration<T>) Activator.CreateInstance(sectionOverrideType);

            return sectionOverride.GetConfiguration();
        }
    }

    public static class Bus
    {
        public static IDisposable Start<TBootstrapper>() where TBootstrapper : NServiceBusBootstrapper, new()
        {
            var bus = CreateInternal<TBootstrapper>();
            bus.Start();
            return bus;
        }

        public static IDisposable Create<TBootstrapper>() where TBootstrapper : NServiceBusBootstrapper, new()
        {
            return CreateInternal<TBootstrapper>();
        }

        static Bus2 CreateInternal<TBootstrapper>() where TBootstrapper : NServiceBusBootstrapper, new()
        {
            return new Bus2(Activator.CreateInstance<TBootstrapper>());
        }
    }

    class Bus2 : IDisposable
    {
        readonly IContainer container;
        ITransport transport;
        Address address;

        public Bus2(NServiceBusBootstrapper bootstrapper)
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            var builder = new ContainerBuilder();
            builder.RegisterType<UnicastBus>().SingleInstance().PropertiesAutowired();
            builder.RegisterType<TransportReceiver>().As<ITransport>().PropertiesAutowired().WithProperty(new NamedParameter("TransactionSettings", new TransactionSettings{ MaxRetries = 5}));
            builder.RegisterType<FaultManager>().As<IManageMessageFailures>().PropertiesAutowired();
            
            container = builder.Build();
            
            address = Address.Parse(bootstrapper.GetLocalAddressName());
        }

        public void Start()
        {
            transport = container.Resolve<ITransport>();

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            transport.StartedMessageProcessing += TransportStartedMessageProcessing;
            transport.TransportMessageReceived += TransportMessageReceived;
            transport.FinishedMessageProcessing += TransportFinishedMessageProcessing;
            transport.FailedMessageProcessing += TransportFailedMessageProcessing;
            transport.Start(address);
            
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (transport != null)
            {
                transport.Stop();
                transport.StartedMessageProcessing -= TransportStartedMessageProcessing;
                transport.TransportMessageReceived -= TransportMessageReceived;
                transport.FinishedMessageProcessing -= TransportFinishedMessageProcessing;
                transport.FailedMessageProcessing -= TransportFailedMessageProcessing;
            }

            container.Dispose();
        }

        void TransportFailedMessageProcessing(object sender, FailedMessageProcessingEventArgs e)
        {
            throw new NotImplementedException();
        }

        void TransportFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
        {
            throw new NotImplementedException();
        }

        void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void TransportStartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class NServiceBusBootstrapper
    {
        public void With()
        {
        }

        public void With(string probeDirectory)
        {
        }

        public void UseTransport<T>(string connectionStringName = null)
        {
        }

        public void PurgeOnStartup(bool flag)
        {
        }

        public void RunHandlersUnderIncomingPrincipal(bool flag)
        {
        }

        public void RijndaelEncryptionService()
        {
        }

        public string GetLocalAddressName()
        {
            return EndpointHelper.GetDefaultEndpointName();
        }
    }
}
