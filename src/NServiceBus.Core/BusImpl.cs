namespace NServiceBus.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using Autofac;
    using Hosting.Helpers;
    using Licensing;
    using Logging;
    using Settings;
    using Unicast;
    using Unicast.Transport;

    class BusImpl : IDisposable
    {
        readonly IContainer container;
        ITransport transport;
        Address address;
        SettingsHolder settings = new SettingsHolder();
        FeatureRegistrar featureRegistrar;

        public BusImpl(NServiceBusBootstrapper bootstrapper)
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            featureRegistrar = new FeatureRegistrar(settings);

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

            if (bootstrapper["features"] != null)
            {
                var s = (Dictionary<Type, bool>)bootstrapper["features"];

                foreach (var b in s)
                {
                    if (b.Value)
                    {
                        featureRegistrar.Enable(b.Key);
                    }
                    else
                    {
                        featureRegistrar.Disable(b.Key);
                    }
                }
            }

            var builder = new ContainerBuilder();
            builder.RegisterType<UnicastBus>().SingleInstance().PropertiesAutowired();
            //builder.RegisterType<TransportReceiver>().As<ITransport>().PropertiesAutowired().WithProperty(new NamedParameter("TransactionSettings", new TransactionSettings{ MaxRetries = 5}));
            //builder.RegisterType<FaultManager>().As<IManageMessageFailures>().PropertiesAutowired();
            
            container = builder.Build();
            
            address = Address.Parse(bootstrapper.GetLocalAddressName());
            
            ActivateAndInvoke<Configurator>(c =>
            {
                c.SetContainer(container);
                c.SetSettingsHolder(settings);
                c.SetBootstrapper(bootstrapper);

                c.TypesToScan = TypesToScan;

                c.InitializeDefaults();
                c.RegisterTypes();
                c.BeforeFinalizingConfiguration();
                c.FinalizeConfiguration();
            });
        }

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

        ILog Logger = LogManager.GetLogger(typeof(BusImpl));

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
}