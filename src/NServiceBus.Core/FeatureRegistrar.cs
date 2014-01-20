namespace NServiceBus.Impl
{
    using System;
    using Features;
    using Settings;

    public class FeatureRegistrar
    {
        readonly SettingsHolder settings;

        internal FeatureRegistrar(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        ///     Enables the give feature
        /// </summary>
        public void Enable<T>() where T : Feature
        {
            Enable(typeof(T));
        }

        /// <summary>
        ///     Enables the give feature
        /// </summary>
        public void Enable(Type featureType)
        {
            settings.Set(featureType.FullName, true);
        }

        /// <summary>
        ///     Enables the give feature unless explicitly disabled
        /// </summary>
        public void EnableByDefault<T>() where T : Feature
        {
            EnableByDefault(typeof(T));
        }

        /// <summary>
        ///     Enables the give feature unless explicitly disabled
        /// </summary>
        public void EnableByDefault(Type featureType)
        {
            settings.SetDefault(featureType.FullName, true);
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
            settings.Set(featureType.FullName, false);
        }

        /// <summary>
        ///     Disabled the give feature unless explicitly enabled
        /// </summary>
        public void DisableByDefault(Type featureType)
        {
            settings.SetDefault(featureType.FullName, false);
        }

        /// <summary>
        ///     Returns true if the given feature is enabled
        /// </summary>
        public bool IsEnabled<T>() where T : Feature
        {
            return IsEnabled(typeof(T));
        }


        /// <summary>
        ///     Returns true if the given feature is enabled
        /// </summary>
        public bool IsEnabled(Type feature)
        {
            return settings.GetOrDefault<bool>(feature.FullName);
        }
    }
}