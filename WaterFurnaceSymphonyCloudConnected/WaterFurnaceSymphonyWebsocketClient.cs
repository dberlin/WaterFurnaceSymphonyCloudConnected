namespace WaterFurnaceSymphonyCloudConnected
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Crestron.SimplSharp;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Rebex;
    using Rebex.Net;
    using Rebex.Security.Certificates;
    using WaterFurnaceCommon;

    /**
     * This class handles WebSocket related stuff.
     * We used to use the crestron websocket client, but it's very annoying and has various race conditions.
     * Now we use Rebex.
     */
    public class WaterFurnaceSymphonyWebsocketClient : IDisposable
    {
        private readonly WaterFurnaceSymphonyPlatformProtocol protocol;

        private CancellationTokenSource webSocketCancellation;

        // WebSocket client we are using to communicate with the symphony service
        private WebSocketClient wssClient;

        public WaterFurnaceSymphonyWebsocketClient(WaterFurnaceSymphonyPlatformProtocol protocol)
        {
            this.protocol = protocol;
        }

        public bool EnableLogging => this.protocol?.EnableLogging ?? false;

        public bool Connected => this.wssClient?.IsConnected ?? false;

        public void Dispose()
        {
            // Dispose of the client first to force exceptions on the threads .
            this.wssClient.Dispose();

            try
            {
                this.webSocketCancellation.Cancel();
                this.webSocketCancellation.Dispose();
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception during cancellation:{e}");
            }
        }

        public void Disconnect()
        {
            try
            {
                this.wssClient.Close();
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception during disconnect:{e}");
            }

            this.Dispose();
        }


        public bool Connect(string websocketUrl)
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Creating websocket client");
            this.webSocketCancellation = new CancellationTokenSource();
            this.wssClient = new WebSocketClient();
            this.wssClient.Settings.SslAllowedVersions |= TlsVersion.TLS12;
            this.wssClient.Settings.SslServerCertificateVerifier = new CrestronCertificateValidator();
            this.wssClient.LogWriter = new CrestronLogWriter(this.protocol) {Level = LogLevel.Debug};
            this.wssClient.ConnectAsync(websocketUrl, this.webSocketCancellation.Token).Wait();

            return true;
        }

        public Task SendWebSocketJson<T>(T thing)
        {
            var serialized = JsonConvert.SerializeObject(thing);
            var serializedBytes = Encoding.UTF8.GetBytes(serialized);
            return this.wssClient.SendAsync(new ArraySegment<byte>(serializedBytes), WebSocketMessageType.Text, true,
                this.webSocketCancellation.Token);
        }

        public async Task<T> ReceiveWebSocketJson<T>()
        {
            var bytes = await this.ReceiveWebSocketBytes();
            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JsonConvert.DeserializeObject<T>(converted);
        }

        public async Task<JObject> ReceiveWebSocketUnknownJson()
        {
            var bytes = await this.ReceiveWebSocketBytes();
            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JObject.Parse(converted);
        }

        private Task<byte[]> ReceiveWebSocketBytes()
        {
            return this.wssClient.ReceiveAsync<byte[]>(this.webSocketCancellation.Token);
            /*  var waitResult = receiveTask.Wait(2000, this.webSocketCancellation.Token);
  
              if (!waitResult)
                  throw new WebSocketException("Timed out waiting for response");
              return receiveTask.Result;*/
        }
    }

    public class CrestronCertificateValidator : ICertificateVerifier
    {
        public TlsCertificateAcceptance Verify(TlsSocket socket, string commonName, CertificateChain certificateChain)
        {
            var leafCommonName = certificateChain.LeafCertificate.GetCommonName();
            WaterFurnaceLogging.TraceMessage(true,
                $"Common name is {commonName}, leaf common name is {leafCommonName}");
            if (leafCommonName == "*.mywaterfurnace.com"
                || leafCommonName == "awlclientproxy.mywaterfurnace.com")
                return TlsCertificateAcceptance.Accept;
            return TlsCertificateAcceptance.CommonNameMismatch;
        }
    }

    public class CrestronLogWriter : LogWriterBase
    {
        private readonly WaterFurnaceSymphonyPlatformProtocol protocol;

        public CrestronLogWriter(WaterFurnaceSymphonyPlatformProtocol protocol)
        {
            this.protocol = protocol;
        }

        protected override void WriteMessage(string message)
        {
            if (!this.protocol.EnableLogging) return;
            CrestronConsole.PrintLine("Rebex: {0}", message);
        }
    }
}