namespace NServiceBus.MessageHeaders
{
    using System.Collections.Generic;
    using Config;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Messages;

    class HeaderBootstrapper :  IWantToRunWhenConfigurationIsComplete
    {
        public IBus Bus { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void SetupHeaderActions()
        {
            ExtensionMethods.GetHeaderAction = (message, key) =>
            {
                if (message == ExtensionMethods.CurrentMessageBeingHandled)
                {
                    LogicalMessage messageBeingReceived;

                    //first try to get the header from the current logical message
                    if (PipelineExecutor.CurrentContext.TryGet(out messageBeingReceived))
                    {
                        string value;

                        messageBeingReceived.Headers.TryGetValue(key, out value);

                        return value;
                    }

                    //falling back to get the headers from the physical message
                    // when we remove the multi message feature we can remove this and instead
                    // share the same header collection btw physical and logical message
                    if (Bus.CurrentMessageContext != null && Bus.CurrentMessageContext.Headers.ContainsKey(key))
                    {
                        return Bus.CurrentMessageContext.Headers[key];
                    }
                    return null;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!PipelineExecutor.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    return null;
                }
                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    return null;
                }

                string headerValue;

                outgoingHeadersForThisMessage.TryGetValue(key, out headerValue);

                return headerValue;
            };

            ExtensionMethods.SetHeaderAction = (message, key, value) =>
            {
                //are we in the process of sending a logical message
                var outgoingLogicalMessageContext = PipelineExecutor.CurrentContext as SendLogicalMessageContext;

                if (outgoingLogicalMessageContext != null && outgoingLogicalMessageContext.MessageToSend.Instance == message)
                {
                    outgoingLogicalMessageContext.MessageToSend.Headers[key] = value;
                }

                Dictionary<object, Dictionary<string, string>> outgoingHeaders;

                if (!PipelineExecutor.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
                {
                    outgoingHeaders = new Dictionary<object, Dictionary<string, string>>();

                    PipelineExecutor.CurrentContext.Set("NServiceBus.OutgoingHeaders", outgoingHeaders);
                }

                Dictionary<string, string> outgoingHeadersForThisMessage;

                if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
                {
                    outgoingHeadersForThisMessage = new Dictionary<string, string>();
                    outgoingHeaders[message] = outgoingHeadersForThisMessage;
                }

                outgoingHeadersForThisMessage[key] = value;
            };

            ExtensionMethods.GetStaticOutgoingHeadersAction = () => Configure.Instance.Builder.Build<IBus>().OutgoingHeaders;
        }

        void IWantToRunWhenConfigurationIsComplete.Run()
        {
            SetupHeaderActions();
        }
    }
}
