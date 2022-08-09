using vatsys;

namespace vatSysApi.Common
{
    public class Frequency
    {
        public Frequency() { }

        public Frequency(VSCSFrequency freq) 
        {
            Name = freq.Name;
            FriendlyName = freq.FriendlyName;
            Hertz = (freq.Frequency / 1000000).ToString("N1");
            MHz = freq.Frequency;
            IsHF = freq.IsHF;
            Receive = freq.Receive;
            Transmit = freq.Transmit;
        }

        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Hertz { get; set; }
        public uint MHz { get; set; }
        public bool IsHF { get; set; }
        public bool Receive { get; set; }
        public bool Transmit { get; set; }
    }
}
