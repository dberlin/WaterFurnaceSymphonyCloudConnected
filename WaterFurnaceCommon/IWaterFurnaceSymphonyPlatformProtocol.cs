namespace WaterFurnaceCommon
{
    public interface IWaterFurnaceSymphonyPlatformProtocol
    {
        void SetHeatingSetPoint(IWaterFurnaceDevice device, int setPoint);
        void SetCoolingSetPoint(IWaterFurnaceDevice device, int setPoint);
    }
}