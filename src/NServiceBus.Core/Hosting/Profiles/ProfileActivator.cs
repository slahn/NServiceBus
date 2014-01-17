namespace NServiceBus.Hosting.Profiles
{
    /// <summary>
    /// Activates the profiles to be used
    /// </summary>
    public class ProfileActivator : Configurator
    {
        /// <summary>
        /// The profile manager
        /// </summary>
        public static ProfileManager ProfileManager { get; set; }
        
        /// <summary>
        /// Activate profile handlers
        /// </summary>
        public override void BeforeFinalizingConfiguration()
        {
            if (ProfileManager != null)
            {
                ProfileManager.ActivateProfileHandlers();
            }
        }
    }
}