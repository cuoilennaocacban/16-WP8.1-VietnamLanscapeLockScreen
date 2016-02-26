#define DEBUG_AGENT

using Microsoft.Phone.Scheduler;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Xml.Serialization;
using Windows.Phone.System.UserProfile;

namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {

        private ObservableCollection<Photo> photoCollection; 

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        private string[] GetImageList()
        {
            string[] result;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                result = isf.GetFileNames("Flickr\\*");
            }

            return result;
        }

        public class Photo
        {
            public string id { get; set; }
            public string secret { get; set; }
            public string server { get; set; }
            public int farm { get; set; }
            public string title { get; set; }
            public int isprimary { get; set; }
            public string url_o { get; set; }
            public string height_o { get; set; }
            public string width_o { get; set; }
            public string url_q { get; set; }
            public string height_q { get; set; }
            public string width_q { get; set; }

            private bool isDownloaded;
            public bool IsDownloaded
            {
                get { return isDownloaded; }
                set
                {
                    if (value.Equals(isDownloaded)) return;
                    isDownloaded = value;
                }
            }

            private bool isSelected;
            public bool IsSelected
            {
                get { return isSelected; }
                set
                {
                    if (value.Equals(isSelected)) return;
                    isSelected = value;
                }
            }
        }

        public static ObservableCollection<Photo> XmlToObject()
        {
            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Data.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Photo>));
                        ObservableCollection<Photo> data = (ObservableCollection<Photo>)serializer.Deserialize(stream);
                        return data;
                    }
                }
            }
            catch
            {
                //add some code here
            }

            return null;
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            // Get the URI of the lock screen background image.
            var currentImage = LockScreen.GetImageUri();
            string Imagename = string.Empty;

            photoCollection = XmlToObject();

            //string[] imageList = GetImageList();

            //string imgCount = currentImage.ToString().Substring(currentImage.ToString().IndexOf('_') + 1, currentImage.ToString().Length - (currentImage.ToString().IndexOf('_') + 1)).Replace(".jpg", "");

            //if (imgCount != "9")
            //    Imagename = "wallpaper/CustomizedPersonalWalleper_" + Convert.ToString(Convert.ToUInt32(imgCount) + 1) + ".jpg";
            //else
            //    Imagename = "wallpaper/CustomizedPersonalWalleper_0.jpg";

            string current = currentImage.ToString();
            current = current.Substring(current.LastIndexOf('/') + 1, current.Length - current.LastIndexOf('/') - 1);

            //for (int i = 0; i < imageList.GetLength(0); i++)
            //{
            //    if (current == imageList[i])
            //    {
            //        Imagename = "Flickr/" + imageList[i + 1];
            //    }
            //}

            bool isSet = false;

            for (int i = 0; i < photoCollection.Count - 1; i++)
            {
                if (current.Contains(photoCollection[i].id))
                {
                    string temp;
                    if (i == photoCollection.Count - 1)
                    {
                        temp = photoCollection[0].url_o;
                    }
                    else
                    {
                        temp = photoCollection[i + 1].url_o;
                    }

                    Imagename = "Flickr/" +
                                temp.Substring(temp.LastIndexOf('/') + 1, temp.Length - temp.LastIndexOf('/') - 1);
                    isSet = true;
                }
            }
            if (!isSet)
            {
                if (photoCollection.Count != 0)
                {
                    string temp = photoCollection[0].url_o;
                    Imagename = "Flickr/" +
                                temp.Substring(temp.LastIndexOf('/') + 1, temp.Length - temp.LastIndexOf('/') - 1);
                    isSet = true;
                }
            }

            if (isSet)
            {
                LockScreenChange(Imagename, false);
            }

            // If debugging is enabled, launch the agent again in one minute.
            // debug, so run in every 30 secs
#if(DEBUG_AGENT)
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(10));
            System.Diagnostics.Debug.WriteLine("Periodic task is started again: " + task.Name);
#endif

            // Call NotifyComplete to let the system know the agent is done working.
            NotifyComplete();
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
            }
        }
    }
}