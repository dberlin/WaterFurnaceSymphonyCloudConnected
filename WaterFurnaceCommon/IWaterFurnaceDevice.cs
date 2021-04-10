namespace WaterFurnaceCommon
{
    public interface IWaterFurnaceDevice
    {
        string DeviceId { get; }
        string AWLId { get; }
        string ThermostatName { get; }
        string Description { get; }
        string Manufacturer { get; }
        string Model { get; }
        string DeviceType { get; }
        string DeviceSubType { get; }
        void SetConnectionStatus(bool connected);
        void SetThermostatName(string name);
        void RefreshDeviceWithData(ReadResponse response);
    }
}