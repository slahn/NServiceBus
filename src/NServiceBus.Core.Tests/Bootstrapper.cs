namespace NServiceBus.Core.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class Bootstrapper
    {
        [Test]  
        public void Foo()
        {
            using (Bus.Start<MyBootStrapper>())
            {
                Console.Out.WriteLine("I'm working");
            }
        }

        public class MyBootStrapper : NServiceBusBootstrapper
        {
            public MyBootStrapper()
            {
                PurgeOnStartup(false);
                Serialization.Json();

            }
        }
    }

    static class FooBar
    {
        public static void ScanAssemblies(this NServiceBusBootstrapper b, string[] assemblies)
        {
            b["sdds"] = assemblies;
        }
    }
}
