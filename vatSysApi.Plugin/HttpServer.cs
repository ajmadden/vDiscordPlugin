using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordPlugin.Plugin
{
    public class HttpServer
    {
        private Task _httpListenTask;
        private CancellationToken _cancellationToken;
        private int _listenPort;

        public HttpServer(int port, CancellationToken token)
        {
            _listenPort = port;
            _cancellationToken = token;

            StartHttpListener();
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

            var expression = @"(GET|POST)( /)(.*)( )(HTTP/1.1)";

            var match = Regex.Match(request, expression);

            if (!match.Success)
            {
                await BadRequestResponse(client, stream);
                return;
            }
            else
            {
                await OkRequestResponse(client, stream, JsonConvert.SerializeObject(DiscordPlugin.Details, Formatting.Indented));
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
