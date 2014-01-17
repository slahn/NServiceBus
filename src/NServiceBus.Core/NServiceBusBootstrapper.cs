namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using System.Web;
    using Autofac;
    using Config.ConfigurationSource;
    using Config.Conventions;
    using Faults;
    using Faults.Forwarder;
    using Hosting.Helpers;
    using Licensing;
    using Logging;
    using Unicast;
    using Unicast.Transport;
    using Utils.Reflection;
    using Parameter = Autofac.Core.Parameter;

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

        public static IEnumerable<Assembly> GetAssembliesInDirectory(string path)
        {
            var assemblyScanner = new AssemblyScanner(path);
            assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);
            
            return assemblyScanner
                .GetScannableAssemblies()
                .Assemblies;
        }

        static List<Type> GetAllowedTypes(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            Array.ForEach(
                assemblies,
                a =>
                {
                    try
                    {
                        types.AddRange(a.GetTypes()
                                        .Where(AssemblyScanner.IsAllowedType));
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        var errorMessage = AssemblyScanner.FormatReflectionTypeLoadException(a.FullName, e);
                        LogManager.GetLogger(typeof(Configure)).Warn(errorMessage);
                        //intentionally swallow exception
                    }
                });
            return types;
        }

        public Bus2(NServiceBusBootstrapper bootstrapper)
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            if (bootstrapper["scan.types"] != null)
            {
                TypesToScan = (IList<Type>) bootstrapper["scan.types"];
            }
            else if (bootstrapper["scan.assemblies"] != null)
            {
                TypesToScan = GetAllowedTypes((Assembly[])bootstrapper["scan.assemblies"]);
            }
            else
            {
                TypesToScan = GetAllowedTypes(GetAssembliesInDirectory((string)bootstrapper["scan.directory"]).ToArray());
            }

            var builder = new ContainerBuilder();
            builder.RegisterType<UnicastBus>().SingleInstance().PropertiesAutowired();
            builder.RegisterType<TransportReceiver>().As<ITransport>().PropertiesAutowired().WithProperty(new NamedParameter("TransactionSettings", new TransactionSettings{ MaxRetries = 5}));
            builder.RegisterType<FaultManager>().As<IManageMessageFailures>().PropertiesAutowired();
            
            container = builder.Build();
            
            address = Address.Parse(bootstrapper.GetLocalAddressName());

            ActivateAndInvoke<Configurator>(c =>
            {
                c.SetContainer(container);
                c.TypesToScan = TypesToScan;

                c.InitializeDefaults();
                c.RegisterTypes();
                c.BeforeFinalizingConfiguration();
                c.FinalizeConfiguration();
            });
        }

        void ForAllTypes<T>(Action<Type> action) where T : class
        {
            TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
              .ToList().ForEach(action);
        }

        void ActivateAndInvoke<T>(Action<T> action, TimeSpan? thresholdForWarning = null) where T : class
        {
            if (!thresholdForWarning.HasValue)
                thresholdForWarning = TimeSpan.FromSeconds(5);

            var totalTime = new Stopwatch();

            totalTime.Start();

            var details = new List<Tuple<Type, TimeSpan>>();

            ForAllTypes<T>(t =>
            {
                var sw = new Stopwatch();

                sw.Start();
                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
                sw.Stop();

                details.Add(new Tuple<Type, TimeSpan>(t, sw.Elapsed));
            });

            totalTime.Stop();

            var message = string.Format("Invocation of {0} completed in {1:f2} s", typeof(T).FullName, totalTime.Elapsed.TotalSeconds);

            var logAsWarn = details.Any(d => d.Item2 > thresholdForWarning);

            var detailsMessage = new StringBuilder();

            detailsMessage.AppendLine(" - Details:");

            foreach (var detail in details.OrderByDescending(d => d.Item2))
            {
                detailsMessage.AppendLine(string.Format("{0} - {1:f4} s", detail.Item1.FullName, detail.Item2.TotalSeconds));
            }


            if (logAsWarn)
            {
                Logger.Warn(message + detailsMessage);
            }
            else
            {
                Logger.Info(message);
                Logger.Debug(detailsMessage.ToString());
            }
        }

        ILog Logger = LogManager.GetLogger(typeof(Bus2));

        public IList<Type> TypesToScan { get; private set; }

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
        private Dictionary<string, object> data = new Dictionary<string, object>();
 
        public object this[string key]
        {
            get
            {
                return data[key];
            }

            set
            {
                data[key] = value;
            }
        }

        public NServiceBusBootstrapper()
        {
            data.Add("scan.directory", AppDomain.CurrentDomain.BaseDirectory);

            if (HttpRuntime.AppDomainAppId != null)
            {
                data["scan.directory"] = HttpRuntime.BinDirectory;
            }
        }

        public void With(string probeDirectory)
        {
            data["scan.directory"] = probeDirectory;
        }

        public void With(IEnumerable<Assembly> assemblies)
        {
            data["scan.assemblies"] = assemblies.ToList();
        }

        /// <summary>
        /// Configure to scan the given assemblies only.
        /// </summary>
        public void With(params Assembly[] assemblies)
        {
            data["scan.assemblies"] = assemblies.ToList();            
        }

        /// <summary>
        /// Configure to scan the given types.
        /// </summary>
        public void With(IEnumerable<Type> typesToScan)
        {
            data["scan.types"] = typesToScan.ToArray();            
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
