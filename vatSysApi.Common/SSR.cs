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

        private int assigned { get; set; } = 0;
        public string Assigned
        {
            get
            {
                if (assigned == 0) return null;
                return ToString(assigned);
            }
            set
            {
                assigned = ToInt(value);
            }
        }
        private int actual { get; set; } = 0;
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
        public bool IsCorrect
        {
            get
            {
                if (assigned == 0 && actual != 0) return true;
                if (assigned == actual) return true;
                return false;
            }
        }
        public bool IsAssigned
        {
            get
            {
                if (assigned == 0) return false;
                return true;
            }
        }
        public bool IsModeC { get; set; }

        public void FDRUpdate(FDP2.FDR updated)
        {
            assigned = updated.AssignedSSRCode;
        }

        public void RadarUpdate(RDP.RadarTrack updated)
        {
            actual = updated.ActualAircraft.TransponderCode;
            IsModeC = updated.ActualAircraft.TransponderModeC;
        }

        private int ToInt(string input)
        {
            try
            {
                return Convert.ToInt32(input, 8);
            }
            catch { return 0; }
        }

        private string ToString(int input)
        {
            if (input == 0) return null;
            return Convert.ToString(input, 8);
        }
    }
}
