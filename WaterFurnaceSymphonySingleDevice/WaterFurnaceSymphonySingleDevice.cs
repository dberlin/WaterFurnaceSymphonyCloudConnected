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
        }

        public void SetThermostatName(string name)
        {
            this.ThermostatName = name;
        }

        public void RefreshDeviceWithData(ReadResponse data)
        {
            this.waterTempLabel.Value = $"Entering water temperature: {data.EnteringWaterTemp}°F";
            this.totalPowerUsageLine1Label.Value = $"{data.TotalUnitPower}W";
            this.compressorPowerUsageLine1Label.Value = $"{data.CompressorPower}W";
            this.fanPowerUsageLine1Label.Value = $"{data.FanPower}W";
            this.pumpPowerUsageLine1Label.Value = $"{data.LoopPumpPower}W";
            this.auxHeatPowerUsageLine1Label.Value = $"{data.AuxPower}W";
            this.thermostatCurrentTemperature.Value = data.ThermostatRoomTemp;
            this.thermostatSetPointCool.Value = (int) data.ActiveSettings.CoolingSetPoint;
            this.thermostatSetPointHeat.Value = (int) data.ActiveSettings.HeatingSetPoint;
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
            this.thermostatSetPointCool = this.CreateProperty<int>(new PropertyDefinition(ThermostatSetPointCoolKey,
                null, DevicePropertyType.Int32, 50, 99, 1));
            this.thermostatSetPointHeat = this.CreateProperty<int>(new PropertyDefinition(ThermostatSetPointHeatKey,
                null, DevicePropertyType.Int32, 50, 99, 1));
            this.thermostatCurrentTemperature = this.CreateProperty<double>(new PropertyDefinition(
                ThermostatCurrentTemperatureKey, null, DevicePropertyType.Double,
                0, 150, 0.1));
            this.thermostatCurrentTempLabel = this.CreateProperty<string>(new PropertyDefinition(
                ThermostatCurrentTempLabelKey, null,
                DevicePropertyType.String));
            this.waterTempLabel =
                this.CreateProperty<string>(new PropertyDefinition(WaterTempLabelKey, null, DevicePropertyType.String));

            this.totalPowerUsageLine1Label =
                this.CreateProperty<string>(new PropertyDefinition(TotalPowerUsageLine1LabelKey, null,
                    DevicePropertyType.String));
            this.compressorPowerUsageLine1Label =
                this.CreateProperty<string>(new PropertyDefinition(CompressorPowerUsageLine1LabelKey, null,
                    DevicePropertyType.String));
            this.fanPowerUsageLine1Label =
                this.CreateProperty<string>(new PropertyDefinition(FanPowerUsageLine1LabelKey, null,
                    DevicePropertyType.String));
            this.pumpPowerUsageLine1Label =
                this.CreateProperty<string>(new PropertyDefinition(PumpPowerUsageLine1LabelKey, null,
                    DevicePropertyType.String));
            this.auxHeatPowerUsageLine1Label =
                this.CreateProperty<string>(new PropertyDefinition(AuxHeatPowerUsageLine1LabelKey, null,
                    DevicePropertyType.String));
        }

        #region UI Property Keys

        private const string ConnectionStateIconKey = "ConnectionStateIcon";
        private const string ThermostatUnitsKey = "ThermostatUnits";
        private const string ThermostatModeIconKey = "ThermostatModeIcon";
        private const string ThermostatSetPointCoolKey = "ThermostatSetPointCool";
        private const string ThermostatSetPointHeatKey = "ThermostatSetPointHeat";
        private const string ThermostatCurrentTemperatureKey = "ThermostatCurrentTemperature";
        private const string ThermostatCurrentTempLabelKey = "ThermostatCurrentTempLabel";
        private const string WaterTempLabelKey = "WaterTempLabel";
        private const string TotalPowerUsageLine1LabelKey = "TotalPowerUsageLine1Label";
        private const string CompressorPowerUsageLine1LabelKey = "CompressorPowerUsageLine1Label";
        private const string FanPowerUsageLine1LabelKey = "FanPowerUsageLine1Label";
        private const string PumpPowerUsageLine1LabelKey = "PumpPowerUsageLine1Label";
        private const string AuxHeatPowerUsageLine1LabelKey = "AuxHeatPowerUsageLine1Label";

        #endregion

        #region UI Properties

        private PropertyValue<string> connectionStateIcon;
        private PropertyValue<string> thermostatUnits;
        private PropertyValue<string> thermostatModeIcon;
        private PropertyValue<int> thermostatSetPointCool;
        private PropertyValue<int> thermostatSetPointHeat;
        private PropertyValue<double> thermostatCurrentTemperature;
        private PropertyValue<string> thermostatCurrentTempLabel;
        private PropertyValue<string> waterTempLabel;
        private PropertyValue<string> totalPowerUsageLine1Label;
        private PropertyValue<string> compressorPowerUsageLine1Label;
        private PropertyValue<string> fanPowerUsageLine1Label;
        private PropertyValue<string> pumpPowerUsageLine1Label;
        private PropertyValue<string> auxHeatPowerUsageLine1Label;

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
                case ThermostatSetPointCoolKey:
                {
                    if (value is double doubleValue)
                    {
                        this.thermostatSetPointCool.Value = (int) doubleValue;
                        this.protocol.SetCoolingSetPoint(this, doubleValue);
                    }

                    break;
                }
                case ThermostatSetPointHeatKey:
                {
                    if (value is double doubleValue)
                    {
                        this.thermostatSetPointHeat.Value = (int) doubleValue;
                        this.protocol.SetHeatingSetPoint(this, doubleValue);
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