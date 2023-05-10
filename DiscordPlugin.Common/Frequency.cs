namespace DiscordPlugin.Common
{
    public class Frequency
    {
        public Frequency() { }

        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Hertz { get; set; }
        public uint MHz { get; set; }
        public bool IsHF { get; set; }
        public bool Receive { get; set; }
        public bool Transmit { get; set; }
    }
}
