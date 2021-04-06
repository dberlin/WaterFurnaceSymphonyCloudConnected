namespace WaterFurnaceSymphonyCloudConnected
{
    using Crestron.RAD.Common.Transports;
    using WaterFurnaceCommon;

    public class WaterFurnaceSymphonyTransport : ATransportDriver
    {
        public WaterFurnaceSymphonyTransport()
        {
            this.IsConnected = true;
            this.IsEthernetTransport = true;
        }

        public override void SendMethod(string message, object[] paramaters)
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging);
        }

        public override void Start()
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging);
        }

        public override void Stop()
        {
            WaterFurnaceLogging.TraceMessage(this.EnableLogging);
        }
    }
}