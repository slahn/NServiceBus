namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq.Expressions;
    using Config;
    using Faults;
    using Faults.Forwarder;
    using Logging;
    using Utils;

    class ConfigureFaultsForwarder : Configurator
	{
        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureFaultsForwarder));

        public override void RegisterTypes()
		{
			if (!IsRegistered<IManageMessageFailures>())
			{
                var ErrorQueue = Address.Undefined;

                var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
                if (section != null)
                {
                    if (string.IsNullOrWhiteSpace(section.ErrorQueue))
                    {
                        throw new ConfigurationErrorsException(
                            "'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
                            "\n The following is an example for adding such a value to your app config: " +
                            "\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");
                    }

                    Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");

                    ErrorQueue = Address.Parse(section.ErrorQueue);

                    Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<FaultManager, object>>, object>
                    {
                        {
                            fm => fm.ErrorQueue, ErrorQueue
                        }
                    });

                    return;
                }


                var errorQueue = RegistryReader<string>.Read("ErrorQueue");
                if (!string.IsNullOrWhiteSpace(errorQueue))
                {
                    Logger.Debug("Error queue retrieved from registry settings.");
                    ErrorQueue = Address.Parse(errorQueue);

                    Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<FaultManager, object>>, object>
                    {
                        {
                            fm => fm.ErrorQueue, ErrorQueue
                        }
                    });
                }

                if (ErrorQueue == Address.Undefined)
                {
                    throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config" +
                    "\n or configure a global one using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
                }
			}
		}
	}
}
