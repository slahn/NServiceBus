namespace NServiceBus
{
    using Features;

    public static class BinarySerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the binary message serializer
        /// </summary>
        public static void Binary(this NServiceBusBootstrapper.SerializationConfig settings)
        {
            settings.Bootstrapper.Features.Enable<BinarySerialization>();
        }
    }
}