namespace Risebird.Entities
{
    public class ConfigurationOptions
    {
        public ConfigurationOptions()
        {

            MicroServiceSettings = new MicroServiceSettingsOptions();

        }

        public MicroServiceSettingsOptions MicroServiceSettings { get; set; }
    }
}
