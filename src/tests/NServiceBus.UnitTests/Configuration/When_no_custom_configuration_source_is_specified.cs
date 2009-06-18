using System;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.UnitTests.Configuration
{
    [TestFixture]
    public class When_no_custom_configuration_source_is_specified
    {

        [SetUp]
        public void SetUp()
        {
            Configure.With();
        }

        [Test]
        public void The_default_configuration_source_should_be_default()
        {
            var configSection = Configure.GetConfigSection<TestConfigurationSection>();

            configSection.TestSetting.ShouldEqual("test");
       }

        [Test,ExpectedException(typeof(ArgumentException))]
        public void Getting_sections_that_not_inherits_from_configsection_should_fail()
        {
            Configure.GetConfigSection<IllegalSection>();
        }
    }

    public class IllegalSection
    {
    }
}