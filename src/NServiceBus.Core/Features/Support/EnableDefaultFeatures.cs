namespace NServiceBus.Features
{
    using System;
    using Logging;

    public class EnableDefaultFeatures : Configurator
    {
        public override void InitializeDefaults()
        {
            ForAllTypes<Feature>(t =>
            {
                var feature = (Feature)Activator.CreateInstance(t);

                if (feature.IsEnabledByDefault)
                {
                    Feature.EnableByDefault(t);
                    Logger.DebugFormat("Feature {0} will be enabled by default", feature.Name);
                }
            });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}