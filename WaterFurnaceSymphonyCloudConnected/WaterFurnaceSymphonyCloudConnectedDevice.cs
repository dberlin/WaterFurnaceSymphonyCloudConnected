namespace WaterFurnaceSymphonyCloudConnected
{
    using System.Globalization;
    using Crestron.RAD.Common.Interfaces;
    using Crestron.RAD.DeviceTypes.Gateway;
    using Crestron.SimplSharp;
    using Flurl.Http;
    using Flurl.Http.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Rebex;
    using WaterFurnaceCommon;

    public class WaterFurnaceSymphonyCloudConnectedDevice : AGateway, ICloudConnected
    {
        public WaterFurnaceSymphonyCloudConnectedDevice()
        {
            Licensing.Key = RebexLicensing.LicensingKey;
            FlurlHttp.Configure(settings =>
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    Converters =
                    {
                        new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal},
                    },
                };
                settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);
            });
            this.EnableLogging = true;
            this.EnableRxDebug = true;
            this.EnableTxDebug = true;
        }

        public void Initialize()
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging, "Initializing WaterFurnace Driver");
            this.ConnectionTransport = new WaterFurnaceSymphonyTransport();
            var waterFurnaceProtocol = new WaterFurnaceSymphonyPlatformProtocol(this.driverUsername,
                this.driverPassword,
                this.ConnectionTransport, this.Id);
            this.Protocol = waterFurnaceProtocol;
            this.Protocol.Initialize(this.DriverData);
        }

        public override void Connect()
        {
            base.Connect();
            if (this.Protocol == null)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    "WaterFurnace Platform Protocol was null when Connect was called");
                ErrorLog.Error("WaterFurnace Platform Protocol was null when Connect was called");

                return;
            }

            WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                "WaterFurnace Platform Protocol starting");
            ((WaterFurnaceSymphonyPlatformProtocol) this.Protocol).Start();
        }

        public override void Disconnect()
        {
            base.Disconnect();
            if (this.Protocol == null)
            {
                WaterFurnaceLogging.TraceMessage(this.EnableLogging,
                    "WaterFurnace Platform Protocol was null when Disconnect was called");
                ErrorLog.Error("WaterFurnace Platform Protocol was null when Disconnect was called");
                return;
            }

            ((WaterFurnaceSymphonyPlatformProtocol) this.Protocol).Stop();
        }

        public override void OverridePassword(string password)
        {
            this.driverPassword = password;
            if (this.Protocol != null)
                ((WaterFurnaceSymphonyPlatformProtocol) this.Protocol).WaterFurnacePassword = password;
        }

        public override void OverrideUsername(string username)
        {
            this.driverUsername = username;
            if (this.Protocol != null)
                ((WaterFurnaceSymphonyPlatformProtocol) this.Protocol).WaterFurnaceUsername = username;
        }

        #region Private fields

        private string driverPassword;
        private string driverUsername;

        #endregion Private fields

        #region Driver overrides

        public override bool SupportsDisconnect => true;
        public override bool SupportsReconnect => true;

        #endregion Driver overrides
    }
}