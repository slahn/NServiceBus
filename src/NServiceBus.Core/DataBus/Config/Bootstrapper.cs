namespace NServiceBus.DataBus.Config
{
    using System;
    using System.Linq;
    using NServiceBus.Config;

    public class Bootstrapper : Configurator, IWantToRunWhenConfigurationIsComplete
	{
        static bool dataBusPropertyFound;

        public override void BeforeFinalizingConfiguration()
		{
            if (!IsRegistered<IDataBusSerializer>() && System.Diagnostics.Debugger.IsAttached)
            {
                var properties = TypesToScan
                    .Where(MessageConventionExtensions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Where(MessageConventionExtensions.IsDataBusProperty);

                foreach (var property in properties)
                {
                    dataBusPropertyFound = true;

                    if (!property.PropertyType.IsSerializable)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                @"The property type for '{0}' is not serializable. 
In order to use the databus feature for transporting the data stored in the property types defined in the call '.DefiningDataBusPropertiesAs()', need to be serializable. 
To fix this, please mark the property type '{0}' as serializable, see http://msdn.microsoft.com/en-us/library/system.runtime.serialization.iserializable.aspx on how to do this.",
                                String.Format("{0}.{1}", property.DeclaringType.FullName, property.Name)));
                    }
                }
            }
            else
            {
                dataBusPropertyFound = TypesToScan
                    .Where(MessageConventionExtensions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Any(MessageConventionExtensions.IsDataBusProperty);
            }

		    if (!dataBusPropertyFound)
		    {
		        return;
		    }

			if (!IsRegistered<IDataBus>())
			{
			    throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");
			}

			Register<DataBusMessageMutator>(DependencyLifecycle.InstancePerCall);

            if (!IsRegistered<IDataBusSerializer>())
            {
                Register<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }
		}

        public void Run()
        {
            if (dataBusPropertyFound)
            {
                Bus.Started += (sender, eventArgs) => Configure.Instance.Builder.Build<IDataBus>().Start();
            }
        }

        public IStartableBus Bus { get; set; }
	}
}
