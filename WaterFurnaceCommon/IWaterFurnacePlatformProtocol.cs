namespace WaterFurnaceCommon
{
    public interface IWaterFurnacePlatformProtocol
    {
        void SetHeatingSetPoint(IWaterFurnaceDevice device, double setPoint);
        void SetCoolingSetPoint(IWaterFurnaceDevice device, double setPoint);

    }
}