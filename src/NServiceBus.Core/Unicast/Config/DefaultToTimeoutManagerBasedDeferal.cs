namespace NServiceBus.Unicast.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Timeout;
    using Transports;

    public class DefaultToTimeoutManagerBasedDeferal : Configurator
    {
        public override void FinalizeConfiguration()
        {
            if (IsRegistered<IDeferMessages>())
                return;

            Register(DependencyLifecycle.InstancePerCall, new Dictionary<Expression<Func<TimeoutManagerDeferrer, object>>, object>
            {
                {p => p.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress()}
            });
        }
    }
}