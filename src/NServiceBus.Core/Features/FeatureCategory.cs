namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;

    public abstract class FeatureCategory
    {
        protected FeatureCategory()
        {
            name = GetType().Name.Replace(typeof(FeatureCategory).Name, String.Empty);
        }

        public static FeatureCategory None
        {
            get { return none; }
        }

        /// <summary>
        ///     Feature name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        ///     Returns the list of features in the category that should be used
        /// </summary>
        public virtual IEnumerable<Feature> GetFeaturesToInitialize()
        {
            return new List<Feature>();
        }

        public IEnumerable<Feature> GetAllAvailableFeatures()
        {
            return Feature.ByCategory(this);
        }

        protected bool Equals(FeatureCategory other)
        {
            return string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((FeatureCategory) obj);
        }

        public override int GetHashCode()
        {
            return (name != null ? name.GetHashCode() : 0);
        }

        public static bool operator ==(FeatureCategory cat1, FeatureCategory cat2)
        {
            if (ReferenceEquals(cat1, null))
            {
                return ReferenceEquals(cat2, null);
            }

            return cat1.Equals(cat2);
        }

        public static bool operator !=(FeatureCategory cat1, FeatureCategory cat2)
        {
            return !(cat1 == cat2);
        }

        static FeatureCategory none = new NoneFeatureCategory();

        string name;

        public class NoneFeatureCategory : FeatureCategory
        {
        }
    }
}