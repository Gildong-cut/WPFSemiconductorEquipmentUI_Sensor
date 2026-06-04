using System.Collections.ObjectModel;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFSemiconductorEquipmentUI_Sensor.Models;
using WPFSemiconductorEquipmentUI_Sensor.Services;

namespace WPFSemiconductorEquipmentUI_Sensor.ViewModels
{
    public class ConsoleViewModel : ScreenViewModelBase, IDisposable
    {
        private const int StaleReadThreshold = 6;
        private readonly ITrainerClient _trainerClient;
        private readonly SettingsViewModel _settings;
        private readonly DispatcherTimer _pollingTimer;
        private bool _disposed;
        private bool _isReading;
        private bool _runningLampIsOn;
        private bool _hasWarningOutputState;
        private bool _warningOutputIsOn;
        private bool _riskOutputIsOn;
        private int _successfulReadCount;
        private int _unchangedReadCount;
        private bool _hasLastRawSnapshot;
        private bool _hasLastLoggedSensorSnapshot;
        private DateTime _lastSensorLogAt = DateTime.MinValue;
        private short _lastPressureRaw;
        private short _lastVibrationRaw;
        private short _lastTemperatureRaw;
        private short _lastHumidityRaw;
        private short _lastLoggedPressureRaw;
        private short _lastLoggedVibrationRaw;
        private short _lastLoggedTemperatureRaw;
        private short _lastLoggedHumidityRaw;
        private string _connectionStatusText;
        private string _connectionStatusTone;
        private string _lastUpdateText;
        private string _etherCatStatusText;
        private string _etherCatStatusTone;
        private string _summaryBadgeText;
        private string _summaryTone;
        private string _summaryText;
        private string _digitalInput1Text;
        private string _digitalInput1Tone;
        private string _digitalInput2Text;
        private string _digitalInput2Tone;
        private string _digitalInput3Text;
        private string _digitalInput3Tone;
        private string _digitalInput4Text;
        private string _digitalInput4Tone;
        private string _opticalSensorText;
        private string _opticalSensorTone;
        private string _inductiveSensorText;
        private string _inductiveSensorTone;
        private bool _isControlEnabled;
        private bool _isAdmin;
        private string _userStateText;
        private string _userStateTone;
        private string _currentUserText;
        private string _controlAccessText;
        private string _controlAccessTone;
        private string _equipmentControlStateText;
        private string _equipmentControlStateTone;
        private bool _isEmergencyVisible;

        public ConsoleViewModel()
            : this(new AdsSensorTrainerClient(), new SettingsViewModel())
        {
        }

        public ConsoleViewModel(ITrainerClient trainerClient, SettingsViewModel settings)
        {
            _trainerClient = trainerClient;
            _settings = settings;

            Title = "WPF Equipment Control Console";
            Description = "Desktop interface for field control, sensor review, and activity monitoring.";

            Sensors = new ObservableCollection<SensorMetric>
            {
                new SensorMetric { Name = "Pressure", Value = "--", Unit = "bar", RangeText = "Raw: --", BadgeText = "WAIT", Tone = "Disabled", IndicatorWidth = 0 },
                new SensorMetric { Name = "Vibration", Value = "--", Unit = "level", RangeText = "Raw: --", BadgeText = "WAIT", Tone = "Disabled", IndicatorWidth = 0 },
                new SensorMetric { Name = "Temperature", Value = "--", Unit = "C", RangeText = "Raw: --", BadgeText = "WAIT", Tone = "Disabled", IndicatorWidth = 0 },
                new SensorMetric { Name = "Humidity", Value = "--", Unit = "%", RangeText = "Raw: --", BadgeText = "WAIT", Tone = "Disabled", IndicatorWidth = 0 }
            };

            ActivityLogs = ActivityLogStore.Instance.SensorLogs;

            ConnectionStatusText = "DISCONNECTED";
            ConnectionStatusTone = "Disabled";
            LastUpdateText = "LAST UPDATE --:--:--";
            EtherCatStatusText = "DISCONNECTED";
            EtherCatStatusTone = "Disabled";
            SummaryBadgeText = "WAITING";
            SummaryTone = "Disabled";
            SummaryText = "Waiting for TwinCAT ADS connection. Last known sensor values will remain visible if reads fail.";
            SetDigitalInputs(false, false, false, false, false, false);
            UserStateText = "NOT LOGGED IN";
            UserStateTone = "Disabled";
            CurrentUserText = "Guest";
            ControlAccessText = "LOCKED";
            ControlAccessTone = "Disabled";
            EquipmentControlStateText = "LOCKED";
            EquipmentControlStateTone = "Disabled";
            IsEmergencyVisible = false;

            StartCommand = new RelayCommand(StartEquipment, parameter => IsControlEnabled);
            StopCommand = new RelayCommand(StopEquipment, parameter => IsControlEnabled);

            _pollingTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _pollingTimer.Tick += OnPollingTimerTick;
            _pollingTimer.Start();
        }

