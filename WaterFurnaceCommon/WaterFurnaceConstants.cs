namespace WaterFurnaceCommon
{
    using System.Text.RegularExpressions;

    public static class WaterFurnaceConstants
    {
        public const string AwlConfigUrl = "https://symphony.mywaterfurnace.com/assets/js/awlconfig.js.php";
        public const string SymphonyLoginUrl = "https://symphony.mywaterfurnace.com/account/login";
        public static readonly Regex WssRegex = new Regex(@"wss?://[^""']+");

        public static string[] DefaultReadList =
        {
            "compressorpower",
            "fanpower",
            "auxpower",
            "looppumppower",
            "totalunitpower",
            "AWLABCType",
            "ModeOfOperation",
            "ActualCompressorSpeed",
            "AirflowCurrentSpeed",
            "AuroraOutputEH1",
            "AuroraOutputEH2",
            "AuroraOutputCC",
            "AuroraOutputCC2",
            "TStatDehumidSetpoint",
            "TStatRelativeHumidity",
            "LeavingAirTemp",
            "TStatRoomTemp",
            "EnteringWaterTemp",
            "AOCEnteringWaterTemp",
            "auroraoutputrv",
            "AWLTStatType",
            "humidity_offset_settings",
            // "iz2_humidity_offset_settings",
            "dehumid_humid_sp",
            // "iz2_dehumid_humid_sp",
            "lockoutstatus",
            "lastfault",
            "lastlockout",
            "homeautomationalarm1",
            "homeautomationalarm2",
            // "roomtemp",
            "activesettings",
            // "TStatActiveSetpoint",
            // "TStatMode",
            // "TStatHeatingSetpoint",
            // "TStatCoolingSetpoint"
        };
    }
}