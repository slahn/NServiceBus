namespace NServiceBus
{
    using System;
    

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

        static Impl.BusImpl CreateInternal<TBootstrapper>() where TBootstrapper : NServiceBusBootstrapper, new()
        {
            return new Impl.BusImpl(Activator.CreateInstance<TBootstrapper>());
        }
    }
}