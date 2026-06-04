using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using WPFSemiconductorEquipmentUI_Sensor.Models;

namespace WPFSemiconductorEquipmentUI_Sensor.Services
{
    public sealed class ActivityLogStore
    {
        private const int MaximumVisibleLogs = 500;
        private static readonly ActivityLogStore CurrentInstance = new ActivityLogStore();
        private readonly UserAccountRepository _userRepository;
        private bool _initialized;

        private ActivityLogStore()
        {
            _userRepository = new UserAccountRepository();
            Logs = new ObservableCollection<ActivityLogItem>();
            SensorLogs = new ObservableCollection<ActivityLogItem>();
            UserActionLogs = new ObservableCollection<ActivityLogItem>();
        }

        public static ActivityLogStore Instance
        {
            get { return CurrentInstance; }
        }

        public ObservableCollection<ActivityLogItem> Logs { get; private set; }
        public ObservableCollection<ActivityLogItem> SensorLogs { get; private set; }
        public ObservableCollection<ActivityLogItem> UserActionLogs { get; private set; }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _userRepository.Initialize();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS activity_logs (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "created_at TEXT NOT NULL, " +
                    "source TEXT NOT NULL, " +
                    "user_id TEXT NOT NULL, " +
                    "event_text TEXT NOT NULL, " +
                    "severity TEXT NOT NULL)";
                command.ExecuteNonQuery();
            }

            LoadRecentLogs();
            _initialized = true;
        }

        public void Add(string source, string userId, string eventText, string severity)
        {
            Initialize();
            var createdAt = DateTime.Now;

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "INSERT INTO activity_logs (created_at, source, user_id, event_text, severity) " +
                    "VALUES (@created_at, @source, @user_id, @event_text, @severity)";
                command.Parameters.AddWithValue("@created_at", createdAt.ToString("s"));
                command.Parameters.AddWithValue("@source", source);
                command.Parameters.AddWithValue("@user_id", string.IsNullOrWhiteSpace(userId) ? "system" : userId);
                command.Parameters.AddWithValue("@event_text", eventText);
                command.Parameters.AddWithValue("@severity", severity);
                command.ExecuteNonQuery();
            }

            var item = CreateItem(createdAt, source, userId, eventText, severity);
            Logs.Insert(0, item);
            GetCategoryLogs(source).Insert(0, item);
            while (Logs.Count > MaximumVisibleLogs)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }

            TrimCategoryLogs(SensorLogs);
            TrimCategoryLogs(UserActionLogs);
        }

        private void LoadRecentLogs()
        {
            Logs.Clear();
            SensorLogs.Clear();
            UserActionLogs.Clear();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT created_at, source, user_id, event_text, severity " +
                    "FROM activity_logs ORDER BY id DESC LIMIT " + MaximumVisibleLogs;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime createdAt;
                        if (!DateTime.TryParse(reader.GetString(0), out createdAt))
                        {
                            createdAt = DateTime.Now;
                        }

                        var item = CreateItem(
                            createdAt,
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4));
                        Logs.Add(item);
                        GetCategoryLogs(item.Source).Add(item);
                    }
                }
            }
        }

        private ObservableCollection<ActivityLogItem> GetCategoryLogs(string source)
        {
            return string.Equals(source, "Sensor", StringComparison.OrdinalIgnoreCase)
                ? SensorLogs
                : UserActionLogs;
        }

        private static void TrimCategoryLogs(ObservableCollection<ActivityLogItem> logs)
        {
            while (logs.Count > MaximumVisibleLogs)
            {
                logs.RemoveAt(logs.Count - 1);
            }
        }

        private static ActivityLogItem CreateItem(DateTime createdAt, string source, string userId, string eventText, string severity)
        {
            return new ActivityLogItem
            {
                Time = createdAt.ToString("HH:mm:ss"),
                Source = source,
                User = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Event = eventText,
                Severity = severity,
                Saved = "YES"
            };
        }

        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection("Data Source=" + _userRepository.DatabasePath + ";Version=3;");
            connection.Open();
            return connection;
        }
    }
}
