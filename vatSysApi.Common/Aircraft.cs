using System;
using vatsys;

namespace vatSysApi.Common
{
    public class Aircraft
    {
        public Aircraft() => LastUpdateUtc = DateTime.UtcNow;

        public Aircraft(FDP2.FDR fdr) : this()
        {
            Callsign = fdr.Callsign;
            FDR = new FDR(fdr);
            SSR = new SSR(fdr);
        }

        public string Callsign { get; set; }
        public DateTime LastUpdateUtc { get; set; }
        public FDR FDR { get; set; } = new FDR();
        public SSR SSR { get; set; } = new SSR();
        public Coordinate LatLong { get; set; }
        public bool OnGround { get; set; }
        public bool ModeC { get; set; }

        public void FDRUpdate(FDP2.FDR updated)
        {
            LastUpdateUtc = DateTime.UtcNow;

            FDR = new FDR(updated);

            SSR.FDRUpdate(updated);
        }

        public void RadarUpdate(RDP.RadarTrack updated)
        {
            LastUpdateUtc = DateTime.UtcNow;
            OnGround = updated.OnGround;
            ModeC = updated.ActualAircraft.TransponderModeC;

            SSR.RadarUpdate(updated);

            LatLong = updated.LatLong;
        }
    }
}