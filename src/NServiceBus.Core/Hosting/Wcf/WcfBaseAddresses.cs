using System;
using System.Linq;

namespace NServiceBus.Hosting.Wcf
{
    public class WcfBaseAddresses
    {
        public readonly Uri[] Addresses;

        public WcfBaseAddresses(params Uri[] addresses)
        {
            Addresses = addresses.Select(u => new Uri(u.AbsoluteUri.TrimEnd('/'))).ToArray();
        }

        public WcfBaseAddresses(params string[] addresses)
        {
            if (!addresses.All(x => Uri.IsWellFormedUriString(x, UriKind.Absolute)))
            {
                throw new ArgumentException("All addresses must be well-formed URI strings", "addresses");
            }

            Addresses = addresses.Select(x => new Uri(x.TrimEnd('/'))).ToArray();
        }        
    }
}
