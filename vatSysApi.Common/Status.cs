using System.Linq;

namespace vatSysApi.Common
{
    public class Status
    {
        public Status() { }
        public Status(Details details) 
        {
            Details = details;
        }

        public Details Details { get; set; }
        public string Title
        {
            get
            {
                if (Details.Frequencies.Any(x => x.Transmit)) return Controlling;
                else if (Details.Frequencies.Any(x => x.Receive)) return Observing;
                return null;
            }
        }
        public string Subtitle
        {
            get
            {
                if (Details.Frequencies.Any(x => x.Transmit)) return TxSent;
                else if (Details.Frequencies.Any(x => x.Receive)) return AcftSpotted;
                return null;
            }
        }
        public string Controlling
        {
            get
            {
                var transmitting = Details.Frequencies.Where(x => x.Transmit).ToList();

                if (transmitting.Count == 0) return "Not controlling anywhere.";

                if (transmitting.Count == 1) return $"Controlling {transmitting[0].FriendlyName}.";

                var output = $"Controlling {transmitting[0].FriendlyName}";
                var count = 1;
                foreach (var freq in transmitting.Skip(1))
                {
                    if (transmitting.Count == count + 1) output += $" and {freq.FriendlyName}.";
                    else output += $", {freq.FriendlyName}";
                }
                return output;
            }
        }
        public string Observing
        {
            get
            {
                var receving = Details.Frequencies.Where(x => x.Receive && !x.Transmit).ToList();

                if (receving.Count == 0) return "Not observing anywhere.";

                if (receving.Count == 1) return $"Observing {receving[0].FriendlyName}.";

                var output = $"Observing {receving[0].FriendlyName}";
                var count = 1;
                foreach (var freq in receving.Skip(1))
                {
                    if (receving.Count == count + 1) output += $" and {freq.FriendlyName}.";
                    else output += $", {freq.FriendlyName}";
                }
                return output;
            }
        }
        public string TxSent
        {
            get
            {
                if (Details.TxSent == 0) return "No transmissions.";

                if (Details.TxSent == 1) return "1 transmission.";

                return $"{Details.TxSent:N0} transmissions.";
            }
        }
        public string TxRecd
        {
            get
            {
                if (Details.TxRecd == 0) return "No transmissions.";

                if (Details.TxRecd == 1) return "1 transmission.";

                return $"{Details.TxRecd:N0} transmissions.";
            }
        }
        public string TxBoth
        {
            get
            {
                return $"Transmissions: {Details.TxSent}/{Details.TxRecd}";
            }
        }
        public string AcftSpotted
        {
            get
            {
                if (Details.AcftSpotted == 0) return "No aircraft spotted.";
                return $"{Details.AcftSpotted} aircraft spotted.";
            }
        }
    }
}
