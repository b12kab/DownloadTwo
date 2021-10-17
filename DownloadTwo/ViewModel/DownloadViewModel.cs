using DownloadTwo.Helper;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DownloadTwo.ViewModel
{
    public class DownloadViewModel : INotifyPropertyChanged
    {
        public INavigation _navigation;
        public ICommand DownloadCommand { get; private set; }
        public ICommand DownloadCheckCommand { get; private set; }
        public ICommand DownloadDeleteCommand { get; private set; }
        public ICommand DownloadUpdateCommand { get; private set; }
 
        public DownloadViewModel(INavigation navigation)
        {
            _navigation = navigation;

            DownloadCommand = new Command(async () => await ExportData());
            DownloadCheckCommand = new Command(async () => await CheckExportFile());
            DownloadDeleteCommand = new Command(async () => await DeleteExportFile());
            DownloadUpdateCommand = new Command(async () => await UpdateExportFile());
            this.DownloadButtonEnabled = false;
            this.UpdateButtonEnabled = false;
            this.PendingFlag = false;
            this.AndroidUri = null;
        }

        private ExportData exportInfo;

        /// <summary>
        /// Update the indicator
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateExportFile()
        {
            if (exportInfo == null)
            {
                exportInfo = new ExportData();
            }

            if (PendingFlag)
            {
                if (string.IsNullOrWhiteSpace(AndroidUri))
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Update not currently possilble", "OK");
                    return;
                }

                bool worked = exportInfo.UpdateExportFile(AndroidUri, out bool updatedOK);

                if (worked)
                {
                    if (updatedOK)
                    {
                        await Application.Current.MainPage.DisplayAlert("Export Data", "Export file updated", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Export Data", "Export file not updated", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export file check failed", "OK");
                }

                this.UpdateButtonEnabled = false;
                this.AndroidUri = null;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Export Data", "Pending not currently set", "OK");
            }
        }

        /// <summary>
        /// Delete the file entry in the database, if possible
        /// </summary>
        /// <returns>Task</returns>
        private async Task DeleteExportFile()
        {
            if (exportInfo == null)
            {
                exportInfo = new ExportData();
            }

            bool worked = exportInfo.DeleteExportFile(Filename, out bool deletedOK);

            if (worked)
            {
                if (deletedOK)
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export file deleted", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export file not deleted", "OK");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Export Data", "Export file check failed", "OK");
            }

            this.DownloadButtonEnabled = false;
            this.UpdateButtonEnabled = false;
        }

        /// <summary>
        /// Check for the file in the database
        /// </summary>
        /// <returns>Task</returns>
        private async Task CheckExportFile()
        {
            if (exportInfo == null)
            {
                exportInfo = new ExportData();
            }

            bool worked = exportInfo.CheckExportFile(Filename, out bool found, out _androidUri);

            if (worked)
            {
                this.DownloadButtonEnabled = found;
                if (found)
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export file exists", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export file missing", "OK");
                }
            }
            else
            {
                this.DownloadButtonEnabled = false;
                this.UpdateButtonEnabled = false;
                await Application.Current.MainPage.DisplayAlert("Export Data", "Export file check failed", "OK");
            }
        }

        /// <summary>
        /// Export data to file
        /// </summary>
        /// <returns>Task</returns>
        private async Task ExportData()
        {
            if (_fileContents == null)
            {
                Contents = "default text";
            }

            bool exportData = await Application.Current.MainPage.DisplayAlert("Export Data", "Do you want to export the data?", "OK", "Cancel");
            if (exportData)
            {
                bool permissionGranted = await CheckPermission();
                if (!permissionGranted)
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "You didn't give permission. If you wish to give permission, please agree to the permission", "OK");
                }

                if (exportInfo == null)
                {
                    exportInfo = new ExportData();
                }

                bool worked = exportInfo.CreateExportFile(Contents, Filename, PendingFlag, out _androidUri);

                if (worked)
                {
                    if (PendingFlag)
                    {
                        this.UpdateButtonEnabled = true;
                    }
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export worked", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Export Data", "Export Failed", "OK");
                    this.UpdateButtonEnabled = false;
                }
            }
        }

        /// <summary>
        /// Check to see if permissions are needed for Android, where it's API 28 or less
        /// </summary>
        /// <returns>Task</returns>
        private async Task<bool> CheckPermission()
        {
            bool permissionNeeded = DependencyService.Get<IExport>().IsPermissionNeeded();

            if (!permissionNeeded)
            {
                return true;
            }

            var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            if (Permissions.ShouldShowRationale<Permissions.StorageWrite>())
            {
                await Application.Current.MainPage.DisplayAlert("Export Data", "Without this permission, we can't export the file", "OK");
            }

            status = await Permissions.RequestAsync<Permissions.StorageWrite>();


            if (status == PermissionStatus.Granted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //---------------------------------------------------------------------
        //---------------------------------------------------------------------

        private bool _pendingFlag;
        public bool PendingFlag
        {
            get => _pendingFlag;
            set
            {
                _pendingFlag = value;
                NotifyPropertyChanged("PendingFlag");
            }
        }

        private bool _deleteEnabled;
        public bool DownloadButtonEnabled
        {
            get => _deleteEnabled;
            set
            {
                _deleteEnabled = value;
                NotifyPropertyChanged("DownloadButtonEnabled");
            }
        }

        private bool _updateEnabled;
        public bool UpdateButtonEnabled
        {
            get => _updateEnabled;
            set
            {
                _updateEnabled = value;
                NotifyPropertyChanged("UpdateButtonEnabled");
            }
        }

        private static string _filename = "testing_file.txt";
        public string Filename
        {
            get => _filename;
            set
            {
                _filename = value;
                NotifyPropertyChanged("Filename");
            }
        }

        private static string _fileContents;
        public string Contents
        {
            get => _fileContents;
            set
            {
                _fileContents = value;
                NotifyPropertyChanged("Contents");
            }
        }

        private static string _androidUri;
        public string AndroidUri
        {
            get => _androidUri;
            set
            {
                _androidUri = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
