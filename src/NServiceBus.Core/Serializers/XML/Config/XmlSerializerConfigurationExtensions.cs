namespace NServiceBus
{
    using System;
    using Features;
    using Serializers.XML.Config;

    public static class XmlSerializerConfigurationExtensions
    {
        /// <summary>
        /// Enables the xml message serializer with the given settings
        /// </summary>
        public static void Xml(this NServiceBusBootstrapper.SerializationConfig settings, Action<XmlSerializationSettings> customSettings = null)
        {
            settings.Bootstrapper.Features.Enable<XmlSerialization>();

            if (customSettings != null)
            {
                customSettings(new XmlSerializationSettings());
            }
        }
    }
}