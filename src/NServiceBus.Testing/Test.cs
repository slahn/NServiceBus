﻿namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Config.ConfigurationSource;
    using DataBus.InMemory;
    using Features;
    using Features.Categories;
    using MessageInterfaces;
    using Persistence;
    using Saga;

    /// <summary>
    ///     Entry class used for unit testing
    /// </summary>
    public static class Test
    {
        /// <summary>
        ///     Get the reference to the bus used for testing.
        /// </summary>
        public static IBus Bus
        {
            get { return bus; }
        }

        /// <summary>
        ///     Initializes the testing infrastructure.
        /// </summary>
        public static void Initialize()
        {
            InitializeInternal(Configure.With());
        }

        /// <summary>
        ///     Initializes the testing infrastructure specifying which assemblies to scan.
        /// </summary>
        public static void Initialize(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException("assemblies");
            }

            Initialize(assemblies.ToArray());
        }

        /// <summary>
        ///     Initializes the testing infrastructure specifying which assemblies to scan.
        /// </summary>
        public static void Initialize(params Assembly[] assemblies)
        {
            InitializeInternal(Configure.With(assemblies));
        }

        /// <summary>
        ///     Initializes the testing infrastructure specifying which types to scan.
        /// </summary>
        public static void Initialize(params Type[] types)
        {
            InitializeInternal(Configure.With(types));
        }

        static void InitializeInternal(Configure config)
        {
            if (initialized)
            {
                return;
            }

            Serializers.SetDefault<XmlSerialization>();


            config.Features.Disable<Sagas>();
            config.Features.Disable<Audit>();

            config.DefineEndpointName("UnitTests")
                .CustomConfigurationSource(testConfigurationSource)
                .DefaultBuilder()
                .UsePersistence<InMemory>()
                .InMemoryFaultManagement()
                .UnicastBus();


            Configure.Component<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            config.Initialize();


            var mapper = Configure.Instance.Builder.Build<IMessageMapper>();
            if (mapper == null)
            {
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");
            }

            messageCreator = mapper;
            ExtensionMethods.GetHeaderAction = (msg, key) =>
            {
                ConcurrentDictionary<string, string> kv;
                if (messageHeaders.TryGetValue(msg, out kv))
                {
                    string val;
                    if (kv.TryGetValue(key, out val))
                    {
                        return val;
                    }
                }

                return null;
            };
            ExtensionMethods.SetHeaderAction = (msg, key, val) =>
                messageHeaders.AddOrUpdate(msg,
                    o => new ConcurrentDictionary<string, string>(new[]
                    {
                        new KeyValuePair<string, string>(key, val)
                    }),
                    (o, dictionary) =>
                    {
                        dictionary.AddOrUpdate(key, val, (s, s1) => val);
                        return dictionary;
                    });

            initialized = true;
        }

        /// <summary>
        ///     Begin the test script for a saga of type T.
        /// </summary>
        public static Saga<T> Saga<T>() where T : Saga, new()
        {
            return Saga<T>(Guid.NewGuid());
        }

        /// <summary>
        ///     Begin the test script for a saga of type T while specifying the saga id.
        /// </summary>
        public static Saga<T> Saga<T>(Guid sagaId) where T : Saga, new()
        {
            var saga = (T) Activator.CreateInstance(typeof(T));

            var prop = typeof(T).GetProperty("Data");
            var sagaData = Activator.CreateInstance(prop.PropertyType) as IContainSagaData;

            saga.Entity = sagaData;

            if (saga.Entity != null)
            {
                saga.Entity.Id = sagaId;
            }

            return Saga(saga);
        }

        /// <summary>
        ///     Begin the test script for the passed in saga instance.
        ///     Callers need to instantiate the saga's data class as well as give it an ID.
        /// </summary>
        public static Saga<T> Saga<T>(T saga) where T : Saga, new()
        {
            bus = new StubBus(messageCreator);
            ExtensionMethods.Bus = bus;

            saga.Bus = Bus;

            return new Saga<T>(saga, bus);
        }

        /// <summary>
        ///     Specify a test for a message handler of type T for a given message of type TMessage.
        /// </summary>
        public static Handler<T> Handler<T>() where T : new()
        {
            var handler = (T) Activator.CreateInstance(typeof(T));

            return Handler(handler);
        }

        /// <summary>
        ///     Specify a test for a message handler while supplying the instance to
        ///     test - injects the bus into a public property (if it exists).
        /// </summary>
        public static Handler<T> Handler<T>(T handler)
        {
            Func<IBus, T> handlerCreator = b => handler;
            var prop = typeof(T).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(IBus));
            if (prop != null)
            {
                handlerCreator = b =>
                {
                    prop.SetValue(handler, b, null);
                    return handler;
                };
            }

            return Handler(handlerCreator);
        }

        /// <summary>
        ///     Specify a test for a message handler specifying a callback to create
        ///     the handler and getting an instance of the bus passed in.
        ///     Useful for handlers based on constructor injection.
        /// </summary>
        public static Handler<T> Handler<T>(Func<IBus, T> handlerCreationCallback)
        {
            bus = new StubBus(messageCreator);
            ExtensionMethods.Bus = bus;

            var handler = handlerCreationCallback.Invoke(bus);

            var isHandler = (from i in handler.GetType().GetInterfaces()
                let args = i.GetGenericArguments()
                where args.Length == 1
                where MessageConventionExtensions.IsMessageType(args[0])
                where typeof(IHandleMessages<>).MakeGenericType(args[0]).IsAssignableFrom(i)
                select i).Any();

            if (!isHandler)
            {
                throw new ArgumentException("The handler object created does not implement IHandleMessages<T>.", "handlerCreationCallback");
            }

            var messageTypes = Configure.Instance.TypesToScan.Where(MessageConventionExtensions.IsMessageType).ToList();

            return new Handler<T>(handler, bus, messageCreator, messageTypes);
        }

        /// <summary>
        ///     Instantiate a new message of type TMessage.
        /// </summary>
        public static TMessage CreateInstance<TMessage>()
        {
            return messageCreator.CreateInstance<TMessage>();
        }

        /// <summary>
        ///     Instantiate a new message of type TMessage performing the given action
        ///     on the created message.
        /// </summary>
        public static TMessage CreateInstance<TMessage>(Action<TMessage> action)
        {
            return messageCreator.CreateInstance(action);
        }

        [ThreadStatic] static StubBus bus;

        static IMessageCreator messageCreator;
        static readonly TestConfigurationSource testConfigurationSource = new TestConfigurationSource();
        static readonly ConcurrentDictionary<object, ConcurrentDictionary<string, string>> messageHeaders = new ConcurrentDictionary<object, ConcurrentDictionary<string, string>>();
        static bool initialized;
    }

    /// <summary>
    ///     Configuration source suitable for testing
    /// </summary>
    public class TestConfigurationSource : IConfigurationSource
    {
        /// <summary>
        ///     Returns null for all types of T.
        /// </summary>
        public T GetConfiguration<T>() where T : class, new()
        {
            return null;
        }
    }
}