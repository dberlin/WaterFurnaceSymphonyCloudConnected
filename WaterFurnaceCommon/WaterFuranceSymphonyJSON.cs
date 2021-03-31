namespace WaterFurnaceCommon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public enum ThermostatMode
    {
        Off = 0,
        Auto = 1,
        Cool = 2,
        Heat = 3,
        EHeat = 4,
    }

    public enum ThermostatModeOfOperation
    {
        Standby = 0,
        FanOnly = 1,
        Cooling1 = 2,
        Cooling2 = 3,
        Reheat = 4,
        Heating1 = 5,
        Heating2 = 6,
        EHeat = 7,
        AuxHeat = 8,
        Lockout = 9,
    }

    public enum AWLABCType
    {
        Unknown = 0,
        SingleSpeed = 1,
        DualSpeed = 2,
        VariableSpeed = 3,
    }

    public static class SymphonyCommandSource
    {
        public const string Dashboard = "consumer dashboard";
        public const string Thermostat = "tstat";
    }

    public class SymphonyCommand
    {
        [JsonProperty("cmd")] public string Command;
        [JsonProperty("sessionid")] public string SessionId;
        [JsonProperty("source")] public string Source;
        [JsonProperty("tid")] public uint TransactionId;
    }

    public class SymphonyReadCommand : SymphonyCommand
    {
        [JsonProperty("awlid")] public string AWLId;
        [JsonProperty("rlist")] public string[] RegisterList;
        [JsonProperty("zone")] public int Zone;
    }

    public class SymphonyWriteCommand : SymphonyCommand
    {
        [JsonProperty("awlid")] public string AWLId;
        [JsonProperty("zone")] public int Zone;
    }

    public class LoginResponse
    {
        [JsonProperty("firstname")] public string FirstName { get; set; }

        [JsonProperty("lastname")] public string LastName { get; set; }

        [JsonProperty("emailaddress")] public string EmailAddress { get; set; }

        [JsonProperty("key")] public int Key { get; set; }

        [JsonProperty("success")] public bool Success { get; set; }

        [JsonProperty("locations")] public List<Location> Locations { get; set; }

        [JsonProperty("rsp")] public string Response { get; set; }

        [JsonProperty("tid")] public int TransactionId { get; set; }

        [JsonProperty("err")] public string Error { get; set; }
    }

    public class Location
    {
        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("postal")] public string PostalCode { get; set; }

        [JsonProperty("city")] public string City { get; set; }

        [JsonProperty("state")] public string State { get; set; }

        [JsonProperty("country")] public string Country { get; set; }

        [JsonProperty("latitude")] public double Latitude { get; set; }

        [JsonProperty("longitude")] public double Longitude { get; set; }

        [JsonProperty("gateways")] public List<Gateway> Gateways { get; set; }
    }

    public class Gateway
    {
        [JsonProperty("gwid")] public string GatewayId { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("awltstattype")] public int AWLThermostatType { get; set; }

        [JsonProperty("awltstattypedesc")] public string AWLThermostatDescription { get; set; }

        [JsonProperty("iz2_max_zones")] public int IntelliZone2MaxZones { get; set; }

        [JsonProperty("awlabctypedesc")] public string AWLABCTypeDescription { get; set; }

        [JsonProperty("awlabctype")] public int AWLABCType { get; set; }

        [JsonProperty("blowertype")] public int BlowerType { get; set; }

        [JsonProperty("online")] public bool Online { get; set; }
        [JsonProperty("tstat_name")] public string ThermostatName { get; set; }
    }

    public class ReadResponse
    {
        [JsonProperty("rsp")] public string Response { get; set; }

        [JsonProperty("awlid")] public string AWLId { get; set; }

        [JsonProperty("tid")] public int TransactionId { get; set; }

        [JsonProperty("err")] public string Error { get; set; }

        [JsonProperty("compressorpower")] public int CompressorPower { get; set; }

        [JsonProperty("zone")] public int Zone { get; set; }

        [JsonProperty("fanpower")] public int FanPower { get; set; }

        [JsonProperty("auxpower")] public int AuxPower { get; set; }

        [JsonProperty("looppumppower")] public int LoopPumpPower { get; set; }

        [JsonProperty("totalunitpower")] public int TotalUnitPower { get; set; }

        [JsonProperty("awlabctype")] public AWLABCType AWLABCType { get; set; }

        [JsonProperty("modeofoperation")] public ThermostatModeOfOperation ModeOfOperation { get; set; }

        [JsonProperty("actualcompressorspeed")]
        public int ActualCompressorSpeed { get; set; }

        [JsonProperty("airflowcurrentspeed")] public int AirflowCurrentSpeed { get; set; }

        [JsonProperty("auroraoutputeh1")] public int AuroraOutputEH1 { get; set; }

        [JsonProperty("auroraoutputeh2")] public int AuroraOutputEH2 { get; set; }

        [JsonProperty("auroraoutputcc")] public int AuroraOutputTCC { get; set; }

        [JsonProperty("auroraoutputcc2")] public int AuroraOutputTCC2 { get; set; }

        [JsonProperty("tstatdehumidsetpoint")] public int ThermostatDehumidifySetPoint { get; set; }

        [JsonProperty("tstatrelativehumidity")]
        public int ThermostatRelativeHumidity { get; set; }

        [JsonProperty("leavingairtemp")] public double LeavingAirTemp { get; set; }

        [JsonProperty("tstatroomtemp")] public double ThermostatRoomTemp { get; set; }

        [JsonProperty("enteringwatertemp")] public double EnteringWaterTemp { get; set; }

        [JsonProperty("aocenteringwatertemp")] public double AOCEnteringWaterTemp { get; set; }

        [JsonProperty("auroraoutputrv")] public int AuroraOutputRV { get; set; }

        [JsonProperty("awltstattype")] public int AwlThermostatType { get; set; }

        [JsonProperty("humidity_offset_settings")]
        public HumidityOffsetSettings HumidityOffsetSettings { get; set; }
        //
        // [JsonProperty("iz2_humidity_offset_settings")]
        // public HumidityOffsetSettings Iz2HumidityOffsetSettings { get; set; }

        [JsonProperty("dehumid_humid_sp")] public DehumidifyHumiditySetPoint DehumidifyHumiditySetPoint { get; set; }

        // [JsonProperty("iz2_dehumid_humid_sp")]
        // public DehumidifyHumiditySetPoint Iz2DehumidifyHumiditySetPoint { get; set; }

        [JsonProperty("lockoutstatus")] public LockoutStatus LockoutStatus { get; set; }

        [JsonProperty("lastfault")] public int LastFault { get; set; }

        [JsonProperty("lastlockout")] public LastLockout LastLockout { get; set; }

        [JsonProperty("homeautomationalarm1")] public int HomeAutomationAlarm1 { get; set; }

        [JsonProperty("homeautomationalarm2")] public int HomeAutomationAlarm2 { get; set; }

        // [JsonProperty("roomtemp")] public int RoomTemp { get; set; }

        [JsonProperty("activesettings")] public ActiveSettings ActiveSettings { get; set; }

        // [JsonProperty("tstatactivesetpoint")] public int ThermostatActiveSetPoint { get; set; }
        //
        // [JsonProperty("tstatmode")] public ThermostatMode ThermostatMode { get; set; }
        //
        // [JsonProperty("tstatheatingsetpoint")] public int ThermostatHeatingSetPoint { get; set; }
        //
        // [JsonProperty("tstatcoolingsetpoint")] public int ThermostatCoolingSetPoint { get; set; }
    }

    public class ActiveSettings
    {
        [JsonProperty("temporaryoverride")] public int TemporaryOverride { get; set; }

        [JsonProperty("permanenthold")] public int PermanentHold { get; set; }

        [JsonProperty("vacationhold")] public int VacationHold { get; set; }

        [JsonProperty("onpeakhold")] public int OnPeakHold { get; set; }

        [JsonProperty("superboost")] public int Superboost { get; set; }

        [JsonProperty("tstatmode")] public int ThermostatMode { get; set; }

        [JsonProperty("activemode")] public ThermostatMode ActiveMode { get; set; }

        [JsonProperty("heatingsp_read")] public double HeatingSetPoint { get; set; }

        [JsonProperty("coolingsp_read")] public double CoolingSetPoint { get; set; }

        [JsonProperty("fanmode_read")] public int FanMode { get; set; }

        [JsonProperty("intertimeon_read")] public int InterTimeOn { get; set; }

        [JsonProperty("intertimeoff_read")] public int InterTimeOff { get; set; }
    }

    public class DehumidifyHumiditySetPoint
    {
        [JsonProperty("dehumidification")] public int Dehumidification { get; set; }

        [JsonProperty("humidification")] public int Humidification { get; set; }
    }

    public class HumidityOffsetSettings
    {
        [JsonProperty("humidity_offset")] public int HumidityOffset { get; set; }

        [JsonProperty("humdity_control_option")]
        public int HumidityControlOption { get; set; }

        [JsonProperty("dehumidification_mode")]
        public int DehumidificationMode { get; set; }

        [JsonProperty("humidification_mode")] public int HumidificationMode { get; set; }
    }

    public class LastLockout
    {
        [JsonProperty("lockoutstatuslast")] public int LockoutStatusLast { get; set; }
    }

    public class LockoutStatus
    {
        [JsonProperty("lockoutstatuscode")] public int LockoutStatusCode { get; set; }

        [JsonProperty("lockedout")] public int LockedOut { get; set; }
    }

    public class GatewayDiffResult
    {
        public readonly List<(Gateway Before, Gateway After)> Changed =
            new List<(Gateway, Gateway After)>();

        public List<Gateway> Added = new List<Gateway>();

        public List<Gateway> Removed = new List<Gateway>();


        public override string ToString()
        {
            return
                $"Added:{this.Added.ToPrettyJsonString()}\n" +
                $"Removed:{this.Removed.ToPrettyJsonString()}\n" +
                $"Changed:{this.Changed.ToPrettyJsonString()}";
        }
    }

    public static class ReadOnlyJsonExtensions
    {
        public static string ToPrettyJsonString<T>(this IReadOnlyCollection<T> arg)
        {
            return JsonConvert.SerializeObject(arg, Formatting.Indented);
        }
    }

    public static class DifferenceOfGateways
    {
        public static GatewayDiffResult GenerateDiff(List<Gateway> first,
            List<Gateway> second)
        {
            var result = new GatewayDiffResult();
            var sortedFirst = first;
            var sortedSecond = second;

            if (first == null || !first.Any())
            {
                // If the first list is empty, we've added whatever is on the second list.
                if (second != null) result.Added = second;

                return result;
            }

            // If the second list is empty, we've removed whatever is on the first list.
            if (!second.Any())
            {
                result.Removed = second;
                return result;
            }

            if (first.Count() > 1) sortedFirst = first.OrderBy(entry => entry.GatewayId).ToList();

            if (second.Count() > 1) sortedSecond = second.OrderBy(entry => entry.GatewayId).ToList();

            var firstIndex = 0;
            var secondIndex = 0;

            while (firstIndex < sortedFirst.Count() && secondIndex < sortedSecond.Count())
            {
                var firstEntry = sortedFirst[firstIndex];
                var secondEntry = sortedSecond[secondIndex];

                var compareResult =
                    string.Compare(firstEntry.GatewayId, secondEntry.GatewayId, StringComparison.Ordinal);
                if (compareResult < 0)
                {
                    // First entry was removed in second list
                    result.Removed.Add(firstEntry);
                    ++firstIndex;
                }
                else if (compareResult > 0)
                {
                    // Second entry was added to the first list
                    result.Added.Add(secondEntry);
                    ++secondIndex;
                }
                else
                {
                    // They are the same
                    if (firstEntry.ThermostatName != secondEntry.ThermostatName)
                        result.Changed.Add((firstEntry, secondEntry));

                    ++firstIndex;
                    ++secondIndex;
                }
            }

            // Anything left on the first list has been removed
            while (firstIndex < sortedFirst.Count())
            {
                result.Removed.Add(sortedFirst[firstIndex]);
                ++firstIndex;
            }

            // Anything left on the second list has been added
            while (secondIndex < sortedSecond.Count())
            {
                result.Added.Add(sortedSecond[secondIndex]);
                ++secondIndex;
            }

            return result;
        }
    }
}