        public ObservableCollection<SensorMetric> Sensors { get; private set; }
        public ObservableCollection<ActivityLogItem> ActivityLogs { get; private set; }
        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public bool IsControlEnabled
        {
            get { return _isControlEnabled; }
            private set
            {
                if (_isControlEnabled == value)
                {
                    return;
                }

                _isControlEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged("StartButtonText");
                OnPropertyChanged("StopButtonText");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StartButtonText
        {
            get { return IsControlEnabled ? "START" : "LOCKED"; }
        }

        public string StopButtonText
        {
            get { return IsControlEnabled ? "STOP" : "LOCKED"; }
        }

        public string UserStateText
        {
            get { return _userStateText; }
            private set { SetProperty(ref _userStateText, value); }
        }

        public string UserStateTone
        {
            get { return _userStateTone; }
            private set { SetProperty(ref _userStateTone, value); }
        }

        public string CurrentUserText
        {
            get { return _currentUserText; }
            private set { SetProperty(ref _currentUserText, value); }
        }

        public string ControlAccessText
        {
            get { return _controlAccessText; }
            private set { SetProperty(ref _controlAccessText, value); }
        }

        public string ControlAccessTone
        {
            get { return _controlAccessTone; }
            private set { SetProperty(ref _controlAccessTone, value); }
        }

        public string EquipmentControlStateText
        {
            get { return _equipmentControlStateText; }
            private set { SetProperty(ref _equipmentControlStateText, value); }
        }

        public string EquipmentControlStateTone
        {
            get { return _equipmentControlStateTone; }
            private set { SetProperty(ref _equipmentControlStateTone, value); }
        }

        public bool IsEmergencyVisible
        {
            get { return _isEmergencyVisible; }
            private set { SetProperty(ref _isEmergencyVisible, value); }
        }

        public bool IsAdmin
        {
            get { return _isAdmin; }
        }

        public string ConnectionStatusText
        {
            get { return _connectionStatusText; }
            private set { SetProperty(ref _connectionStatusText, value); }
        }

        public string ConnectionStatusTone
        {
            get { return _connectionStatusTone; }
            private set { SetProperty(ref _connectionStatusTone, value); }
        }

        public string LastUpdateText
        {
            get { return _lastUpdateText; }
            private set { SetProperty(ref _lastUpdateText, value); }
        }

        public string EtherCatStatusText
        {
            get { return _etherCatStatusText; }
            private set { SetProperty(ref _etherCatStatusText, value); }
        }

        public string EtherCatStatusTone
        {
            get { return _etherCatStatusTone; }
            private set { SetProperty(ref _etherCatStatusTone, value); }
        }

        public string SummaryBadgeText
        {
            get { return _summaryBadgeText; }
            private set { SetProperty(ref _summaryBadgeText, value); }
        }

        public string SummaryTone
        {
            get { return _summaryTone; }
            private set { SetProperty(ref _summaryTone, value); }
        }

        public string SummaryText
        {
            get { return _summaryText; }
            private set { SetProperty(ref _summaryText, value); }
        }

        public string DigitalInput1Text
        {
            get { return _digitalInput1Text; }
            private set { SetProperty(ref _digitalInput1Text, value); }
        }

        public string DigitalInput1Tone
        {
            get { return _digitalInput1Tone; }
            private set { SetProperty(ref _digitalInput1Tone, value); }
        }

        public string DigitalInput2Text
        {
            get { return _digitalInput2Text; }
            private set { SetProperty(ref _digitalInput2Text, value); }
        }

        public string DigitalInput2Tone
        {
            get { return _digitalInput2Tone; }
            private set { SetProperty(ref _digitalInput2Tone, value); }
        }

        public string DigitalInput3Text
        {
            get { return _digitalInput3Text; }
            private set { SetProperty(ref _digitalInput3Text, value); }
        }

        public string DigitalInput3Tone
        {
            get { return _digitalInput3Tone; }
            private set { SetProperty(ref _digitalInput3Tone, value); }
        }

        public string DigitalInput4Text
        {
            get { return _digitalInput4Text; }
            private set { SetProperty(ref _digitalInput4Text, value); }
        }

        public string DigitalInput4Tone
        {
            get { return _digitalInput4Tone; }
            private set { SetProperty(ref _digitalInput4Tone, value); }
        }

        public string OpticalSensorText
        {
            get { return _opticalSensorText; }
            private set { SetProperty(ref _opticalSensorText, value); }
        }

        public string OpticalSensorTone
        {
            get { return _opticalSensorTone; }
            private set { SetProperty(ref _opticalSensorTone, value); }
        }

        public string InductiveSensorText
        {
            get { return _inductiveSensorText; }
            private set { SetProperty(ref _inductiveSensorText, value); }
        }

        public string InductiveSensorTone
        {
            get { return _inductiveSensorTone; }
            private set { SetProperty(ref _inductiveSensorTone, value); }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pollingTimer.Stop();
            _pollingTimer.Tick -= OnPollingTimerTick;
            _trainerClient.Dispose();
        }

        private void TrySetRunningLampOff()
        {
            if (!_runningLampIsOn)
            {
                return;
            }

            try
            {
                _trainerClient.SetRunningLamp(false);
            }
            catch
            {
            }
            finally
            {
                _runningLampIsOn = false;
            }
        }

        private void TrySetRunningLampOn()
        {
            if (_runningLampIsOn)
            {
                return;
            }

            _trainerClient.SetRunningLamp(true);
            _runningLampIsOn = true;
        }

        public void SetUserAccess(UserAccount account)
        {
            CurrentUserText = account.UserId;
            _isAdmin = string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            OnPropertyChanged("IsAdmin");
            var isApproved = _isAdmin || string.Equals(account.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase);

            IsControlEnabled = isApproved;
            UserStateText = _isAdmin ? "ADMIN" : isApproved ? "APPROVED" : "PENDING";
            UserStateTone = isApproved ? "Normal" : "Warning";
            ControlAccessText = isApproved ? "ENABLED" : "LOCKED";
            ControlAccessTone = isApproved ? "Normal" : "Disabled";
            IsEmergencyVisible = _isAdmin;

            if (isApproved)
            {
                if (_isAdmin)
                {
                    return;
                }

                SetOperatorDigitalInputsLocked();
                return;
            }

            SetDigitalInputsLocked();
            EquipmentControlStateText = "LOCKED";
            EquipmentControlStateTone = "Disabled";
        }

        private void OnPollingTimerTick(object sender, EventArgs e)
        {
            if (_disposed || _isReading)
            {
                return;
            }

            _isReading = true;
            try
            {
                var snapshot = _trainerClient.ReadSnapshot();

                if (IsStaleSnapshot(snapshot))
                {
                    TrySetRunningLampOff();
                    TrySetWarningOutputs(false, false);
                    ApplyStaleSnapshot(snapshot);
                }
                else
                {
                    TrySetRunningLampOn();
                    ApplySnapshot(snapshot);
                    ApplyWarningState(snapshot);
                }
            }
            catch (Exception ex)
            {
                TrySetRunningLampOff();
                TrySetWarningOutputs(false, false);
                ApplyReadFailure(ex);
            }
            finally
            {
                _isReading = false;
            }
        }

        private void TryDisableAllDigitalOutputs()
        {
            try
            {
                _trainerClient.DisableAllDigitalOutputs();
            }
            catch
            {
            }
            finally
            {
                _runningLampIsOn = false;
            }
        }

        private void ApplyWarningState(SensorTrainerSnapshot snapshot)
        {
            var pressureWarning = CalibratePressure(snapshot.Pressure) > _settings.PressureWarningThreshold;
            var vibrationWarning = CalibrateVibration(snapshot.Vibration) > _settings.VibrationWarningThreshold;
            var temperatureWarning = CalibrateTemperature(snapshot.Temperature) > _settings.TemperatureWarningThreshold;
            var humidityWarning = CalibrateHumidity(snapshot.Humidity) > _settings.HumidityWarningThreshold;

            SetSensorWarning(Sensors[0], pressureWarning);
            SetSensorWarning(Sensors[1], vibrationWarning);
            SetSensorWarning(Sensors[2], temperatureWarning);
            SetSensorWarning(Sensors[3], humidityWarning);

            var warningCount = (pressureWarning ? 1 : 0)
                + (vibrationWarning ? 1 : 0)
                + (temperatureWarning ? 1 : 0)
                + (humidityWarning ? 1 : 0);

            TrySetWarningOutputs(warningCount >= 1, warningCount >= 2);
        }

        private void TrySetWarningOutputs(bool warningOn, bool riskOn)
        {
            if (_hasWarningOutputState
                && _warningOutputIsOn == warningOn
                && _riskOutputIsOn == riskOn)
            {
                return;
            }

            try
            {
                _trainerClient.SetWarningOutputs(warningOn, riskOn);
                _warningOutputIsOn = warningOn;
                _riskOutputIsOn = riskOn;
                _hasWarningOutputState = true;
            }
            catch
            {
            }
        }

        private static void SetSensorWarning(SensorMetric sensor, bool isWarning)
        {
            sensor.BadgeText = isWarning ? "WARNING" : "LIVE";
            sensor.Tone = isWarning ? "Warning" : "Normal";
        }

        private void TryDisableOperatorRestrictedOutputs()
        {
            try
            {
                _trainerClient.DisableOperatorRestrictedOutputs();
            }
            catch
            {
            }
        }

        private void ClearEquipmentData()
        {
            foreach (var sensor in Sensors)
            {
                sensor.Value = "--";
                sensor.RangeText = "Access locked";
                sensor.BadgeText = "LOCKED";
                sensor.Tone = "Disabled";
                sensor.IndicatorWidth = 0d;
            }

            SetDigitalInputsLocked();
            ResetStaleDetection();
            ConnectionStatusText = "ACCESS LOCKED";
            ConnectionStatusTone = "Disabled";
            LastUpdateText = "NO SENSOR ACCESS";
            EtherCatStatusText = "LOCKED";
            EtherCatStatusTone = "Disabled";
            SummaryBadgeText = "LOCKED";
            SummaryTone = "Disabled";
            SummaryText = "This account is not approved. Sensor reads, digital input monitoring, and equipment controls are disabled.";
        }

        private void ResetStaleDetection()
        {
            _unchangedReadCount = 0;
            _hasLastRawSnapshot = false;
        }

        private void SetDigitalInputsLocked()
        {
            SetDigitalStatusLocked(out _digitalInput1Text, out _digitalInput1Tone, "DigitalInput1Text", "DigitalInput1Tone");
            SetDigitalStatusLocked(out _digitalInput2Text, out _digitalInput2Tone, "DigitalInput2Text", "DigitalInput2Tone");
            SetDigitalStatusLocked(out _digitalInput3Text, out _digitalInput3Tone, "DigitalInput3Text", "DigitalInput3Tone");
            SetDigitalStatusLocked(out _digitalInput4Text, out _digitalInput4Tone, "DigitalInput4Text", "DigitalInput4Tone");
            SetDigitalStatusLocked(out _opticalSensorText, out _opticalSensorTone, "OpticalSensorText", "OpticalSensorTone");
            SetDigitalStatusLocked(out _inductiveSensorText, out _inductiveSensorTone, "InductiveSensorText", "InductiveSensorTone");
        }

        private void SetDigitalStatusLocked(out string textField, out string toneField, string textPropertyName, string tonePropertyName)
        {
            textField = "LOCKED";
            toneField = "Disabled";
            OnPropertyChanged(textPropertyName);
            OnPropertyChanged(tonePropertyName);
        }

        private void StartEquipment(object parameter)
        {
            if (!IsControlEnabled)
            {
                return;
            }

            EquipmentControlStateText = "STARTED";
            EquipmentControlStateTone = "Normal";
            AddControlLog("START command");
        }

        private void StopEquipment(object parameter)
        {
            if (!IsControlEnabled)
            {
                return;
            }

            EquipmentControlStateText = "STOPPED";
            EquipmentControlStateTone = "Warning";
            AddControlLog("STOP command");
        }

        private void AddControlLog(string eventText)
        {
            ActivityLogStore.Instance.Add("Control", CurrentUserText, eventText, "INFO");
        }

        private bool IsStaleSnapshot(SensorTrainerSnapshot snapshot)
        {
            if (!_hasLastRawSnapshot)
            {
                SaveLastRawSnapshot(snapshot);
                return false;
            }

            if (_lastPressureRaw == snapshot.Pressure
                && _lastVibrationRaw == snapshot.Vibration
                && _lastTemperatureRaw == snapshot.Temperature
                && _lastHumidityRaw == snapshot.Humidity)
            {
                _unchangedReadCount++;
            }
            else
            {
                _unchangedReadCount = 0;
                SaveLastRawSnapshot(snapshot);
            }

            return _unchangedReadCount >= StaleReadThreshold;
        }

        private void SaveLastRawSnapshot(SensorTrainerSnapshot snapshot)
        {
            _lastPressureRaw = snapshot.Pressure;
            _lastVibrationRaw = snapshot.Vibration;
            _lastTemperatureRaw = snapshot.Temperature;
            _lastHumidityRaw = snapshot.Humidity;
            _hasLastRawSnapshot = true;
        }

        private void ApplySnapshot(SensorTrainerSnapshot snapshot)
        {
            if (_disposed)
            {
                return;
            }

            _successfulReadCount++;

            UpdateSensor(Sensors[0], snapshot.Pressure, CalibratePressure, "0.00", 0d, 1d);
            UpdateSensor(Sensors[1], snapshot.Vibration, CalibrateVibration, "0.0", 0d, 10d);
            UpdateSensor(Sensors[2], snapshot.Temperature, CalibrateTemperature, "0.0", 0d, 60d);
            UpdateSensor(Sensors[3], snapshot.Humidity, CalibrateHumidity, "0.0", 0d, 100d);

            ApplyDigitalInputAccess(snapshot);
            LogSensorChange(snapshot);

            ConnectionStatusText = "ADS READ OK";
            ConnectionStatusTone = "Normal";
            LastUpdateText = "READ #" + _successfulReadCount + " " + snapshot.ReceivedAt.ToString("HH:mm:ss");
            EtherCatStatusText = "READY";
            EtherCatStatusTone = "Normal";
            SummaryBadgeText = "PLC LINK";
            SummaryTone = "Normal";
            SummaryText = "TwinCAT ADS is readable. Sensor power state is not verified; displayed values are the latest PLC raw inputs from GVL.NX_AD4203.";
        }

        private void LogSensorChange(SensorTrainerSnapshot snapshot)
        {
            var changed = !_hasLastLoggedSensorSnapshot
                || _lastLoggedPressureRaw != snapshot.Pressure
                || _lastLoggedVibrationRaw != snapshot.Vibration
                || _lastLoggedTemperatureRaw != snapshot.Temperature
                || _lastLoggedHumidityRaw != snapshot.Humidity;

            if (!changed || DateTime.Now - _lastSensorLogAt < TimeSpan.FromSeconds(1))
            {
                return;
            }

            _lastLoggedPressureRaw = snapshot.Pressure;
            _lastLoggedVibrationRaw = snapshot.Vibration;
            _lastLoggedTemperatureRaw = snapshot.Temperature;
            _lastLoggedHumidityRaw = snapshot.Humidity;
            _hasLastLoggedSensorSnapshot = true;
            _lastSensorLogAt = DateTime.Now;

            var eventText = string.Format(
                "P {0:0.00} bar ({1}), V {2:0.0} level ({3}), T {4:0.0} C ({5}), H {6:0.0}% ({7})",
                CalibratePressure(snapshot.Pressure),
                snapshot.Pressure,
                CalibrateVibration(snapshot.Vibration),
                snapshot.Vibration,
                CalibrateTemperature(snapshot.Temperature),
                snapshot.Temperature,
                CalibrateHumidity(snapshot.Humidity),
                snapshot.Humidity);

            ActivityLogStore.Instance.Add("Sensor", "system", eventText, "INFO");
        }

        private void ApplyStaleSnapshot(SensorTrainerSnapshot snapshot)
        {
            if (_disposed)
            {
                return;
            }

            _successfulReadCount++;

            UpdateSensor(Sensors[0], snapshot.Pressure, CalibratePressure, "0.00", 0d, 1d);
            UpdateSensor(Sensors[1], snapshot.Vibration, CalibrateVibration, "0.0", 0d, 10d);
            UpdateSensor(Sensors[2], snapshot.Temperature, CalibrateTemperature, "0.0", 0d, 60d);
            UpdateSensor(Sensors[3], snapshot.Humidity, CalibrateHumidity, "0.0", 0d, 100d);
            SetSensorStale(Sensors[0]);
            SetSensorStale(Sensors[1]);
            SetSensorStale(Sensors[2]);
            SetSensorStale(Sensors[3]);

            ApplyDigitalInputAccess(snapshot);

            ConnectionStatusText = "STALE";
            ConnectionStatusTone = "Warning";
            LastUpdateText = "READ #" + _successfulReadCount + " " + snapshot.ReceivedAt.ToString("HH:mm:ss");
            EtherCatStatusText = "STALE";
            EtherCatStatusTone = "Warning";
            SummaryBadgeText = "STALE";
            SummaryTone = "Warning";
            SummaryText = "PLC raw sensor values have not changed for about 3 seconds. ADS is readable, but sensor input updates appear stopped.";
        }

        private void ApplyReadFailure(Exception ex)
        {
            if (_disposed)
            {
                return;
            }

            ConnectionStatusText = "READ ERROR";
            ConnectionStatusTone = "Danger";
            EtherCatStatusText = "DISCONNECTED";
            EtherCatStatusTone = "Danger";
            SummaryBadgeText = "READ ERROR";
            SummaryTone = "Danger";
            SummaryText = "Unable to read TwinCAT ADS data. Check TwinCAT runtime, ADS route, port 851, and GVL.NX_* variables. " + ex.Message;
        }

        private void SetSensorStale(SensorMetric sensor)
        {
            sensor.BadgeText = "STALE";
            sensor.Tone = "Warning";
        }

        private void UpdateSensor(
            SensorMetric sensor,
            short rawValue,
            Func<short, double> calibration,
            string format,
            double minimum,
            double maximum)
        {
            var calibratedValue = calibration(rawValue);

            sensor.Value = calibratedValue.ToString(format);
            sensor.RangeText = "Raw: " + rawValue;
            sensor.BadgeText = "LIVE";
            sensor.Tone = "Normal";
            sensor.IndicatorWidth = CalculateIndicatorWidth(calibratedValue, minimum, maximum);
        }

        private static double CalculateIndicatorWidth(double value, double minimum, double maximum)
        {
            if (maximum <= minimum)
            {
                return 8d;
            }

            var normalized = (value - minimum) / (maximum - minimum);
            normalized = Math.Max(0d, Math.Min(1d, normalized));
            return Math.Max(8d, normalized * 220d);
        }

        private static double CalibratePressure(short rawValue)
        {
            return (rawValue * 0.00016129032258064516d) - 0.1532258064516129d;
        }

        private static double CalibrateVibration(short rawValue)
        {
            return Clamp((rawValue * 0.001282051282051282d) + 0.0512820512820511d, 0d, 10d);
        }

        private static double CalibrateTemperature(short rawValue)
        {
            return (rawValue * 0.014035087719298246d) - 39.55789473684211d;
        }

        private static double CalibrateHumidity(short rawValue)
        {
            return (rawValue * 0.010344827586206896d) + 5.793103448275861d;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private void SetDigitalInputs(
            bool digitalInput1,
            bool digitalInput2,
            bool digitalInput3,
            bool digitalInput4,
            bool opticalSensor,
            bool inductiveSensor)
        {
            SetDigitalStatus(digitalInput1, out _digitalInput1Text, out _digitalInput1Tone, "DigitalInput1Text", "DigitalInput1Tone");
            SetDigitalStatus(digitalInput2, out _digitalInput2Text, out _digitalInput2Tone, "DigitalInput2Text", "DigitalInput2Tone");
            SetDigitalStatus(digitalInput3, out _digitalInput3Text, out _digitalInput3Tone, "DigitalInput3Text", "DigitalInput3Tone");
            SetDigitalStatus(digitalInput4, out _digitalInput4Text, out _digitalInput4Tone, "DigitalInput4Text", "DigitalInput4Tone");
            SetDigitalStatus(opticalSensor, out _opticalSensorText, out _opticalSensorTone, "OpticalSensorText", "OpticalSensorTone");
            SetDigitalStatus(inductiveSensor, out _inductiveSensorText, out _inductiveSensorTone, "InductiveSensorText", "InductiveSensorTone");
        }

        private void ApplyDigitalInputAccess(SensorTrainerSnapshot snapshot)
        {
            if (!IsControlEnabled)
            {
                SetDigitalInputsLocked();
                return;
            }

            if (!_isAdmin)
            {
                SetDigitalStatus(snapshot.DigitalInput1, out _digitalInput1Text, out _digitalInput1Tone, "DigitalInput1Text", "DigitalInput1Tone");
                SetDigitalStatus(snapshot.DigitalInput2, out _digitalInput2Text, out _digitalInput2Tone, "DigitalInput2Text", "DigitalInput2Tone");
                SetOperatorDigitalInputsLocked();
                SetDigitalStatus(snapshot.OpticalSensor, out _opticalSensorText, out _opticalSensorTone, "OpticalSensorText", "OpticalSensorTone");
                SetDigitalStatus(snapshot.InductiveSensor, out _inductiveSensorText, out _inductiveSensorTone, "InductiveSensorText", "InductiveSensorTone");
                return;
            }

            SetDigitalInputs(
                snapshot.DigitalInput1,
                snapshot.DigitalInput2,
                snapshot.DigitalInput3,
                snapshot.DigitalInput4,
                snapshot.OpticalSensor,
                snapshot.InductiveSensor);
        }

        private void SetOperatorDigitalInputsLocked()
        {
            SetDigitalStatusLocked(out _digitalInput3Text, out _digitalInput3Tone, "DigitalInput3Text", "DigitalInput3Tone");
            SetDigitalStatusLocked(out _digitalInput4Text, out _digitalInput4Tone, "DigitalInput4Text", "DigitalInput4Tone");
        }

        private void SetDigitalStatus(bool isOn, out string textField, out string toneField, string textPropertyName, string tonePropertyName)
        {
            textField = isOn ? "ON" : "OFF";
            toneField = isOn ? "Normal" : "Disabled";
            OnPropertyChanged(textPropertyName);
            OnPropertyChanged(tonePropertyName);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}
