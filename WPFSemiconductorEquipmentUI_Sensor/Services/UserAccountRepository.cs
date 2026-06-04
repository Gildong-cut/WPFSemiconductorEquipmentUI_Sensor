using System;
using System.Data.SQLite;
using System.IO;
using WPFSemiconductorEquipmentUI_Sensor.Models;

namespace WPFSemiconductorEquipmentUI_Sensor.Services
{
    public sealed class UserAccountRepository
    {
        private const string DatabaseFolderName = "Data";
        private const string DatabaseFileName = "equipment.db";

        private readonly string _databasePath;

        public UserAccountRepository()
        {
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFolderName, DatabaseFileName);
        }

        public string DatabasePath
        {
            get { return _databasePath; }
        }

        public void Initialize()
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS users (" +
                    "user_id TEXT PRIMARY KEY NOT NULL, " +
                    "password TEXT NOT NULL, " +
                    "department TEXT NOT NULL, " +
                    "approval_status TEXT NOT NULL, " +
                    "created_at TEXT NOT NULL)";
                command.ExecuteNonQuery();
            }

            DeleteUser("operator01");
            DeleteUser("pending01");
            SeedUser("test1", "1", "Process Equipment", "Approved");
            SeedUser("test2", "1", "Process Equipment", "Pending");
        }

        public UserAccount FindByUserId(string userId)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT user_id, password, department, approval_status FROM users WHERE user_id = @user_id";
                command.Parameters.AddWithValue("@user_id", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserAccount
                    {
                        UserId = reader.GetString(0),
                        Password = reader.GetString(1),
                        Department = reader.GetString(2),
                        ApprovalStatus = reader.GetString(3)
                    };
                }
            }
        }

        public void AddOrUpdateUser(string userId, string password, string department, string approvalStatus)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "INSERT OR REPLACE INTO users (user_id, password, department, approval_status, created_at) " +
                    "VALUES (@user_id, @password, @department, @approval_status, @created_at)";
                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@department", department);
                command.Parameters.AddWithValue("@approval_status", approvalStatus);
                command.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("s"));
                command.ExecuteNonQuery();
            }
        }

        private void SeedUser(string userId, string password, string department, string approvalStatus)
        {
            if (FindByUserId(userId) != null)
            {
                return;
            }

            AddOrUpdateUser(userId, password, department, approvalStatus);
        }

        private void DeleteUser(string userId)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM users WHERE user_id = @user_id";
                command.Parameters.AddWithValue("@user_id", userId);
                command.ExecuteNonQuery();
            }
        }

        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;");
            connection.Open();
            return connection;
        }
    }
}
