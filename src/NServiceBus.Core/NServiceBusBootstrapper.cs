namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using Config.Conventions;
    using Features;
    using Logging;

    public static class  Foo
    {
        public static void ScanSubFolders(this NServiceBusBootstrapper bootstrapper, string[] subFolders)
        {
            //bootstrapper
        }
    }

    public class Foo2:Configurator
    {
        public override void RegisterTypes()
        {
            base.RegisterTypes();
        }

        public override void InitializeDefaults()
        {
            base.InitializeDefaults();
        }
    }

    public abstract partial class  NServiceBusBootstrapper
    {
        public NServiceBusBootstrapper()
        {
            featureConfig = new FeatureConfig(this);
            serializationConfig = new SerializationConfig(this);

            data.Add("scan.directory", AppDomain.CurrentDomain.BaseDirectory);

            if (HttpRuntime.AppDomainAppId != null)
            {
                data["scan.directory"] = HttpRuntime.BinDirectory;
            }
        }

        public FeatureConfig Features
        {
            get { return featureConfig; }
        }

        public SerializationConfig Serialization
        {
            get { return serializationConfig; }
        }

        public object this[string key]
        {
            get
            {
                if (!data.ContainsKey(key))
                {
                    return null;
                }

                return data[key];
            }

            set { data[key] = value; }
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
        ///     Configure to scan the given assemblies only.
        /// </summary>
        public void With(params Assembly[] assemblies)
        {
            data["scan.assemblies"] = assemblies.ToList();
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(NServiceBusBootstrapper));

        /// <summary>
        ///     Configure to scan the given types.
        /// </summary>
        public void With(IEnumerable<Type> typesToScan)
        {
            data["scan.types"] = typesToScan.ToArray();
        }

        public string GetLocalAddressName()
        {
            return EndpointHelper.GetDefaultEndpointName();
        }

        readonly FeatureConfig featureConfig;
        readonly SerializationConfig serializationConfig;
        Dictionary<string, object> data = new Dictionary<string, object>();

        public class SerializationConfig
        {
            NServiceBusBootstrapper bootstrapper;

            internal SerializationConfig(NServiceBusBootstrapper bootstrapper)
            {
                this.bootstrapper = bootstrapper;
            }

            /// <summary>
            /// Enables the json message serializer
            /// </summary>
            public void Json()
            {
                bootstrapper.Features.Enable<JsonSerialization>();
            }

            /// <summary>
            /// Enables the bson message serializer
            /// </summary>
            public void Bson()
            {
                bootstrapper.Features.Enable<BsonSerialization>();
            }

            /// <summary>
            /// Tells the framework to always wrap out going messages as if there was multiple messages being sent
            /// </summary>
            public void WrapSingleMessages()
            {
                bootstrapper["SerializationSettings.WrapSingleMessages"] = true;
            }

            /// <summary>
            /// Tells the framework to not wrap out going messages as if there was multiple messages being sent
            /// </summary>
            public void DoNotWrapSingleMessages()
            {
                bootstrapper["SerializationSettings.WrapSingleMessages"] = false;

            }
            public NServiceBusBootstrapper Bootstrapper
            {
                get { return bootstrapper; }
            }
        }

        public class FeatureConfig
        {
            internal FeatureConfig(NServiceBusBootstrapper bootstrapper)
            {
                this.bootstrapper = bootstrapper;
            }

            public void Enable<T>() where T : Feature
            {
                Enable(typeof(T));
            }

            /// <summary>
            ///     Enables the give feature
            /// </summary>
            public void Enable(Type featureType)
            {
                Set(featureType, true);
            }

            /// <summary>
            ///     Turns the given feature off
            /// </summary>
            public void Disable<T>() where T : Feature
            {
                Disable(typeof(T));
            }

            /// <summary>
            ///     Turns the given feature off
            /// </summary>
            public void Disable(Type featureType)
            {
                Set(featureType, false);
            }

            void Set(Type key, bool value)
            {
                features[key] = value;
                bootstrapper["features"] = features;
            }

            readonly NServiceBusBootstrapper bootstrapper;
            Dictionary<Type, bool> features = new Dictionary<Type, bool>();
        }
    }
}