using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using Windows.Phone.System.UserProfile;
using System;

namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
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

            string imgCount = currentImage.ToString().Substring(currentImage.ToString().IndexOf('_') + 1, currentImage.ToString().Length - (currentImage.ToString().IndexOf('_') + 1)).Replace(".jpg", "");

            

            LockScreenChange(Imagename, true);

            // If debugging is enabled, launch the agent again in one minute.
            // debug, so run in every 30 secs
#if(DEBUG_AGENT)
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
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