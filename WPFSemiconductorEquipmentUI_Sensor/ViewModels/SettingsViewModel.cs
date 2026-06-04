namespace WPFSemiconductorEquipmentUI_Sensor.ViewModels
{
    public class SettingsViewModel : ScreenViewModelBase
    {
        private double _pressureWarningThreshold;
        private double _vibrationWarningThreshold;
        private double _temperatureWarningThreshold;
        private double _humidityWarningThreshold;

        public SettingsViewModel()
        {
            Title = "System Settings";
            Description = "Manage local console connection, access, risk, and log retention settings.";

            PressureWarningThreshold = 0.80d;
            VibrationWarningThreshold = 8.0d;
            TemperatureWarningThreshold = 40.0d;
            HumidityWarningThreshold = 70.0d;
        }

        public double PressureWarningThreshold
        {
            get { return _pressureWarningThreshold; }
            set
            {
                _pressureWarningThreshold = value;
                OnPropertyChanged();
            }
        }

        public double VibrationWarningThreshold
        {
            get { return _vibrationWarningThreshold; }
            set
            {
                _vibrationWarningThreshold = value;
                OnPropertyChanged();
            }
        }

        public double TemperatureWarningThreshold
        {
            get { return _temperatureWarningThreshold; }
            set
            {
                _temperatureWarningThreshold = value;
                OnPropertyChanged();
            }
        }

        public double HumidityWarningThreshold
        {
            get { return _humidityWarningThreshold; }
            set
            {
                _humidityWarningThreshold = value;
                OnPropertyChanged();
            }
        }
    }
}
