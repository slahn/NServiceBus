namespace NServiceBus
{
    /// <summary>
    /// Configures purging
    /// </summary>
    public abstract partial class NServiceBusBootstrapper
    {
        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        public void PurgeOnStartup(bool value)
        {
            data["PurgeOnStartup"] = value;
        }
    }
}