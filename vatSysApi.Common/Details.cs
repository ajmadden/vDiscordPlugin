using System;
using System.Collections.Generic;

namespace vatSysApi.Common
{
    public class Details
    {
        public bool Connected { get; set; }
        public DateTime? StartUtc { get; set; }
        public int TxRecd { get; set; }
        public int TxSent { get; set; }
        public int AcftSpotted { get; set; }
        public List<Frequency> Frequencies { get; set; } = new List<Frequency>();
        public string[] ATIS { get; set; }

        public void Connect()
        {
            Connected = true;
            StartUtc = DateTime.UtcNow;
        }

        public void Disconnect()
        {
            Connected = false;
            Frequencies.Clear();
            AcftSpotted = 0;
            TxSent = 0;
            TxRecd = 0;
            StartUtc = null;
            SetATIS();
        }

        public void SetATIS(string[] atis = null)
        {
            ATIS = atis;
        }
    }
}
