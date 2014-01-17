namespace NServiceBus.Config
{
    /// <summary>
    /// Initializes the local address
    /// </summary>
    public class AddressInitializer : Configurator
    {
        public override void InitializeDefaults()
        {
            if (Address.Local == null)
            {
                Address.InitializeLocalAddress(ConfigureSettingLocalAddressNameAction.GetLocalAddressName());
            }
        }
    }
}