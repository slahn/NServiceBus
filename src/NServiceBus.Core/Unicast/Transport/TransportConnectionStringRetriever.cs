namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Configuration;

    public class TransportConnectionStringRetriever
    {
        public void Override(Func<string> func)
        {
            if (func == null)
            {
                return;
            }

            GetValue = _ => func();
        }

        public string GetConnectionStringOrNull(string connectionStringName = null)
        {
            return GetValue(connectionStringName ?? DefaultConnectionStringName);
        }

        const string DefaultConnectionStringName = "NServiceBus/Transport";

        Func<string, string> GetValue = connectionStringName =>
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringSettings == null)
            {
                return null;
            }

            return connectionStringSettings.ConnectionString;
        };
    }
}