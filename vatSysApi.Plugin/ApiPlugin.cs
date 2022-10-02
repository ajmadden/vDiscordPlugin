using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using vatsys;
using vatsys.Plugin;
using vatSysApi.Common;

namespace RemoteVSCSPlugin
{
    [Export(typeof(IPlugin))]
    public class ApiPlugin : IPlugin
    {
        public string Name => "API";

        private static readonly string _discordAppFile = @"\vatSys Files\Discord\vatSysApi.Discord.exe";
        private static readonly int _apiPort = 45341;

        private static HttpServer _httpServer;
        private static CancellationTokenSource _cancellationToken;

        private static readonly List<VSCSFrequency> _vscsFreqs = new List<VSCSFrequency>();

        public ApiPlugin()
        {
            StartDiscordApp();

            _cancellationToken = new CancellationTokenSource();
            _httpServer = new HttpServer(_apiPort, _cancellationToken.Token);

            Audio.TransmittingChanged += Audio_TransmittingChanged;
            Audio.VSCSFrequenciesChanged += Audio_VSCSFrequenciesChanged;
            Network.Connected += Network_Connected;
            Network.Disconnected += Network_Disconnected;
            Network.ATISConnected += Network_ATISConnected;
            Network.ATISDisconnected += Network_ATISDisconnected;
        }

        private void Network_ATISDisconnected(object sender, EventArgs e)
        {
            _httpServer.ATIS();
        }

        private void Network_ATISConnected(object sender, EventArgs e)
        {
            _httpServer.ATIS(Network.Me.ATIS);
        }

        private void StartDiscordApp()
        {
            var fileName = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}{_discordAppFile}";

            if (!new FileInfo(fileName).Exists) return;

            System.Diagnostics.Process.Start(fileName);
        }

        private void Network_Connected(object sender, EventArgs e)
        {
            _httpServer.NetworkConnected();
        }

        private void Network_Disconnected(object sender, EventArgs e)
        {
            _httpServer.NetworkDisconnected();
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            if (FDP2.GetFDRIndex(updated.Callsign) == -1)
            {
                _httpServer.RemoveAircraft(updated.Callsign);
            }
            
            _httpServer.FDRUpdate(updated);
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            _httpServer.RadarUpdate(updated);

            var fdr = FDP2.GetFDRs.FirstOrDefault(x => x.Callsign == updated.ActualAircraft.Callsign);
            if (fdr == null) return;
            _httpServer.FDRUpdate(fdr);
        }

        private void Audio_TransmittingChanged(object sender, EventArgs e)
        {
            if (!Audio.Transmitting || !Audio.IsAFVConnected) return;

            _httpServer.TransmissionSent();
        }

        private void Audio_VSCSFrequenciesChanged(object sender, EventArgs e)
        {
            foreach (var freq in _vscsFreqs)
            {
                freq.ReceivingChanged -= Freq_ReceivingChanged;
                freq.TransmitChanged -= Freq_Changed;
                freq.ReceiveChanged -= Freq_Changed;
            }

            _vscsFreqs.Clear();

            foreach (var freq in Audio.VSCSFrequencies)
            {
                freq.ReceivingChanged += Freq_ReceivingChanged;
                freq.TransmitChanged += Freq_Changed;
                freq.ReceiveChanged += Freq_Changed;
                _vscsFreqs.Add(freq);
                _httpServer.FreqAdd(freq);
            }
        }

        private void Freq_ReceivingChanged(object sender, EventArgs e)
        {
            _httpServer.TransmissionReceived();
        }

        private void Freq_Changed(object sender, EventArgs e)
        {
            _httpServer.FreqClear();

            foreach (var freq in Audio.VSCSFrequencies)
            {
                _httpServer.FreqAdd(freq);
            }
        }
    }
}
