namespace DiscordPlugin.Common
{
    public class Settings
    {
        public Settings() { }

        public Settings(Details details) 
        { 
            DisplayType = details.DisplayType;
            Debug = details.Debug;
        }

        public DisplayType DisplayType { get; set; } = DisplayType.TxSent;
        public bool Debug { get; set; } = false;
    }
}
