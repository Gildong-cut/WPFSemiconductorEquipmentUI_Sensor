namespace WPFSemiconductorEquipmentUI_Sensor.ViewModels
{
    using System;
    using WPFSemiconductorEquipmentUI_Sensor.Models;
    using WPFSemiconductorEquipmentUI_Sensor.Services;

    public class LoginViewModel : ScreenViewModelBase
    {
        private readonly UserAccountRepository _repository;
        private string _userId;
        private string _department;
        private string _loginStatusText;
        private string _loginStatusTone;

        public event Action<UserAccount> LoginSucceeded;

        public LoginViewModel()
        {
            _repository = new UserAccountRepository();
            _repository.Initialize();

            Title = "Login / Sign up";
            Description = "Only approved operators can enter the equipment control console.";
            UserId = "test1";
            Department = "Process Equipment";
            LoginStatusText = "Try admin / 1, test1 / 1, or test2 / 1.";
            LoginStatusTone = "Blue";
        }

        public string UserId
        {
            get { return _userId; }
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }

        public string Department
        {
            get { return _department; }
            set
            {
                _department = value;
                OnPropertyChanged();
            }
        }

        public string LoginStatusText
        {
            get { return _loginStatusText; }
            private set
            {
                _loginStatusText = value;
                OnPropertyChanged();
            }
        }

        public string LoginStatusTone
        {
            get { return _loginStatusTone; }
            private set
            {
                _loginStatusTone = value;
                OnPropertyChanged();
            }
        }

        public void Login(string password)
        {
            var account = _repository.FindByUserId(UserId);
            if (account == null || account.Password != password)
            {
                LoginStatusText = "Login failed. Check user ID and password.";
                LoginStatusTone = "Danger";
                ActivityLogStore.Instance.Add("Auth", UserId, "Login failed", "WARN");
                return;
            }

            Department = account.Department;
            if (account.Role == "Admin")
            {
                LoginStatusText = "Administrator login: " + account.UserId + " has full equipment access.";
                LoginStatusTone = "Normal";
                ActivityLogStore.Instance.Add("Auth", account.UserId, "Administrator login successful", "INFO");
                RaiseLoginSucceeded(account);
                return;
            }

            if (account.ApprovalStatus == "Approved")
            {
                LoginStatusText = "Approved login: " + account.UserId + " can access equipment controls.";
                LoginStatusTone = "Normal";
                ActivityLogStore.Instance.Add("Auth", account.UserId, "Approved user login successful", "INFO");
                RaiseLoginSucceeded(account);
                return;
            }

            LoginStatusText = "Pending approval: " + account.UserId + " is saved in SQLite but cannot use controls yet.";
            LoginStatusTone = "Warning";
            ActivityLogStore.Instance.Add("Auth", account.UserId, "Pending user login successful with controls locked", "WARN");
            RaiseLoginSucceeded(account);
        }

        public void SignUp(string password)
        {
            if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(password))
            {
                LoginStatusText = "Sign up needs both user ID and password.";
                LoginStatusTone = "Warning";
                return;
            }

            _repository.AddOrUpdateUser(UserId, password, Department, "Pending");
            LoginStatusText = "Pending account saved: " + UserId + ".";
            LoginStatusTone = "Warning";
            ActivityLogStore.Instance.Add("Auth", UserId, "New pending account registered", "INFO");
        }

        private void RaiseLoginSucceeded(UserAccount account)
        {
            var handler = LoginSucceeded;
            if (handler != null)
            {
                handler(account);
            }
        }
    }
}
