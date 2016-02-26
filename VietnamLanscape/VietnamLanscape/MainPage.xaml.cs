#define DEBUG_AGENT

using Microsoft.Phone.BackgroundTransfer;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using VietnamLanscape.Data;
using VietnamLanscape.Utilities;
using Windows.Phone.System.UserProfile;

namespace VietnamLanscape
{
    public partial class MainPage : PhoneApplicationPage
    {

        #region Var for Download

        // Booleans for tracking if any transfers are waiting for user action.
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        //Background Transfer
        private IEnumerable<BackgroundTransferRequest> transferRequests;

        private string transferId = "";

        private List<Photo> selectedPhoto = new List<Photo>();

        #endregion

        #region var for periodic task

        private PeriodicTask periodicTask;
        private string periodicTaskName = "PeriodicAgent";
        public bool agentsAreEnabled = true;

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Create Flickr folder to store downloaded folder
            StaticMethod.CreateFlickrFolder(StaticData.flickrFolder);

            if (!StaticMethod.netWorkAvailable())
            {
                MessageBox.Show("Please turn on Wifi or 3g to continue");
                return;
            }
            else
            {
                LoadImageList();
            }
        }

        private async void LoadImageList()
        {
            string jsonString = await StaticMethod.GetHttpAsString("http://api.flickr.com/services/rest/?method=flickr.photosets.getPhotos&api_key=ba1de784ef9cb9d764cc8dc984cf41d8&photoset_id=72157635871020763&extras=url_o%2C+url_q&format=json&nojsoncallback=1");
            StaticData.rootFlickrObject = JsonConvert.DeserializeObject<RootObject>(jsonString);

            string[] fileList = StaticMethod.GetImageList();
            string fileListCat = "";
            foreach (string s in fileList)
            {
                fileListCat += s + ",";
            }

            foreach (Photo photo in StaticData.rootFlickrObject.photoset.photo)
            {
                if (fileListCat.Contains(photo.id))
                {
                    photo.IsDownloaded = true;
                }
                photo.DownloadProgress = 0;
            }

            imageListBox.ItemsSource = StaticData.rootFlickrObject.photoset.photo;
        }

        private void imageListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (StaticData.rootFlickrObject.photoset.photo[imageListBox.SelectedIndex].IsSelected == true)
            {
                StaticData.rootFlickrObject.photoset.photo[imageListBox.SelectedIndex].IsSelected = false;
                selectedPhoto.Remove(StaticData.rootFlickrObject.photoset.photo[imageListBox.SelectedIndex]);
            }
            else
            {
                StaticData.rootFlickrObject.photoset.photo[imageListBox.SelectedIndex].IsSelected = true;
                selectedPhoto.Add(StaticData.rootFlickrObject.photoset.photo[imageListBox.SelectedIndex]);
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            foreach (Photo photo in selectedPhoto)
            {
                if (!photo.IsDownloaded)
                {
                    AddBackgroundTransfer(photo.url_o);
                }
            }
            StaticMethod.Object2Xml(selectedPhoto);
            InitialTransferStatusCheck();
        }

        #region Download Image

        private void UpdateRequestsList()
        {
            // The Requests property returns new references, so make sure that
            // you dispose of the old references to avoid memory leaks.
            if (transferRequests != null)
            {
                foreach (var request in transferRequests)
                {
                    request.Dispose();
                }
            }
            transferRequests = BackgroundTransferService.Requests;

            if (!transferRequests.Any())
            {
                string[] fileList = StaticMethod.GetImageList();
                if (!fileList.Any())
                {
                    MessageBox.Show("Select at least 1 image to set as lock screen", "Error", MessageBoxButton.OK);
                }
                else
                {
                    ProgressIndicator progress = new ProgressIndicator
                    {
                        IsVisible = true,
                        IsIndeterminate = true,
                        Text = "Set images as lockscreen..."
                    };

                    SystemTray.SetProgressIndicator(this, progress);

                    string temp = selectedPhoto[0].url_o;
                    temp = temp.Substring(temp.LastIndexOf('/') + 1, temp.Length - temp.LastIndexOf('/') - 1);

                    LockScreenChange("Flickr/" + temp, false);

                    progress.IsVisible = false;
                    SystemTray.SetProgressIndicator(this, progress);

                    StartPeriodicAgent();
                }
            }
        }

        private void RemoveTransferRequest(string transferID)
        {
            // Use Find to retrieve the transfer request with the specified ID.
            BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);

