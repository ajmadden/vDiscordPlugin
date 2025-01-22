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
using System.Reflection;

namespace DiscordPlugin.Plugin
{
    [Export(typeof(IPlugin))]
    public class DiscordPlugin : IPlugin
    {
        public string Name => "Discord";
        public static string DisplayName => "Discord";

        private static readonly Version Version = new Version(1, 4);
        private static string FileName => Path.Combine(AssemblyDirectory, "Discord.json");
        private static readonly string VersionUrl = "https://raw.githubusercontent.com/badvectors/DiscordPlugin/master/Version.json";

        private static readonly string AppName = "DiscordPlugin.Discord";
        private static readonly string AppFile = $"{AppName}.exe";

        private static HttpServer HttpServer;
        private static CancellationTokenSource CancellationToken;
        private static readonly int ApiPort = 45341;

        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly List<VSCSFrequency> VscsFreqs = new List<VSCSFrequency>();

        private static CustomToolStripMenuItem DiscordMenu;
        private static DiscordWindow DiscordWindow;

        public static Details Details = new Details();

        public DiscordPlugin()
        {
            DiscordMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem(DisplayName));
            DiscordMenu.Item.Click += DiscordMenu_Click;
            MMI.AddCustomMenuItem(DiscordMenu);

            CancellationToken = new CancellationTokenSource();
            HttpServer = new HttpServer(ApiPort, CancellationToken.Token);

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
                if (DiscordWindow == null || DiscordWindow.IsDisposed)
                {
                    DiscordWindow = new DiscordWindow();
                }
                else if (DiscordWindow.Visible) return;

                DiscordWindow.Show();
            });
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            StartDiscordApp();
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static void StartDiscordApp()
        {
            if (Process.GetProcessesByName(AppName).Any())
            {
                return;
            }

            var file = Path.Combine(AssemblyDirectory, AppFile);

            if (!new FileInfo(file).Exists) return;

            Process.Start(file);
        }

        private static async Task CheckVersion()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(VersionUrl);

                var version = JsonConvert.DeserializeObject<Version>(response);

                if (version.Major == Version.Major && version.Minor == Version.Minor) return;

                Errors.Add(new Exception("A new version of the plugin is available."), DisplayName);
            }
            catch { }
        }

        public static void LoadSettings()
        {
            if (!File.Exists(FileName))
            {
                return;
            }

            string json = File.ReadAllText(FileName);

            var details = JsonConvert.DeserializeObject<Details>(json);

            Details.Debug = details.Debug;
            Details.DisplayType = details.DisplayType;
        }

        public static void SaveSettings()
        {
            var settings = new Settings(Details);

            var json = JsonConvert.SerializeObject(settings);

            File.WriteAllText(FileName, json);
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
            foreach (var freq in VscsFreqs)
            {
                freq.ReceivingChanged -= Freq_ReceivingChanged;
                freq.TransmitChanged -= Freq_Changed;
                freq.ReceiveChanged -= Freq_Changed;
            }

            VscsFreqs.Clear();

            foreach (var freq in Audio.VSCSFrequencies)
            {
                freq.ReceivingChanged += Freq_ReceivingChanged;
                freq.TransmitChanged += Freq_Changed;
                freq.ReceiveChanged += Freq_Changed;
                VscsFreqs.Add(freq);

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
