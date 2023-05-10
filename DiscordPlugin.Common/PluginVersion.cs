namespace DiscordPlugin.Common
{
    public class PluginVersion
    {
        public PluginVersion() { }

        public PluginVersion(int major, int minor) 
        { 
            Major = major;
            Minor = minor;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
    }
}
