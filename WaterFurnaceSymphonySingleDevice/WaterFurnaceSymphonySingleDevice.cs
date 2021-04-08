namespace WaterFurnaceSymphonySingleDevice
{
    using Crestron.RAD.Common.Enums;
    using Crestron.RAD.Common.Interfaces;
    using Crestron.RAD.Common.Interfaces.ExtensionDevice;
    using Crestron.RAD.DeviceTypes.ExtensionDevice;
    using WaterFurnaceCommon;

    public class WaterFurnaceSymphonySingleDevice : AExtensionDevice, ICloudConnected, IWaterFurnaceDevice
    {
        #region Private driver fields

        private readonly IWaterFurnacePlatformProtocol protocol;

        #endregion Private driver fields

        public WaterFurnaceSymphonySingleDevice(string awlId, string thermostatName,
            string modelName,
            IWaterFurnacePlatformProtocol protocol)
        {
            this.DeviceId = WaterFurnaceUtilities.FormatDeviceId(awlId);
            this.AWLId = awlId;
            this.ThermostatName = thermostatName;
            this.Model = modelName;
            this.protocol = protocol;
            this.CreateDeviceDefinition();
            this.Initialize();
        }

        #region ICloudConnected members

        public void Initialize()
        {
            this.thermostatModeIcon.Value = "#icClimateRegular";
            this.thermostatUnits.Value = "Fahrenheit";
            this.thermostatHeatModeEnabled.Value = true;
            this.thermostatCoolModeEnabled.Value = true;
            this.thermostatAutoModeEnabled.Value = false;

            this.Commit();
        }

        #endregion

        /// <summary>
        ///     Change the connection status of this device.
        ///     This is usually set from either the platform protocol (if it can't connect)
        ///     or locally when the device status is retrieved from the platform.
        /// </summary>
        /// <param name="connected">Whether the device is connected to the platform</param>
        public void SetConnectionStatus(bool connected)
        {
            if (this.Connected == connected) return;
            this.Connected = connected;
            this.connectionStateIcon.Value = connected ? "#icClimateRegular" : "#icClimateDisabled";
        }

        public void SetThermostatName(string name)
        {
            this.ThermostatName = name;
        }

        public void RefreshDeviceWithData(ReadResponse data)
        {
            this.waterTempLabel.Value = $"{data.EnteringWaterTemp}°F";
            this.returnAirTempLabel.Value = $"{data.LeavingAirTemp}°F";
            this.totalPowerUsageLabel.Value = $"{data.TotalUnitPower}W";
            this.compressorPowerUsageLabel.Value = $"{data.CompressorPower}W";
            this.fanPowerUsageLabel.Value = $"{data.FanPower}W";
            this.pumpPowerUsageLabel.Value = $"{data.LoopPumpPower}W";
            this.auxHeatPowerUsageLabel.Value = $"{data.AuxPower}W";
            this.thermostatCurrentTemperature.Value = data.ThermostatRoomTemp;
            this.thermostatCoolSetPoint.Value = data.ActiveSettings.CoolingSetPoint;
            this.thermostatHeatSetPoint.Value = data.ActiveSettings.HeatingSetPoint;
            switch (data.ModeOfOperation)
            {
                case ThermostatModeOfOperation.Standby:
                {
                    this.thermostatCurrentTempLabel.Value = "Standby";
                    this.thermostatModeIcon.Value = "#icClimateRegular";
                    break;
                }
                case ThermostatModeOfOperation.FanOnly:
                {
                    this.thermostatCurrentTempLabel.Value = "Fan only";
                    this.thermostatModeIcon.Value = "#icFanOn";
                    break;
                }
                case ThermostatModeOfOperation.AuxHeat:
                {
                    this.thermostatCurrentTempLabel.Value = "Aux Heat";
                    this.thermostatModeIcon.Value = "#icFireOn";
                    break;
                }
                case ThermostatModeOfOperation.Cooling1:
                {
                    this.thermostatCurrentTempLabel.Value = data.AWLABCType == AWLABCType.VariableSpeed
                        ? $"Cooling Speed {data.ActualCompressorSpeed}"
                        : "Cooling Stage 1";
                    this.thermostatModeIcon.Value = "#icCoolingRegular";
                    break;
                }
                case ThermostatModeOfOperation.Cooling2:
                {
                    this.thermostatCurrentTempLabel.Value = data.AWLABCType == AWLABCType.VariableSpeed
                        ? $"Cooling Speed {data.ActualCompressorSpeed}"
                        : "Cooling Stage 2";
                    this.thermostatModeIcon.Value = "#icCoolingRegular";
                    break;
                }
                case ThermostatModeOfOperation.Heating1:
                {
                    this.thermostatCurrentTempLabel.Value = data.AWLABCType == AWLABCType.VariableSpeed
                        ? $"Heating Speed {data.ActualCompressorSpeed}"
                        : "Heating Stage 1";
                    this.thermostatModeIcon.Value = "#icHeatingRegular";
                    break;
                }
                case ThermostatModeOfOperation.Heating2:
                {
                    this.thermostatCurrentTempLabel.Value = data.AWLABCType == AWLABCType.VariableSpeed
                        ? $"Heating Speed {data.ActualCompressorSpeed}"
                        : "Heating Stage 2";
                    this.thermostatModeIcon.Value = "#icHeatingRegular";
                    break;
                }
            }

            this.Commit();
        }

        private void CreateDeviceDefinition()
        {
            this.connectionStateIcon =
                this.CreateProperty<string>(new PropertyDefinition(ConnectionStateIconKey, null,
                    DevicePropertyType.String));
            this.thermostatUnits =
                this.CreateProperty<string>(new PropertyDefinition(ThermostatUnitsKey, null,
                    DevicePropertyType.String));
            this.thermostatModeIcon =
                this.CreateProperty<string>(new PropertyDefinition(ThermostatModeIconKey, null,
                    DevicePropertyType.String));
            this.thermostatHeatModeEnabled =
                this.CreateProperty<bool>(new PropertyDefinition(ThermostatHeatModeEnabledKey, null,
                    DevicePropertyType.Boolean));
            this.thermostatCoolModeEnabled =
                this.CreateProperty<bool>(new PropertyDefinition(ThermostatCoolModeEnabledKey, null,
                    DevicePropertyType.Boolean));
            this.thermostatAutoModeEnabled =
                this.CreateProperty<bool>(new PropertyDefinition(ThermostatAutoModeEnabledKey, null,
                    DevicePropertyType.Boolean));
            this.thermostatAutoSetPoint = this.CreateProperty<int>(new PropertyDefinition(ThermostatAutoSetPointKey,
                null, DevicePropertyType.Int32, 50, 90, 1));
            this.thermostatHeatSetPoint = this.CreateProperty<int>(new PropertyDefinition(ThermostatHeatSetPointKey,
                null, DevicePropertyType.Int32, 45, 80, 1));
            this.thermostatCoolSetPoint = this.CreateProperty<int>(new PropertyDefinition(ThermostatCoolSetPointKey,
                null, DevicePropertyType.Int32, 65, 90, 1));
            this.thermostatCurrentTemperature = this.CreateProperty<double>(new PropertyDefinition(
                ThermostatCurrentTemperatureKey, null, DevicePropertyType.Double, 50, 95, 0.1));
            this.thermostatCurrentTempLabel = this.CreateProperty<string>(new PropertyDefinition(
                ThermostatCurrentTempLabelKey, null,
                DevicePropertyType.String));
            this.waterTempLabel =
                this.CreateProperty<string>(new PropertyDefinition(WaterTempLabelKey, null, DevicePropertyType.String));
            this.returnAirTempLabel =
                this.CreateProperty<string>(new PropertyDefinition(ReturnAirTempLabelKey, null,
                    DevicePropertyType.String));
            this.totalPowerUsageLabel =
                this.CreateProperty<string>(new PropertyDefinition(TotalPowerUsageLabelKey, null,
                    DevicePropertyType.String));
            this.compressorPowerUsageLabel =
                this.CreateProperty<string>(new PropertyDefinition(CompressorPowerUsageLabelKey, null,
                    DevicePropertyType.String));
            this.fanPowerUsageLabel =
                this.CreateProperty<string>(new PropertyDefinition(FanPowerUsageLabelKey, null,
                    DevicePropertyType.String));
            this.pumpPowerUsageLabel =
                this.CreateProperty<string>(new PropertyDefinition(PumpPowerUsageLabelKey, null,
                    DevicePropertyType.String));
            this.auxHeatPowerUsageLabel =
                this.CreateProperty<string>(new PropertyDefinition(AuxHeatPowerUsageLabelKey, null,
                    DevicePropertyType.String));
        }

        #region UI Property Keys

        private const string ConnectionStateIconKey = "ConnectionStateIcon";
        private const string ThermostatHeatModeEnabledKey = "ThermostatHeatModeEnabled";
        private const string ThermostatCoolModeEnabledKey = "ThermostatCoolModeEnabled";
        private const string ThermostatAutoModeEnabledKey = "ThermostatAutoModeEnabled";
        private const string ThermostatUnitsKey = "ThermostatUnits";
        private const string ThermostatModeIconKey = "ThermostatModeIcon";
        private const string ThermostatCoolSetPointKey = "ThermostatCoolSetPoint";
        private const string ThermostatHeatSetPointKey = "ThermostatHeatSetPoint";
        private const string ThermostatAutoSetPointKey = "ThermostatAutoSetPoint";
        private const string ThermostatCurrentTemperatureKey = "ThermostatCurrentTemperature";
        private const string ThermostatCurrentTempLabelKey = "ThermostatCurrentTempLabel";
        private const string WaterTempLabelKey = "WaterTempLabel";
        private const string ReturnAirTempLabelKey = "ReturnAirTempLabel";
        private const string TotalPowerUsageLabelKey = "TotalPowerUsageLabel";
        private const string CompressorPowerUsageLabelKey = "CompressorPowerUsageLabel";
        private const string FanPowerUsageLabelKey = "FanPowerUsageLabel";
        private const string PumpPowerUsageLabelKey = "PumpPowerUsageLabel";
        private const string AuxHeatPowerUsageLabelKey = "AuxHeatPowerUsageLabel";

        #endregion

        #region UI Properties

        private PropertyValue<string> connectionStateIcon;
        private PropertyValue<bool> thermostatHeatModeEnabled;
        private PropertyValue<bool> thermostatCoolModeEnabled;
        private PropertyValue<bool> thermostatAutoModeEnabled;
        private PropertyValue<string> thermostatUnits;
        private PropertyValue<string> thermostatModeIcon;
        private PropertyValue<int> thermostatCoolSetPoint;
        private PropertyValue<int> thermostatHeatSetPoint;
        private PropertyValue<int> thermostatAutoSetPoint;
        private PropertyValue<double> thermostatCurrentTemperature;
        private PropertyValue<string> thermostatCurrentTempLabel;
        private PropertyValue<string> waterTempLabel;
        private PropertyValue<string> totalPowerUsageLabel;
        private PropertyValue<string> compressorPowerUsageLabel;
        private PropertyValue<string> fanPowerUsageLabel;
        private PropertyValue<string> pumpPowerUsageLabel;
        private PropertyValue<string> auxHeatPowerUsageLabel;
        private PropertyValue<string> returnAirTempLabel;

        #endregion


        #region AExtensionDevice members

        protected override IOperationResult DoCommand(string command, string[] parameters)
        {
            this.Commit();
            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string propertyKey, T value)
        {
            switch (propertyKey)
            {
                case ThermostatCoolSetPointKey:
                {
                    if (value is int intValue)
                    {
                        this.thermostatCoolSetPoint.Value = intValue;
                        this.protocol.SetCoolingSetPoint(this, intValue);
                    }

                    break;
                }
                case ThermostatHeatSetPointKey:
                {
                    if (value is int intValue)
                    {
                        this.thermostatHeatSetPoint.Value = intValue;
                        this.protocol.SetHeatingSetPoint(this, intValue);
                    }

                    break;
                }
            }

            this.Commit();
            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string objectId, string propertyKey, T value)
        {
            this.Commit();
            return new OperationResult(OperationResultCode.Success);
        }

        #endregion AExtensionDevice members

        #region Gateway device info

        public string DeviceId { get; }
        public string AWLId { get; }

        public string ThermostatName { get; private set; }

        public string Model { get; }

        public string DeviceType => "HVAC";

        public string DeviceSubType => string.Empty;

        #endregion
    }
}