using System;
using vatsys;

namespace vatSysApi.Common
{
    public class SSR
    {
        public SSR() { }

        public SSR(FDP2.FDR fdr) : this()
        {
            assigned = fdr.AssignedSSRCode;
        }

        private int assigned { get; set; } = -1;
        public string Assigned
        {
            get
            {
                return ToString(assigned);
            }
            set
            {
                assigned = ToInt(value);
            }
        }
        private int actual { get; set; } = -1;
        public string Actual
        {
            get
            {
                return ToString(actual);
            }
            set
            {
                actual = ToInt(value);
            }
        }
        public bool Correct
        {
            get
            {
                if (assigned == -1 && actual != -1) return true;
                if (assigned == actual) return true;
                return false;
            }
        }
        public bool ModeC { get; set; }

        public void FDRUpdate(FDP2.FDR updated)
        {
            assigned = updated.AssignedSSRCode;
        }

        public void RadarUpdate(RDP.RadarTrack updated)
        {
            actual = updated.ActualAircraft.TransponderCode;
            ModeC = updated.ActualAircraft.TransponderModeC;
        }

        private int ToInt(string input)
        {
            try
            {
                return Convert.ToInt32(input, 8);
            }
            catch { return -1; }
        }

        private string ToString(int input)
        {
            if (input == -1) return null;
            return Convert.ToString(input, 8);
        }
    }
}
