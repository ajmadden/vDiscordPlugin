using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using vatsys;

namespace vatSysApi.Common
{
    public class HttpServer
    {
        private Task _httpListenTask;
        private CancellationToken _cancellationToken;
        private int _listenPort;

        private static List<Aircraft> _aircraft = new List<Aircraft>();
        private static Details _details = new Details();

        public HttpServer(int port, CancellationToken token)
        {
            _listenPort = port;
            _cancellationToken = token;

            StartHttpListener();
        }

        public void RadarUpdate(RDP.RadarTrack updated)
        {
            var existingAircraft = _aircraft.FirstOrDefault(x => x.Callsign == updated.ActualAircraft.Callsign);

            if (existingAircraft == null) return;

            existingAircraft.RadarUpdate(updated);
        }

        public void FDRUpdate(FDP2.FDR updated)
        {
            var existingAircraft = _aircraft.FirstOrDefault(x => x.Callsign == updated.Callsign);

            if (existingAircraft == null)
            {
                _aircraft.Add(new Aircraft(updated));
                _details.AcftSpotted++;
            }
            else
                existingAircraft.FDRUpdate(updated);
        }

        public void RemoveAircraft(string callsign)
        {
            var existingAircraft = _aircraft.FirstOrDefault(x => x.Callsign == callsign);

            if (existingAircraft == null) return;

            _aircraft.Remove(existingAircraft);
        }

        public void TransmissionReceived()
        {
            if (!_details.Frequencies.Any(x => x.Transmit)) return;

            _details.TxRecd++;
        }

        public void TransmissionSent()
        {
            if (!_details.Frequencies.Any(x => x.Transmit)) return;

            _details.TxSent++;
        }

        public void NetworkConnected()
        {
            _details.Connect();
        }

        public void NetworkDisconnected()
        {
            _aircraft.Clear();
            _details.Disconnect();
        }

        public void Squawk(FDP2.FDR fdr, int requestedCode = -1)
        {
            FDP2.SetASSR(fdr, requestedCode);
        }

        public void FreqClear()
        {
            _details.Frequencies.Clear();
        }

        public void FreqAdd(VSCSFrequency freq)
        {
            _details.Frequencies.Add(new Frequency(freq));
        }

        private void StartHttpListener()
        {
            _httpListenTask = new Task(() => HttpListen(), _cancellationToken, TaskCreationOptions.LongRunning);
            _httpListenTask.Start();
        }

        private async void HttpListen()
        {
            var listener = new TcpListener(IPAddress.Any, _listenPort);
            listener.Start();

            _cancellationToken.Register(() => listener.Stop());

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();

                    ProcessHttpClient(client);
                }
            }
            catch { }
        }

        private async void ProcessHttpClient(TcpClient client)
        {
            client.LingerState = new LingerOption(true, 500);
            var stream = client.GetStream();
            stream.WriteTimeout = 500;

            var receive = new byte[client.ReceiveBufferSize];
            var bytesRequest = await stream.ReadAsync(receive, 0, receive.Length);

            string request = Encoding.ASCII.GetString(receive, 0, bytesRequest);

            var response = new StringBuilder();

            var expression = @"(GET|POST)( /)(.*)( )(HTTP/1.1)";

            var match = Regex.Match(request, expression);

            if (!match.Success)
            {
                await BadRequestResponse(client, stream);
                return;
            }
            else
            {
                string requestType = match.Groups[1].Value;
                string requestPath = match.Groups[3].Value;

                if (requestPath == "Version")
                {
                    await OkRequestResponse(client, stream, JsonConvert.SerializeObject(new Version(), Formatting.Indented));
                    return;
                }

                else if (requestPath == "Details")
                {
                    await OkRequestResponse(client, stream, JsonConvert.SerializeObject(_details, Formatting.Indented));
                    return;
                }

                else if (requestType == "GET" && requestPath.StartsWith("Aircraft"))
                {
                    var split = requestPath.Split('/');

                    if (split.Length == 1)
                    {
                        await OkRequestResponse(client, stream, JsonConvert.SerializeObject(_aircraft, Formatting.Indented));
                        return;
                    }
                    else
                    {
                        var aircraft = _aircraft.FirstOrDefault(x => x.Callsign == split[1]);

                        if (aircraft == null)
                        {
                            await NotFoundResponse(client, stream);
                            return;
                        }

                        await OkRequestResponse(client, stream, JsonConvert.SerializeObject(aircraft, Formatting.Indented));
                        return;
                    }
                }

                else if (requestType == "POST" && requestPath.StartsWith("Aircraft"))
                {
                    var split = requestPath.Split('/');

                    if (split.Length < 3 || split.Length > 4)
                    {
                        await BadRequestResponse(client, stream);
                        return;
                    }

                    var aircraft = _aircraft.FirstOrDefault(x => x.Callsign == split[1]);

                    var fdr = FDP2.GetFDRs.FirstOrDefault(x => x.Callsign == split[1]);

                    if (aircraft == null || fdr == null) 
                    {
                        await NotFoundResponse(client, stream);
                        return;
                    }

                    if (split[2] == "Squawk")
                    {
                        if (split.Length == 3) FDP2.SetASSR(fdr);

                        if (split.Length == 4) FDP2.SetASSR(fdr, Convert.ToInt32(split[3], 8));

                        await OkRequestResponse(client, stream, null);

                        return;
                    }

                    if (split[2] == "CFL")
                    {
                        if (split.Length == 3)
                        {
                            await BadRequestResponse(client, stream);
                            return;
                        }

                        FDP2.SetCFL(fdr, split[3]);

                        await OkRequestResponse(client, stream, null);

                        return;
                    }
                }

                await BadRequestResponse(client, stream);
                return;
            }
        }

        private async Task SendResponse(TcpClient client, NetworkStream stream, StringBuilder response)
        {
            var bytesResponse = Encoding.ASCII.GetBytes(response.ToString());

            await stream.WriteAsync(bytesResponse, 0, bytesResponse.Length);

            client.Close();
        }

        private async Task OkRequestResponse(TcpClient client, NetworkStream stream, string jsonObject)
        {
            var response = new StringBuilder();
            response.Append("HTTP/1.0 200 OK" + Environment.NewLine);
            response.Append(Environment.NewLine);
            if (jsonObject != null) response.Append(jsonObject);

            await SendResponse(client, stream, response);
        }

        private async Task BadRequestResponse(TcpClient client, NetworkStream stream)
        {
            var response = new StringBuilder();
            response.Append("HTTP/1.0 500 Bad Request" + Environment.NewLine);
            response.Append(Environment.NewLine);

            await SendResponse(client, stream, response);
        }

        private async Task NotFoundResponse(TcpClient client, NetworkStream stream)
        {
            var response = new StringBuilder();
            response.Append("HTTP/1.0 401 Not Found" + Environment.NewLine);
            response.Append(Environment.NewLine);

            await SendResponse(client, stream, response);
        }
    }
}
