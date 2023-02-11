using System;
using vatsys;

namespace vatSysApi.Common
{
    public class SSR
    {
        public SSR() { }

        public SSR(FDP2.FDR fdr) : this()
        {
            Assigned = ToString(fdr.AssignedSSRCode);
        }

        public string Assigned { get; set; }
        public string Actual { get; set; }
        public bool IsCorrect
        {
            get
            {
                if (Assigned == Actual) return true;
                return false;
            }
        }
        public bool IsAssigned
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Assigned);
            }
        }
        public bool IsModeC { get; set; }

        public void FDRUpdate(FDP2.FDR updated)
        {
            Assigned = ToString(updated.AssignedSSRCode);
        }

        public void RadarUpdate(RDP.RadarTrack updated)
        {
            Actual = ToString(updated.ActualAircraft.TransponderCode);
            IsModeC = updated.ActualAircraft.TransponderModeC;
        }

        private int ToInt(string input)
        {
            return Convert.ToInt32(input, 8);
        }

        private string ToString(int input)
        {
            if (input == 0) return null;
            var converted = Convert.ToString(input, 8);
            if (converted == "37777777777") return null;
            return converted;
        }
    }
}
