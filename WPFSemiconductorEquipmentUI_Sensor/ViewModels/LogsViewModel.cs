using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System;
using WPFSemiconductorEquipmentUI_Sensor.Models;
using WPFSemiconductorEquipmentUI_Sensor.Services;

namespace WPFSemiconductorEquipmentUI_Sensor.ViewModels
{
    public class LogsViewModel : ScreenViewModelBase
    {
        private ActivityLogItem _selectedLog;
        private bool _isAdmin;

        public LogsViewModel()
        {
            Title = "Activity & Sensor Logs";
            Description = "Approved users can review sensor history; administrators can also review user actions.";

            SensorLogs = ActivityLogStore.Instance.SensorLogs;
            UserActionLogs = ActivityLogStore.Instance.UserActionLogs;
            SensorLogs.CollectionChanged += OnLogsChanged;
            UserActionLogs.CollectionChanged += OnLogsChanged;
            if (SensorLogs.Count > 0)
            {
                SelectedLog = SensorLogs[0];
            }
        }

        public ObservableCollection<ActivityLogItem> SensorLogs { get; private set; }
        public ObservableCollection<ActivityLogItem> UserActionLogs { get; private set; }

        public bool IsAdmin
        {
            get { return _isAdmin; }
            private set
            {
                _isAdmin = value;
                OnPropertyChanged();
                OnPropertyChanged("TotalLogCount");
                OnPropertyChanged("WarningCount");
                OnPropertyChanged("RiskCount");
            }
        }

        public ActivityLogItem SelectedLog
        {
            get { return _selectedLog; }
            set
            {
                _selectedLog = value;
                OnPropertyChanged();
            }
        }

        public int TotalLogCount
        {
            get { return SensorLogs.Count + (IsAdmin ? UserActionLogs.Count : 0); }
        }

        public int WarningCount
        {
            get { return SensorLogs.Count(item => item.Severity == "WARN") + (IsAdmin ? UserActionLogs.Count(item => item.Severity == "WARN") : 0); }
        }

        public int RiskCount
        {
            get { return SensorLogs.Count(item => item.Severity == "RISK") + (IsAdmin ? UserActionLogs.Count(item => item.Severity == "RISK") : 0); }
        }

        public string LastEventTime
        {
            get
            {
                if (IsAdmin && UserActionLogs.Count > 0)
                {
                    return UserActionLogs[0].Time;
                }

                return SensorLogs.Count > 0 ? SensorLogs[0].Time : "--:--:--";
            }
        }

        public void SetUserAccess(UserAccount account)
        {
            IsAdmin = string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            SelectedLog = SensorLogs.Count > 0 ? SensorLogs[0] : IsAdmin && UserActionLogs.Count > 0 ? UserActionLogs[0] : null;
        }

        private void OnLogsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("TotalLogCount");
            OnPropertyChanged("WarningCount");
            OnPropertyChanged("RiskCount");
            OnPropertyChanged("LastEventTime");

            if (SensorLogs.Count > 0)
            {
                SelectedLog = SensorLogs[0];
            }
            else if (IsAdmin && UserActionLogs.Count > 0)
            {
                SelectedLog = UserActionLogs[0];
            }
        }
    }
}
