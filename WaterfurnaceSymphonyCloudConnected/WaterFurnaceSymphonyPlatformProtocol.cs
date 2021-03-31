namespace WaterFurnaceSymphonyCloudConnected
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Crestron.RAD.Common.Transports;
    using Crestron.RAD.DeviceTypes.Gateway;
    using Crestron.SimplSharp.CrestronWebSocketClient;
    using Flurl.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using WaterFurnaceCommon;
    using WaterFurnaceSymphonySingleDevice;
    using Timer = System.Timers.Timer;

    public class WaterFurnaceSymphonyPlatformProtocol : AGatewayProtocol, IWaterFurnacePlatformProtocol
    {
        private static string ToFormattedJson<T>(T thing)
        {
            return JsonConvert.SerializeObject(thing, Formatting.Indented);
        }

        public WaterFurnaceSymphonyPlatformProtocol(string username, string password,
            ISerialTransport transport, byte id) : base(transport, id)

        {
            this.WaterFurnaceUsername = username;
            this.WaterFurnacePassword = password;
            this.isAuthenticatedToWaterFurnace = false;
        }

        public void SetHeatingSetPoint(IWaterFurnaceDevice device, double setPoint)
        {
            this.heatingSetPointChanges.Enqueue((device, setPoint));
        }

        public void SetCoolingSetPoint(IWaterFurnaceDevice device, double setPoint)
        {
            this.coolingSetPointChanges.Enqueue((device, setPoint));
        }

        private void SendWebSocketJson<T>(T thing)
        {
            var serialized = JsonConvert.SerializeObject(thing);
            var serializedBytes = Encoding.UTF8.GetBytes(serialized);

            var sendResult = this.wssClient.Send(serializedBytes, (uint) serializedBytes.Length,
                WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME);
            if (sendResult != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Send did not complete, got result {sendResult}");
                throw new WebSocketException($"Send did not complete, got result {sendResult}");
            }
        }

        private T ReceiveWebSocketJson<T>()
        {
            var bytes = this.ReceiveWebSocketBytes();
            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JsonConvert.DeserializeObject<T>(converted);
        }

        private JObject ReceiveWebSocketUnknownJson()
        {
            var bytes = this.ReceiveWebSocketBytes();

            var converted = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return JObject.Parse(converted);
        }

        private byte[] ReceiveWebSocketBytes()
        {
            var receiveResult = this.wssClient.Receive(out var bytes, out var opcode);
            if (receiveResult != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Receive did not complete, got result {receiveResult}");
                throw new WebSocketException($"Receive did not okay, got result {receiveResult}");
            }

            if (opcode != WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Did not receive a text message, got {opcode} and {bytes} instead");
                throw new WebSocketException($"Did not receive a text message, got {opcode} and {bytes} instead");
            }

            return bytes;
        }

        /// <summary>
        ///     Connection changed event handler. Updates the status of child devices.
        /// </summary>
        /// <param name="connected">Whether the platform is connected or not.</param>
        protected override void ConnectionChangedEvent(bool connected)
        {
            // Make a copy since it may get updated
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"connection:{connected}");
            base.ConnectionChangedEvent(connected);
            // Return early if connection is valid. It tells us nothing about the lower level devices.
            if (connected) return;

            // We could reduce lock contention even further by only using the lock to make a copy of the device list
            // but this is not worth it for a list of 1-2 devices.
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "acquired device update lock");
            foreach (var device in this.waterFurnaceDevices.Values)
                device.SetConnectionStatus(false);
        }

        private void SessionTimeoutTriggered(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            this.sessionTimeoutTriggered = true;
            this.sessionId = string.Empty;
        }

        public void Start()
        {
            this.sessionTimeoutTimer = new Timer(SessionTimeoutLength);
            this.sessionTimeoutTimer.Elapsed += this.SessionTimeoutTriggered;
            this.backgroundLoginCancellation = new CancellationTokenSource();
            var token = this.backgroundLoginCancellation.Token;
            this.backgroundLoginTask = Task.Run(() => { this.BackgroundLoginHandler(token); }, token);
        }

        /**
         * This routine handles ensuring we stay logged in and that the websocket client is connected and running
         * properly.
         * The login dance is complicated enough that having it handled this way, rather than trying to handle it
         * during polling, is a lot less complicated.
         */
        private void BackgroundLoginHandler(CancellationToken cancelToken)
        {
            while (true)
            {
                try
                {
                    if (cancelToken.IsCancellationRequested)
                        return;

                    if (this.sessionTimeoutTriggered || !this.isAuthenticatedToWaterFurnace || this.wssClient == null ||
                        !this.wssClient.Connected)
                    {
                        this.sessionTimeoutTriggered = false;
                        this.isAuthenticatedToWaterFurnace = false;
                        this.RestartWebsocketConnection();
                    }

                    var isAlive = this.wssClient.Connected;
                    if (this.IsConnected != isAlive)
                        this.ConnectionChanged(isAlive);
                    this.PollSymphony();
                }
                catch (Exception e)
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception in login handler:{e}");
                }

                Thread.Sleep(5000);
            }
        }

        public void Stop()
        {
            try
            {
                this.backgroundLoginCancellation.Cancel();
                this.backgroundLoginTask.Wait();
                this.backgroundLoginCancellation.Dispose();
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception during cancellation:{e}");
            }

            this.waterFurnaceCookies = null;
            this.sessionTimeoutTimer.Stop();
            this.sessionTimeoutTimer = null;
            this.isAuthenticatedToWaterFurnace = false;
            this.wssClient = null;
        }

        private void LoginUsingWeb()
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Starting web login");
            this.sessionId = string.Empty;
            this.sessionTimeoutTimer.Stop();
            this.waterFurnaceCookies =
                new CookieJar().AddOrReplace("legal-acknowledge", "yes", WaterFurnaceConstants.SymphonyLoginUrl);
            try
            {
                var loginResult = WaterFurnaceConstants.SymphonyLoginUrl.WithAutoRedirect(false)
                    .WithCookies(this.waterFurnaceCookies).PostUrlEncodedAsync(new
                    {
                        op = "login",
                        redirect = "/",
                        emailaddress = this.WaterFurnaceUsername,
                        password = this.WaterFurnacePassword,
                    }).Result;

                if (loginResult != null && loginResult.StatusCode == 302 && loginResult.Cookies.Count == 1)
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Successfully performed HTTP Login");

                    this.isAuthenticatedToWaterFurnace = true;
                    this.sessionTimeoutTimer.Interval = SessionTimeoutLength;
                    this.sessionTimeoutTimer.Start();
                    // Return session ID

                    this.sessionId = loginResult.Cookies[0].Value;
                }
                else
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                        $"Error Authenticating:{loginResult?.StatusCode}");
                }
            }
            catch (AggregateException e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Error authenticating:{e}");
            }
        }

        private static string GetWebSocketUrl()
        {
            var configResult = WaterFurnaceConstants.AwlConfigUrl.GetStringAsync().Result;
            var wssMatch = WaterFurnaceConstants.WssRegex.Match(configResult);
            return wssMatch.Success ? wssMatch.Value : "";
        }

        private void RestartWebsocketConnection()
        {
            if (!this.isAuthenticatedToWaterFurnace)
                this.LoginUsingWeb();
            if (this.sessionId == string.Empty)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    "Session ID was not set, must not have authenticated properly");
                return;
            }

            var websocketUrl = GetWebSocketUrl();
            if (websocketUrl == string.Empty)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Could not find websocket URL");
                return;
            }

            this.SetupWssClient(websocketUrl);
        }

        private void SetupWssClient(string websocketUrl)
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Creating websocket client");
            this.wssClient = new WebSocketClient
            {
                URL = websocketUrl,
                Port = WebSocketClient.WEBSOCKET_DEF_SSL_SECURE_PORT,
                DisconnectCallBack = this.WebSocketDisconnectHandler,
                SSL = true
            };

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Performing connect to {this.wssClient.URL}");
            var connectResult = this.wssClient.Connect();
            if (connectResult != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Error while connecting to WebSocket:{connectResult}");
                return;
            }

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Connect done, starting login");

            this.PerformWebSocketLogin();
        }

        private int WebSocketDisconnectHandler(WebSocketClient.WEBSOCKET_RESULT_CODES error, object o1)
        {
            if (this.IsConnected)
                this.ConnectionChanged(false);
            this.isAuthenticatedToWaterFurnace = false;
            return 0;
        }

        private void PerformWebSocketLogin()
        {
            this.transactionCounter = new WaterFurnaceSymphonyTransactionCounter();
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Created transaction counter");

            // Send login command
            this.SendWebSocketJson(new SymphonyCommand
            {
                Command = "login",
                TransactionId = this.transactionCounter.GetNextTransactionId(),
                SessionId = this.sessionId,
                Source = SymphonyCommandSource.Dashboard,
            });

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Sent JSON");
            var loginResponse = this.ReceiveWebSocketJson<LoginResponse>();

            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Received JSON");
            this.HandleLoginResponse(loginResponse);
            if (!this.IsConnected)
                this.ConnectionChanged(true);
        }

        private void HandleLoginResponse(LoginResponse loginResponse)
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                $"Login response is {ToFormattedJson(loginResponse)}");
            if (!loginResponse.Success)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Login failed, restarting client!");
                this.wssClient.Disconnect();
            }


            var gateways = new List<Gateway>();
            if (!loginResponse.Locations.Any()) return;
            gateways = loginResponse.Locations.Aggregate(gateways,
                (current, location) => current.Concat(location.Gateways).ToList());
            this.ProcessDeviceChanges(gateways);
        }

        private void ProcessDeviceChanges(List<Gateway> gatewayList)
        {
            try
            {
                var gatewayListDiff = DifferenceOfGateways.GenerateDiff(this.lastLoginGatewayList, gatewayList);
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Old gateway list:{JsonConvert.SerializeObject(this.lastLoginGatewayList)}");
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"New gateway list:{JsonConvert.SerializeObject(gatewayList)}");
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Added gateway list:{JsonConvert.SerializeObject(gatewayListDiff.Added)}");
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Removed gateway list:{JsonConvert.SerializeObject(gatewayListDiff.Removed)}");
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Changed gateway list:{JsonConvert.SerializeObject(gatewayListDiff.Changed)}");
                foreach (var entry in gatewayListDiff.Removed)
                {
                    var deviceId = WaterFurnaceUtilities.FormatDeviceId(entry.GatewayId);

                    WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                        $"Removing paired device for ID: {deviceId}");
                    if (!this.waterFurnaceDevices.Remove(deviceId))
                    {
                        WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                            $"When trying to remove: missing key {deviceId} in the device dictionary!");
                        continue;
                    }

                    this.RemovePairedDevice(deviceId);
                }

                foreach (var entry in gatewayListDiff.Added)
                {
                    var deviceId = WaterFurnaceUtilities.FormatDeviceId(entry.GatewayId);
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                        $"Adding paired device for ID: {deviceId}");
                    if (this.waterFurnaceDevices.ContainsKey(deviceId))
                    {
                        WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                            $"When trying to add: Already have key {deviceId} in the device dictionary!");
                        continue;
                    }

                    var deviceInstance = new WaterFurnaceSymphonySingleDevice(entry.GatewayId,
                        entry.ThermostatName, this);

                    var pairedDeviceInfo = new GatewayPairedDeviceInformation(deviceId,
                        deviceInstance.ThermostatName, deviceInstance.Description, deviceInstance.Manufacturer,
                        deviceInstance.Model, deviceInstance.DeviceType,
                        deviceInstance.DeviceSubType);
                    this.waterFurnaceDevices.Add(deviceId, deviceInstance);
                    this.AddPairedDevice(pairedDeviceInfo, deviceInstance);
                    deviceInstance.SetConnectionStatus(entry.Online);
                }

                foreach (var entry in gatewayListDiff.Changed)
                {
                    var beforeDeviceId = WaterFurnaceUtilities.FormatDeviceId(entry.Before.GatewayId);
                    if (!this.waterFurnaceDevices.ContainsKey(beforeDeviceId))
                    {
                        WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                            $"When trying to change: missing key {beforeDeviceId} in the device dictionary!");
                        continue;
                    }

                    var deviceInstance = this.waterFurnaceDevices[beforeDeviceId];

                    var pairedDeviceInfo = new GatewayPairedDeviceInformation(beforeDeviceId,
                        entry.After.ThermostatName, deviceInstance.Description, deviceInstance.Manufacturer,
                        deviceInstance.Model, deviceInstance.DeviceType,
                        deviceInstance.DeviceSubType);

                    deviceInstance.SetThermostatName(entry.After.ThermostatName);
                    this.UpdatePairedDevice(beforeDeviceId, pairedDeviceInfo);
                }

                this.lastLoginGatewayList = gatewayList;
            }
            catch (Exception e)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Exception processing devices:{e}");
            }
        }

        private void HandleReadResponse(ReadResponse readResponse)
        {
            var deviceId = WaterFurnaceUtilities.FormatDeviceId(readResponse.AWLId);

            this.waterFurnaceDevices.TryGetValue(deviceId, out var correctDevice);

            if (correctDevice == null)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    $"Could not find key {deviceId} in device list, returning");
                return;
            }

            correctDevice.RefreshDeviceWithData(readResponse);
        }

        private void PollSymphony()
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Polling symphony");

            if (this.wssClient == null || !this.wssClient.Connected)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Not connected to service during Poll");
                return;
            }


            var finalDeviceUpdates = new Dictionary<IWaterFurnaceDevice, Dictionary<string, object>>();

            // Process any pending setpoint changes.  
            foreach (var device in this.waterFurnaceDevices.Values)
                finalDeviceUpdates[device] = new Dictionary<string, object>();

            while (this.heatingSetPointChanges.TryDequeue(out var queuePair))
                finalDeviceUpdates[queuePair.device]["heatingsp_write"] = queuePair.temperature;
            while (this.coolingSetPointChanges.TryDequeue(out var queuePair))
                finalDeviceUpdates[queuePair.device]["coolingsp_write"] = queuePair.temperature;

            foreach (var device in this.waterFurnaceDevices.Values)
            {
                try
                {
                    // See if there are writes to be made
                    if (finalDeviceUpdates.TryGetValue(device, out var deviceUpdate))
                        if (deviceUpdate.Any())
                            this.SendDeviceUpdate(device, deviceUpdate);

                    var readResponse = this.ReadDeviceData(device);
                    this.HandleReadResponse(readResponse);
                }
                catch (Exception e)
                {
                    WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                        $"Exception while sending read command:{e}");
                }
            }
        }

        private ReadResponse ReadDeviceData(IWaterFurnaceDevice device)
        {
            var readCommand = new SymphonyReadCommand
            {
                Source = SymphonyCommandSource.Dashboard,
                TransactionId = this.transactionCounter.GetNextTransactionId(),
                Command = "read",
                SessionId = this.sessionId,
                Zone = 0,
                AWLId = device.AWLId,
                RegisterList = WaterFurnaceConstants.DefaultReadList,
            };
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, $"Sending JSON to device:{device.AWLId}");
            this.SendWebSocketJson(readCommand);
            var readResponse = this.ReceiveWebSocketJson<ReadResponse>();
            WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                $"Received JSON from device {device.AWLId}:{ToFormattedJson(readResponse)}");
            return readResponse;
        }

        private void SendDeviceUpdate(IWaterFurnaceDevice device, Dictionary<string, object> deviceUpdate)
        {
            var writeCommand = new SymphonyWriteCommand
            {
                Source = SymphonyCommandSource.Dashboard,
                TransactionId = this.transactionCounter.GetNextTransactionId(),
                Zone = 0,
                AWLId = device.AWLId,
                SessionId = this.sessionId,
                Command = "write",
            };
            var writeJson = JObject.FromObject(writeCommand);
            foreach (var keyValue in deviceUpdate)
                writeJson.Add(keyValue.Key, JToken.FromObject(keyValue.Value));

            WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                $"Sending write command {writeJson}");
            this.SendWebSocketJson(writeJson);
            var writeResult = this.ReceiveWebSocketUnknownJson();
            WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                $"Received result from write command:{writeResult}");
        }


        #region Fields

        internal string WaterFurnaceUsername { get; set; }
        internal string WaterFurnacePassword { get; set; }

        // Set of cookies from the WaterFurnace website
        private CookieJar waterFurnaceCookies;

        // Whether we are authenticated to the WaterFurnace platform
        private bool isAuthenticatedToWaterFurnace;

        // Current session ID from logging in
        private string sessionId;

        // Whether a session timeout was triggered
        private bool sessionTimeoutTriggered;

        // Timer that handles session timeouts
        private Timer sessionTimeoutTimer;

        // Session timeout. WaterFurnace JS has this at 1500 seconds.
        private const double SessionTimeoutLength = 1500 * 1000; // 15 minutes

        // List of instantiated devices
        private readonly Dictionary<string, IWaterFurnaceDevice> waterFurnaceDevices =
            new Dictionary<string, IWaterFurnaceDevice>();

        // Transaction Id generator for symphony commands
        private WaterFurnaceSymphonyTransactionCounter transactionCounter;

        // WebSocket client we are using to communicate with the symphony service
        private WebSocketClient wssClient;

        // List of gateways that existed as of our last poll. May be empty/null.
        private List<Gateway> lastLoginGatewayList;

        // Background thread that handles staying logged in
        private Task backgroundLoginTask;
        private CancellationTokenSource backgroundLoginCancellation;

        // Queue of heating setpoint changes requested by devices
        private readonly ConcurrentQueue<(IWaterFurnaceDevice device, double temperature)> heatingSetPointChanges =
            new ConcurrentQueue<(IWaterFurnaceDevice device, double temperature)>();

        // Queue of cooling setpoint changes requested by devices
        private readonly ConcurrentQueue<(IWaterFurnaceDevice device, double temperature)> coolingSetPointChanges =
            new ConcurrentQueue<(IWaterFurnaceDevice device, double temperature)>();

        #endregion Fields
    }
}