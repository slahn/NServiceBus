using NServiceBus;

namespace UnobtrusiveConventions
{
    public class MessageConvention : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Define your unobtrusive conventions here.
        /// </summary>
        public void Init()
        {
            Configure.Instance.DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"));
        }
    }
}
