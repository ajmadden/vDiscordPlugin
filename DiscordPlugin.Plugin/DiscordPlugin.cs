using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using vatsys;
using vatsys.Plugin;
using DiscordPlugin.Common;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordPlugin.Plugin
{
    [Export(typeof(IPlugin))]
    public class DiscordPlugin : IPlugin
    {
        public string Name => "Discord";

        private static readonly Version _version = new Version(1, 2);
        private static readonly string _fileName = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\vatSys Files\Discord.json";
        private static readonly string _versionUrl = "https://raw.githubusercontent.com/badvectors/DiscordPlugin/master/Version.json";

        private static readonly string _discordAppName = "DiscordPlugin.Discord";
        private static readonly string _discordAppFile = $"{_discordAppName}.exe";

        private static HttpServer _httpServer;
        private static CancellationTokenSource _cancellationToken;
        private static readonly int _apiPort = 45341;

        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly List<VSCSFrequency> _vscsFreqs = new List<VSCSFrequency>();

        private static CustomToolStripMenuItem _discordMenu;
        private static DiscordWindow _discordWindow;

        public static Details Details = new Details();

        public DiscordPlugin()
        {
            _discordMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("Discord"));
            _discordMenu.Item.Click += DiscordMenu_Click;
            MMI.AddCustomMenuItem(_discordMenu);

            _cancellationToken = new CancellationTokenSource();
            _httpServer = new HttpServer(_apiPort, _cancellationToken.Token);

            Audio.TransmittingChanged += Audio_TransmittingChanged;
            Audio.VSCSFrequenciesChanged += Audio_VSCSFrequenciesChanged;
            Network.Connected += Network_Connected;
            Network.Disconnected += Network_Disconnected;

            LoadSettings();

            StartDiscordApp();

            var _timer = new System.Timers.Timer();
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            _timer.Enabled = true;
            _timer.Start();

            _ = CheckVersion();
        }

        private void DiscordMenu_Click(object sender, EventArgs e)
        {
            ShowDiscordWindow();
        }

        private static void ShowDiscordWindow()
        {
            MMI.InvokeOnGUI((MethodInvoker)delegate () 
            {
                if (_discordWindow == null || _discordWindow.IsDisposed)
                {
                    _discordWindow = new DiscordWindow();
                }
                else if (_discordWindow.Visible) return;

                _discordWindow.Show();
            });
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            StartDiscordApp();
        }

        public static void StartDiscordApp()
        {
            if (Process.GetProcessesByName(_discordAppName).Any())
            {
                return;
            }

            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            var file = Path.Combine(Environment.CurrentDirectory, "Plugins", "Discord", _discordAppFile);

            if (!new FileInfo(file).Exists) return;

            Process.Start(file);
        }

        private static async Task CheckVersion()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_versionUrl);

                var version = JsonConvert.DeserializeObject<Version>(response);

                if (version.Major == _version.Major && version.Minor == _version.Minor) return;

                Errors.Add(new Exception("A new version of the plugin is available."), "Discord Plugin");
            }
            catch { }
        }

        public static void LoadSettings()
        {
            if (!File.Exists(_fileName))
            {
                return;
            }

            string json = File.ReadAllText(_fileName);

            var details = JsonConvert.DeserializeObject<Details>(json);

            Details.Debug = details.Debug;
            Details.DisplayType = details.DisplayType;
        }

        public static void SaveSettings()
        {
            var settings = new Settings(Details);

            var json = JsonConvert.SerializeObject(settings);

            File.WriteAllText(_fileName, json);
        }

        private void Network_Connected(object sender, EventArgs e)
        {
            Details.Connect(Network.Me.Callsign, Network.IsOfficialServer);
        }

        private void Network_Disconnected(object sender, EventArgs e)
        {
            Details.Disconnect();
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            try
            {
                var spotted = Details.AircraftSpotted.Any(x => x == updated.Callsign);

                if (!spotted) Details.AircraftSpotted.Add(updated.Callsign);

                if (updated.ControllingSector.Callsign != Network.Me.Callsign) return;

                var controlled = Details.AircraftControlled.Any(x => x == updated.Callsign);

                if (!controlled) Details.AircraftControlled.Add(updated.Callsign);
            }
            catch { }
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            try
            {
                var spotted = Details.AircraftSpotted.Any(x => x == updated.ActualAircraft?.Callsign);

                if (!spotted) Details.AircraftSpotted.Add(updated.ActualAircraft.Callsign);

                if (updated.CoupledFDR == null) return;

                if (updated.CoupledFDR?.ControllingSector.Callsign != Network.Me.Callsign) return;

                var controlled = Details.AircraftControlled.Any(x => x == updated.CoupledFDR.Callsign);

                if (!controlled) Details.AircraftControlled.Add(updated.CoupledFDR.Callsign);
            }
            catch { }
        }

        private void Audio_TransmittingChanged(object sender, EventArgs e)
        {
            if (!Audio.Transmitting || !Audio.IsAFVConnected) return;

            if (!Details.Frequencies.Any(x => x.Transmit)) return;

            Details.TxSent++;
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

                FreqAdd(new Frequency()
                {
                    Name = freq.Name,
                    FriendlyName = !string.IsNullOrWhiteSpace(freq.FriendlyName) ? freq.FriendlyName : freq.Name,
                    Hertz = (freq.Frequency / 1000000).ToString("N1"),
                    MHz = freq.Frequency,
                    IsHF = freq.IsHF,
                    Receive = freq.Receive,
                    Transmit = freq.Transmit
                });
            }
        }

        private void Freq_ReceivingChanged(object sender, EventArgs e)
        {
            if (!Details.Frequencies.Any(x => x.Transmit)) return;

            Details.TxRecd++;
        }

        private void Freq_Changed(object sender, EventArgs e)
        {
            Details.Frequencies.Clear();

            foreach (var freq in Audio.VSCSFrequencies)
            {
                FreqAdd(new Frequency()
                {
                    Name = freq.Name,
                    FriendlyName = !string.IsNullOrWhiteSpace(freq.FriendlyName) ? freq.FriendlyName : freq.Name,
                    Hertz = (freq.Frequency / 1000000).ToString("N1"),
                    MHz = freq.Frequency,
                    IsHF = freq.IsHF,
                    Receive = freq.Receive,
                    Transmit = freq.Transmit
                });
            }
        }

        public void FreqAdd(Frequency freq)
        {
            if (Details.Frequencies.Any(x => x.Name == freq.Name)) return;

            Details.Frequencies.Add(freq);
        }

    }
}
