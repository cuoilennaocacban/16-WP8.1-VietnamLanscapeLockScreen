#define DEBUG_AGENT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SeaWallpaper.Resources;
using Windows.Phone.System.UserProfile;
using System.Collections.ObjectModel;
using SeaWallpaper.Data;
using Microsoft.Phone.Scheduler;
using SeaWallpaper.Utilities;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Threading.Tasks;
using System.IO;

namespace SeaWallpaper
{
    public partial class MainPage : PhoneApplicationPage
    {
        PeriodicTask periodicTask;
        string periodicTaskName = "PeriodicAgent";
        public bool agentsAreEnabled = true;
        ObservableCollection<WallPaperImage> imageList = new ObservableCollection<WallPaperImage>();
        ObservableCollection<FlickrImage> flickrList = new ObservableCollection<FlickrImage>();

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
            CreateFlickrFolder("Flickr");
            AddData();
            imageListBox.ItemsSource = imageList;
            StartPeriodicAgent();
        }

        private void CreateFlickrFolder(string folderName)
        {
            try
            {
                IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
                if (!string.IsNullOrEmpty(folderName) && !myIsolatedStorage.DirectoryExists(folderName))
                {
                    myIsolatedStorage.CreateDirectory(folderName);
                }
            }
            catch (Exception ex)
            {
                // handle the exception
            }
        }

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
                var currentImage = LockScreen.GetImageUri();
                System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                MessageBox.Show("Lock screen changed");
            }
            else
            {
                MessageBox.Show("Background cant be updated as you clicked no!!");
            }
        }

        private void AddData()
        {
            for (int i = 0; i < 10; i++)
            {
                WallPaperImage newWallPaperImage = new WallPaperImage();
                newWallPaperImage.imageUrl = "wallpaper/CustomizedPersonalWalleper_" + i.ToString() + ".jpg";
                imageList.Add(newWallPaperImage);
            }
        }

        private void imageListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WallPaperImage selectedImage = imageListBox.SelectedItem as WallPaperImage;
            if (selectedImage != null)
            {
                LockScreenChange(selectedImage.imageUrl, true);
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
#if(DEBUG_AGENT)
                ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
                System.Diagnostics.Debug.WriteLine("Periodic task is started: " + periodicTaskName);
#endif

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

        private async void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainPivot.SelectedIndex == 1)
            {
                string[] fileList = GetFileListFromIso("Flickr");
                //DisplayFlickrImage(fileList);

                string jsonString = await StaticMethod.GetHttpAsString("http://api.flickr.com/services/rest/?method=flickr.photosets.getPhotos&api_key=a89b85007437a89df3fe1d31a777a6ed&photoset_id=72157635871020763&extras=url_o%2C+url_q&format=json&nojsoncallback=1");
                StaticData.rootFlickrObject = JsonConvert.DeserializeObject<RootObject>(jsonString);

                for (int i = 0; i < StaticData.rootFlickrObject.photoset.photo.Count; i++)
                {
                    string link = StaticData.rootFlickrObject.photoset.photo[i].url_q;
                    string name = link.Substring(link.LastIndexOf('/') + 1, link.Length - link.LastIndexOf('/') - 1);

                    if (!fileList.Contains(name))
                    {
                        //BitmapImage bitmap = await StaticMethod.DownloadImage(link);
                        CopyToIso(name, await StaticMethod.DownloadImage(link));
                    }
                    else
                    {
                        FlickrImage newFlickrImage = new FlickrImage();
                        newFlickrImage.name = name;
                        newFlickrImage.thumbnail = await FetchImage("Flickr/" + name);

                        flickrList.Add(newFlickrImage);
                    }
                }

                //fileList = GetFileListFromIso("Flickr");
                //DisplayFlickrImage(fileList);
            }
        }

        private async void DisplayFlickrImage(string[] imageList)
        {
            flickrList = new ObservableCollection<FlickrImage>();

            foreach (string fileName in imageList)
            {
                FlickrImage newFlickrImage = new FlickrImage();
                newFlickrImage.name = fileName;
                newFlickrImage.thumbnail = await FetchImage("Flickr/" + fileName);

                flickrList.Add(newFlickrImage);
            }

            imageListBox2.ItemsSource = flickrList;
        }

        private void CopyToIso(string name, BitmapImage bitmapImage)
        {
            name = "Flickr/" + name;
            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(name))
                {
                    myIsolatedStorage.DeleteFile(name);
                }

                IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(name);

                StreamResourceInfo sri = null;
                Uri uri = new Uri(name, UriKind.Relative);
                sri = Application.GetResourceStream(uri);

                WriteableBitmap wb = new WriteableBitmap(bitmapImage);

                // Encode WriteableBitmap object to a JPEG stream.
                System.Windows.Media.Imaging.Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 10);

                //wb.SaveJpeg(fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                fileStream.Close();
            }
        }

        private string[] GetFileListFromIso(string folderName)
        {
            string searchPattern = folderName + "\\*";
            string[] fileNames;
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                fileNames = myIsolatedStorage.GetFileNames(searchPattern);
            }

            return fileNames;
        }

        private Task<Stream> LoadImageAsync(string filename)
        {
            return Task.Factory.StartNew<Stream>(() =>
            {
                if (filename == null)
                {
                    throw new ArgumentException("one of parameters is null");
                }

                Stream stream = null;

                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(filename))
                    {
                        stream = isoStore.OpenFile(filename, System.IO.FileMode.Open, FileAccess.Read);
                    }
                }
                return stream;
            });
        }

        public async Task<BitmapImage> FetchImage(string imagePath)
        {
            BitmapImage image = null;
            using (var imageStream = await LoadImageAsync(imagePath))
            {
                if (imageStream != null)
                {
                    image = new BitmapImage();
                    image.SetSource(imageStream);
                }
            }
            return image;
        }

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