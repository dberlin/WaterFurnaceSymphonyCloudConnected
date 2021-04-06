namespace WaterFurnaceCommon
{
    public interface IWaterFurnacePlatformProtocol
    {
        void SetHeatingSetPoint(IWaterFurnaceDevice device, int setPoint);
        void SetCoolingSetPoint(IWaterFurnaceDevice device, int setPoint);
    }
}