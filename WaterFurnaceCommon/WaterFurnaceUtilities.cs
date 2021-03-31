namespace WaterFurnaceCommon
{
    public static class WaterFurnaceUtilities
    {
        public static string FormatDeviceId(string awlId)
        {
            return $"WaterFurnaceSymphonySingleDevice-{awlId}";
        }
    }
}