namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using System.Text;
    using Logging;

    public class FeatureInitializer : Configurator
    {
        public override void BeforeFinalizingConfiguration()
        {
            ForAllTypes<Feature>(t =>
                {
                    var feature = (Feature)Activator.CreateInstance(t);
                    feature.SetConfigurator(this);

                    if (feature.IsEnabledByDefault && !FeatureRegistrar.IsEnabled(t))
                    {
                        Logger.InfoFormat("Default feature {0} has been explicitly disabled", feature.Name);
                        return;
                    }

                    if (feature.IsEnabledByDefault && !feature.ShouldBeEnabled())
                    {
                        FeatureRegistrar.Disable(t);
                        Logger.DebugFormat("Default feature {0} disabled", feature.Name);
                    }
                });
        }

        public override void FinalizeConfiguration()
        {
            InitializeFeatures();
            InitializeCategories();
        }

        void InitializeFeatures()
        {
            var statusText = new StringBuilder();

            ForAllTypes<Feature>(t =>
                {
                    var feature = (Feature) Activator.CreateInstance(t);

                    if (feature.Category != FeatureCategory.None)
                    {
                        statusText.AppendLine(string.Format("{0} - Controlled by category {1}", feature,
                                                            feature.Category.Name));
                        return;
                    }

                    if (!FeatureRegistrar.IsEnabled(t))
                    {
                        statusText.AppendLine(string.Format("{0} - Disabled", feature));
                        return;
                    }

                    feature.Initialize();

                    statusText.AppendLine(string.Format("{0} - Enabled", feature));
                });

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        void InitializeCategories()
        {
            var statusText = new StringBuilder();

            ForAllTypes<FeatureCategory>(t =>
            {
                if(t == typeof(FeatureCategory.NoneFeatureCategory))
                    return;

                var category = (FeatureCategory)Activator.CreateInstance(t);

                var featuresToInitialize = category.GetFeaturesToInitialize().ToList();

                statusText.AppendLine(string.Format("   - {0}", category.Name));

                foreach (var feature in category.GetAllAvailableFeatures())
                {
                    var shouldBeInitialized = featuresToInitialize.Contains(feature);

                    if (shouldBeInitialized)
                        feature.Initialize();

                    statusText.AppendLine(string.Format("       * {0} - {1}", feature.Name, shouldBeInitialized ? "Enabled" : "Disabled"));    
                }

            });

            Logger.InfoFormat("Feature categories: \n{0}", statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (FeatureInitializer));
    }
}