using System;
using System.Collections.Generic;

namespace DiscordPlugin.Common
{
    public class Details
    {
        public bool Connected => StartUtc.HasValue;
        public bool OfficalServer { get; set; }
        public DateTime? StartUtc { get; set; }
        public string Callsign { get; set; }
        public int TxRecd { get; set; }
        public int TxSent { get; set; }
        public int AcftSpotted => AircraftSpotted.Count;
        public int AcftControlled => AircraftControlled.Count;
        public List<Frequency> Frequencies { get; set; } = new List<Frequency>();
        public List<string> AircraftSpotted { get; set; } = new List<string>();
        public List<string> AircraftControlled { get; set; } = new List<string>();
        public DisplayType DisplayType { get; set; } = DisplayType.AcftSpotted;
        public bool Debug { get; set; }

        public void Connect(string callsign, bool officalServer = false)
        {
            StartUtc = DateTime.UtcNow;
            Callsign = callsign;
            OfficalServer = officalServer;
        }

        public void Disconnect()
        {
            StartUtc = null;
            Frequencies.Clear();
            AircraftSpotted.Clear();
            AircraftControlled.Clear();
            TxSent = 0;
            TxRecd = 0;
        }
    }
}
