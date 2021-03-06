﻿namespace NServiceBus.Transports
{
    using Unicast;

    /// <summary>
    /// Called when the bus wants to defer a message
    /// </summary>
    public interface IDeferMessages
    {
        /// <summary>
        /// Defers the given message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sendOptions"></param>
        void Defer(TransportMessage message, SendOptions sendOptions);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}