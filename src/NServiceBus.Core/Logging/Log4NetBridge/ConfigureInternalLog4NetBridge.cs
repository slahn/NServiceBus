namespace NServiceBus.Logging.Log4NetBridge
{
    public class ConfigureInternalLog4NetBridge : Configurator
    {
        public override void InitializeDefaults()
        {
            var isLog4NetMerged = typeof(log4net.ILog).Assembly == System.Reflection.Assembly.GetExecutingAssembly();

            if (isLog4NetMerged)
            {
                log4net.Config.BasicConfigurator.Configure(new Log4NetBridgeAppender());
            }
        }
    }
}