            // Try to remove the transfer from the background transfer service.
            try
            {
                BackgroundTransferService.Remove(transferToRemove);
                transferId = "";
            }
            catch (Exception e)
            {
                // Handle the exception.
            }
        }

        void transfer_TransferStatusChanged(object sender, BackgroundTransferEventArgs e)
        {
            ProcessTransfer(e.Request);
            UpdateRequestsList();
        }

        void transfer_TransferProgressChanged(object sender, BackgroundTransferEventArgs e)
        {
            //progressTextBlock.Text = e.Request.BytesReceived + " / " + e.Request.TotalBytesToReceive;
            //statusProgressBar.Value = (e.Request.BytesReceived * 100) / e.Request.TotalBytesToReceive;

            //ProgressIndicator progress = new ProgressIndicator
            //{
            //    IsVisible = true,
            //    IsIndeterminate = false,
            //    Value = (float) (e.Request.BytesReceived)/e.Request.TotalBytesToReceive,
            //    Text =
            //        "Downloading " + (selectedPhoto.Count - transferRequests.Count() + 1) + " of " + selectedPhoto.Count +
            //        " ..." + ((e.Request.BytesReceived*100)/e.Request.TotalBytesToReceive).ToString() + "%"
            //};

            //SystemTray.SetProgressIndicator(this, progress);

            string filename = e.Request.Tag;
            foreach (Photo photo in StaticData.rootFlickrObject.photoset.photo)
            {
                if (filename.Contains(photo.id))
                {
                    photo.DownloadProgress = Convert.ToInt32((e.Request.BytesReceived * 100) / e.Request.TotalBytesToReceive);
                }
            }
        }

        private void ProcessTransfer(BackgroundTransferRequest transfer)
        {
            switch (transfer.TransferStatus)
            {
                case Microsoft.Phone.BackgroundTransfer.TransferStatus.Completed:

                    // If the status code of a completed transfer is 200 or 206, the
                    // transfer was successful
                    if (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                    {
                        // Remove the transfer request in order to make room in the 
                        // queue for more transfers. Transfers are not automatically
                        // removed by the system.
                        RemoveTransferRequest(transfer.RequestId);
                        //progressTextBlock.Text = "Transfer Completed";

                        // In this example, the downloaded file is moved into the root
                        // Isolated Storage directory
                        using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            string filename = transfer.Tag;
                            //StaticData.downloadedFile = filename;

                            if (isoStore.FileExists(filename))
                            {
                                isoStore.DeleteFile(filename);
                            }

                            isoStore.MoveFile(transfer.DownloadLocation.OriginalString, filename);

                            foreach (Photo photo in StaticData.rootFlickrObject.photoset.photo)
                            {
                                if (filename.Contains(photo.id))
                                {
                                    photo.IsDownloaded = true;
                                    //StaticMethod.Object2Xml(selectedPhoto);
                                }
                            }
                        }

                        transferId = "";

                        //NavigationService.Navigate(new Uri("/ViewGroup/ViewPage.xaml", UriKind.Relative));
                        //(Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/ViewGroup/ViewPage.xaml", UriKind.Relative));
                    }
                    else
                    {
                        // This is where you can handle whatever error is indicated by the
                        // StatusCode and then remove the transfer from the queue. 
                        RemoveTransferRequest(transfer.RequestId);

                        if (transfer.TransferError != null)
                        {
                            // Handle TransferError if one exists.
                            //progressTextBlock.Text = "Transfer Errors";

                            ProgressIndicator progress = new ProgressIndicator
                            {
                                IsVisible = true,
                                IsIndeterminate = false,
                                Text = "Transfer Errors"
                            };

                            SystemTray.SetProgressIndicator(this, progress);
                        }
                    }
                    break;


                case Microsoft.Phone.BackgroundTransfer.TransferStatus.WaitingForExternalPower:
                    WaitingForExternalPower = true;
                    //statusTextBlock.Text = "Transfer WaitingForExternalPower";
                    break;

                case Microsoft.Phone.BackgroundTransfer.TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                    WaitingForExternalPowerDueToBatterySaverMode = true;
                    //statusTextBlock.Text = "Transfer WaitingForExternalPowerDueToBatterySaverMode";
                    break;

                case Microsoft.Phone.BackgroundTransfer.TransferStatus.WaitingForNonVoiceBlockingNetwork:
                    WaitingForNonVoiceBlockingNetwork = true;
                    //statusTextBlock.Text = "Transfer WaitingForNonVoiceBlockingNetwork";
                    break;

                case Microsoft.Phone.BackgroundTransfer.TransferStatus.WaitingForWiFi:
                    WaitingForWiFi = true;
                    //statusTextBlock.Text = "Transfer WaitingForWiFi";
                    break;
            }
        }

        private void InitialTransferStatusCheck()
        {
            UpdateRequestsList();

            foreach (var transfer in transferRequests)
            {
                transfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                transfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                ProcessTransfer(transfer);
            }

            if (WaitingForExternalPower)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power to continue transferring.");
            }
            if (WaitingForExternalPowerDueToBatterySaverMode)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
            }
            if (WaitingForNonVoiceBlockingNetwork)
            {
                MessageBox.Show("You have one or more file transfers waiting for a network that supports simultaneous voice and data.");
            }
            if (WaitingForWiFi)
            {
                MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring.");
            }
        }

        public void AddBackgroundTransfer(string transferFileName)
        {
            // Bind the list of URLs to the ListBox.
            //URLListBox.ItemsSource = urls;

            // Make sure that the required "/shared/transfers" directory exists in isolated storage.
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isoStore.DirectoryExists("/shared/transfers"))
                {
                    isoStore.CreateDirectory("/shared/transfers");
                }
            }

            // Check to see if the maximum number of requests per app has been exceeded.
            //if (BackgroundTransferService.Requests
            //{
            //    // Note: Instead of showing a message to the user, you could store the
            //    // requested file URI in isolated storage and add it to the queue later.
            //    MessageBox.Show("The maximum number of background file transfer requests for this application has been exceeded. ");
            //    return;
            //}

            //string transferFileName = ((Button)sender).Tag as string;

            //string transferFileName = downloadLink.TrimEnd('g', 'j', 'p') + "zip";
            //string transferFileName = downloadLink;
            Uri transferUri = new Uri(Uri.EscapeUriString(transferFileName), UriKind.RelativeOrAbsolute);

            // Create the new transfer request, passing in the URI of the file to be transferred.
            BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(transferUri);

            // Set the transfer method. GET and POST are supported.
            transferRequest.Method = "GET";

            // Get the file name from the end of the transfer URI and create a local URI 
            // in the "transfers" directory in isolated storage.
            string downloadFile = transferFileName.Substring(transferFileName.LastIndexOf("/") + 1);
            Uri downloadUri = new Uri("shared/transfers/" + downloadFile, UriKind.RelativeOrAbsolute);
            transferRequest.DownloadLocation = downloadUri;

            // Pass custom data with the Tag property. In this example, the friendly name
            // is passed.
            //transferRequest.Tag = StaticData.musicFolder + "/" + downloadFile + ".mp4";
            transferRequest.Tag = StaticData.flickrFolder + "/" + downloadFile;

            // If the Wi-Fi-only check box is not checked, then set the TransferPreferences
            // to allow transfers over a cellular connection.

            transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
            // Add the transfer request using the BackgroundTransferService. Do this in 
            // a try block in case an exception is thrown.
            try
            {
                BackgroundTransferService.Add(transferRequest);
                //transferId = transferRequest.RequestId;
                //InitialTransferStatusCheck();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Unable to add background transfer request. " + ex.Message);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to add background transfer request.");
            }
        }

        #endregion

        #region Set as LockScreen

        private async void LockScreenChange(string filePathOfTheImage, bool isAppResource)
        {
            if (!LockScreenManager.IsProvidedByCurrentApplication)
            {
                // If you're not the provider, this call will prompt the user for permission.
                // Calling RequestAccessAsync from a background agent is not allowed.
                await LockScreenManager.RequestAccessAsync();
            }

            // Only do further work if the access is granted.
            if (LockScreenManager.IsProvidedByCurrentApplication)
            {
                // At this stage, the app is the active lock screen background provider.
                // The following code example shows the new URI schema.
                // ms-appdata points to the root of the local app data folder.
                // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package
                var schema = isAppResource ? "ms-appx:///" : "ms-appdata:///Local/";
                var uri = new Uri(schema + filePathOfTheImage, UriKind.Absolute);

                // Set the lock screen background image.
                LockScreen.SetImageUri(uri);

                // Get the URI of the lock screen background image.
                //var currentImage = LockScreen.GetImageUri();
                //System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                MessageBox.Show("Lock screen changed");
            }
            else
            {
                MessageBox.Show("Background can't be updated as you clicked no!!");
            }
        }

        private void StartPeriodicAgent()
        {
            // is old task running, remove it
            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;
            if (periodicTask != null)
            {
                try
                {
                    ScheduledActionService.Remove(periodicTaskName);
                }
                catch (Exception)
                {
                }
            }
            // create a new task
            periodicTask = new PeriodicTask(periodicTaskName);
            // load description from localized strings
            periodicTask.Description = "This is Lockscreen image provider app.";
            // set expiration days
            periodicTask.ExpirationTime = DateTime.Now.AddDays(14);
            try
            {
                // add thas to scheduled action service
                ScheduledActionService.Add(periodicTask);
                // debug, so run in every 30 secs
//#if(DEBUG_AGENT)
                ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
                System.Diagnostics.Debug.WriteLine("Periodic task is started: " + periodicTaskName);
//#endif

            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    // load error text from localized strings
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }
                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
            catch (SchedulerServiceException)
            {
                // No user action required.
            }
        }

        #endregion

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}