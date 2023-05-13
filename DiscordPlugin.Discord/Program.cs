using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Timers;
using DiscordPlugin.Common;

namespace DiscordPlugin.App
{
    internal class Program
    {
        private static readonly int _apiPort = 45341;
        private static readonly string _apiUri = $"http://localhost:{_apiPort}";
        private static readonly double _apiSeconds = 1;

        private static readonly string _vatsysProcessName = $@"vatSys";

        private static Discord _discord;
        private static ActivityManager _activityManager;
        private static readonly string _clientID = "1002630602538889387";

        private static readonly HttpClient _client = new HttpClient();

        static void Main()
        {
            try
            {
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
                {
                    Environment.Exit(0);
                }

                _discord = new Discord(Int64.Parse(_clientID), (UInt64)CreateFlags.Default);

                _activityManager = _discord.GetActivityManager();

                CheckVatsys();

                var _timer = new System.Timers.Timer();
                _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _timer.Interval = TimeSpan.FromSeconds(_apiSeconds).TotalMilliseconds;
                _timer.Enabled = true;
                _timer.Start();

                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(_apiSeconds));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                _discord.Dispose();
            }
        }

        private static async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                var response = await _client.GetStringAsync(_apiUri);

                var details = JsonConvert.DeserializeObject<Details>(response);

                await Console.Out.WriteLineAsync(response);

                if (!details.Connected)
                {
                    _activityManager.ClearActivity(result => {
                        Console.WriteLine("Update Activity {0}", result);
                    });
                }
                else
                {
                    var status = new Common.Status(details);

                    _activityManager.UpdateActivity(new Activity
                    {
                        Details = status.Title,
                        State = status.Subtitle,
                        Timestamps =
                    {
                        Start = ConvertToUnixTimestamp(status.Details.StartUtc.Value)
                    },
                        Assets = {
                        LargeImage = "68678556"
                    },
                        Instance = true,
                    }, result => {
                        Console.WriteLine("Update Activity {0}", result);
                    });
                }

                _discord.RunCallbacks();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private static void CheckVatsys()
        {
            var vatsysProcess = Process.GetProcessesByName(_vatsysProcessName);

            if (vatsysProcess.Length == 0) Environment.Exit(0);

            ProcessMonitor.MonitorForExit(vatsysProcess[0]);
        }

        private static long ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Convert.ToInt64(Math.Floor(diff.TotalSeconds));
        }

        public static class ProcessMonitor
        {
            public static event EventHandler ProcessClosed;

            public static void MonitorForExit(Process process)
            {
                Thread thread = new Thread(() =>
                {
                    process.WaitForExit();
                    OnProcessClosed(EventArgs.Empty);
                });
                thread.Start();
            }

            private static void OnProcessClosed(EventArgs e)
            {
                ProcessClosed?.Invoke(null, e);
                Environment.Exit(0);
            }
        }
    }
}
