namespace WaterFurnaceSymphonyCloudConnected
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Crestron.SimplSharp.CrestronWebSocketClient;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using WaterFurnaceCommon;

    /**
     * This class handles WebSocket related stuff
     * Unfortunately, they way Crestron does things makes no standard WebSocket client library work
     * (including websocket-client, websocket-sharp, etc).  The Crestron provided Websocket client
     * also doesn't provide enough functionality to port the libraries that rely on .net WebSocket functionality
     * to crestron home.  Under the covers, it's really a C library being exposed through .net.
     * Overall, the situation is a mess.  We try to abstract it here
     */
    public class WaterFurnaceSymphonyWebsocketClient : IDisposable
    {
        public WebSocketClient.WebSocketClientAsyncDisconnectCallback DisconnectCallback;
        private readonly WaterFurnaceSymphonyPlatformProtocol protocol;
        private CancellationTokenSource webSocketRecvCancellation;
        private const int TakeTimeout = 10000;

        private readonly BlockingCollection<byte[]>
            webSocketRecvQueue = new BlockingCollection<byte[]>();

        // Background task that reads websocket responses
        private Task webSocketRecvTask;
        private CancellationTokenSource webSocketSendCancellation;

        private readonly BlockingCollection<(byte[] bytes, WebSocketClient.WEBSOCKET_PACKET_TYPES packetType)>
            webSocketSendQueue =
                new BlockingCollection<(byte[] bytes, WebSocketClient.WEBSOCKET_PACKET_TYPES packetType)>();


        // Background task that reads websocket responses
        private Task webSocketSendTask;

        // WebSocket client we are using to communicate with the symphony service
        private WebSocketClient wssClient;

        public WaterFurnaceSymphonyWebsocketClient(WaterFurnaceSymphonyPlatformProtocol protocol)
        {
            this.protocol = protocol;
        }

        public bool EnableLogging => this.protocol?.EnableLogging ?? false;

        public bool Connected => this.wssClient?.Connected ?? false;

        public void Dispose()
        {
            // Dispose of the client first to force exceptions on the threads .
            this.wssClient.Dispose();
            this.wssClient = null;
            try
            {
                this.webSocketRecvCancellation.Cancel();
                this.webSocketRecvTask.Wait();
                this.webSocketRecvCancellation.Dispose();
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception during cancellation:{e}");
            }

            try
            {
                this.webSocketSendCancellation.Cancel();
                this.webSocketSendTask.Wait();
                this.webSocketSendCancellation.Dispose();
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception during cancellation:{e}");
            }

            this.webSocketSendQueue.Dispose();
            this.webSocketRecvQueue.Dispose();
        }

        public void Disconnect()
        {
            this.wssClient.Disconnect();
            this.Dispose();
        }

        private int WebSocketReceiveCallback(byte[] bytes, uint bytesLen,
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode,
            WebSocketClient.WEBSOCKET_RESULT_CODES result)
        {
            if (result != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Receive did not complete, got result {result}");
                return (int) result;
            }

            if (opcode == WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME)
            {
                this.webSocketRecvQueue.Add(bytes, this.webSocketRecvCancellation.Token);
            }
            else if (opcode == WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__PING)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Received ping, sending pong!");
                this.webSocketSendQueue.Add((null, WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__PONG),
                    this.webSocketSendCancellation.Token);
            }
            else
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Did not receive a text message, got {opcode} and {bytes} instead");
                throw new WebSocketException(
                    $"Did not receive a text message, got {opcode} and {bytes} instead");
            }

            return 0; // success
        }

        private void BackgroundWebSocketRecvHandler(CancellationToken cancelToken)
        {
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                try
                {
                    this.wssClient.ReceiveAsync();
                    Thread.Sleep(200);
                }
                catch (Exception e)
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception while receiving:{e}");
                }
            }
        }

        private void BackgroundWebSocketSendHandler(CancellationToken cancelToken)
        {
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                try
                {
                    this.webSocketSendQueue.TryTake(out var takeOutput, TakeTimeout, cancelToken);

                    var result = this.wssClient.SendAsync(takeOutput.bytes, (uint) (takeOutput.bytes?.Length ?? 0),
                        takeOutput.packetType);
                    if (result != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PENDING)
                        WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                            $"Send did not complete successfully, got result:{result}");
                    Thread.Sleep(200);
                }
                catch (Exception e)
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception while sending:{e}");
                }
            }
        }

        public bool Connect(string websocketUrl)
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Creating websocket client");

            this.wssClient = new WebSocketClient
            {
                URL = websocketUrl,
                Port = WebSocketClient.WEBSOCKET_DEF_SSL_SECURE_PORT,
                DisconnectCallBack = this.DisconnectCallback,
                SSL = true,
                KeepAlive = false,
                ReceiveCallBack = this.WebSocketReceiveCallback,
            };

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Performing connect to {this.wssClient.URL}");
            var connectResult = this.wssClient.Connect();
            if (connectResult != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Error while connecting to WebSocket:{connectResult}");
                return false;
            }

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Connect done, starting login");
            this.webSocketSendCancellation = new CancellationTokenSource();
            var sendToken = this.webSocketSendCancellation.Token;
            this.webSocketRecvCancellation = new CancellationTokenSource();
            var recvToken = this.webSocketRecvCancellation.Token;
            this.webSocketSendTask = Task.Run(() => { this.BackgroundWebSocketSendHandler(sendToken); }, sendToken);
            this.webSocketRecvTask = Task.Run(() => { this.BackgroundWebSocketRecvHandler(recvToken); }, recvToken);

            return true;
        }

        public void SendWebSocketJson<T>(T thing)
        {
            var serialized = JsonConvert.SerializeObject(thing);
            var serializedBytes = Encoding.UTF8.GetBytes(serialized);

            this.webSocketSendQueue.Add((serializedBytes,
                    WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME),
                this.webSocketSendCancellation.Token);
        }

        public T ReceiveWebSocketJson<T>()
        {
            var bytes = this.ReceiveWebSocketBytes();
            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JsonConvert.DeserializeObject<T>(converted);
        }

        public JObject ReceiveWebSocketUnknownJson()
        {
            var bytes = this.ReceiveWebSocketBytes();

            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JObject.Parse(converted);
        }

        private byte[] ReceiveWebSocketBytes()
        {
            if (this.webSocketRecvQueue.TryTake(out var takeResult, TakeTimeout, webSocketRecvCancellation.Token))
                return takeResult;
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "TryTake failed in ReceiveWebSocketBytes");
            throw new TimeoutException("TryTake failed in ReceiveWebSocketBytes");

        }
    }
}