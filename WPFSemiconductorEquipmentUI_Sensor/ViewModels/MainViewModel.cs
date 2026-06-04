using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WPFSemiconductorEquipmentUI_Sensor.Models;
using WPFSemiconductorEquipmentUI_Sensor.Services;

namespace WPFSemiconductorEquipmentUI_Sensor.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private object _currentViewModel;
        private bool _disposed;
        private readonly LoginViewModel _loginViewModel;
        private readonly ConsoleViewModel _consoleViewModel;
        private readonly LogsViewModel _logsViewModel;
        private readonly NavigationItem _authNavigationItem;
        private readonly NavigationItem _consoleNavigationItem;
        private readonly NavigationItem _logsNavigationItem;
        private string _currentUserId = "guest";

        public MainViewModel()
        {
            _loginViewModel = new LoginViewModel();
            _logsViewModel = new LogsViewModel();
            var settings = new SettingsViewModel();
            _consoleViewModel = new ConsoleViewModel(new AdsSensorTrainerClient(), settings);

            _authNavigationItem = new NavigationItem { Title = "Auth", ViewModel = _loginViewModel, IsSelected = true };
            _consoleNavigationItem = new NavigationItem { Title = "Console", ViewModel = _consoleViewModel };
            _logsNavigationItem = new NavigationItem { Title = "Logs", ViewModel = _logsViewModel, IsEnabled = false };

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                _authNavigationItem,
                _consoleNavigationItem,
                _logsNavigationItem,
                new NavigationItem { Title = "Settings", ViewModel = settings }
            };

            PendingViewModel = new PendingViewModel();
            CurrentViewModel = _loginViewModel;
            NavigateCommand = new RelayCommand(Navigate);
            _loginViewModel.LoginSucceeded += OnLoginSucceeded;
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; private set; }
        public PendingViewModel PendingViewModel { get; private set; }
        public ICommand NavigateCommand { get; private set; }

        public object CurrentViewModel
        {
            get { return _currentViewModel; }
            private set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        private void Navigate(object parameter)
        {
            var item = parameter as NavigationItem;
            if (item == null)
            {
                return;
            }

            if (!item.IsEnabled)
            {
                ActivityLogStore.Instance.Add("Security", _currentUserId, "Denied access to " + item.Title + " view", "WARN");
                return;
            }

            ActivityLogStore.Instance.Add("Navigation", _currentUserId, "Opened " + item.Title + " view", "INFO");
            NavigateTo(item);
        }

        private void OnLoginSucceeded(UserAccount account)
        {
            _currentUserId = account.UserId;
            var isApproved = string.Equals(account.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            _logsNavigationItem.IsEnabled = isApproved || isAdmin;
            _logsViewModel.SetUserAccess(account);
            _consoleViewModel.SetUserAccess(account);
            NavigateTo(_consoleNavigationItem);
        }

        private void NavigateTo(NavigationItem item)
        {
            foreach (var navigationItem in NavigationItems)
            {
                navigationItem.IsSelected = false;
            }

            item.IsSelected = true;
            CurrentViewModel = item.ViewModel;
            OnPropertyChanged("NavigationItems");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _loginViewModel.LoginSucceeded -= OnLoginSucceeded;

            foreach (var item in NavigationItems)
            {
                var disposable = item.ViewModel as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
