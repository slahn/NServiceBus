namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Autofac;
    using Autofac.Core;
    using Config.ConfigurationSource;
    using Impl;
    using Settings;
    using Utils.Reflection;

    public abstract class Configurator
    {
        IContainer container;
        SettingsHolder settingsHolder;
        FeatureRegistrar featureRegistrar;
        NServiceBusBootstrapper bootstrapper;

        internal void SetContainer(IContainer container)
        {
            this.container = container;
        }

        internal void SetSettingsHolder(SettingsHolder settings)
        {
            settingsHolder = settings;
        }

        internal void SetFeatureRegistrar(FeatureRegistrar featureRegistrar)
        {
            this.featureRegistrar = featureRegistrar;
        }

        internal void SetBootstrapper(NServiceBusBootstrapper bootstrapper)
        {
            this.bootstrapper = bootstrapper;
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
            // throw new NotImplementedException();
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

        public SettingsHolder SettingsHolder
        {
            get { return settingsHolder; }
        }

        public FeatureRegistrar FeatureRegistrar
        {
            get { return featureRegistrar; }
        }

        public NServiceBusBootstrapper Bootstrapper
        {
            get { return bootstrapper; }
        }

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
}