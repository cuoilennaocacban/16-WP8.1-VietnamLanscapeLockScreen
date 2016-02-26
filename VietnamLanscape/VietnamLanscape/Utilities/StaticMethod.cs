using System.Collections.ObjectModel;
using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Media;
using VietnamLanscape.Data;

namespace VietnamLanscape.Utilities
{
    public class StaticMethod
    {
        public static async Task<string> GetHttpAsString(string link)
        {
            WebRequest request = WebRequest.Create(new Uri(link, UriKind.Absolute));
            WebResponse response = await request.GetResponseAsync();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string result = reader.ReadToEnd();

            return result;
        }

        public static void CreateFlickrFolder(string folderName)
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

        public static string[] GetImageList()
        {
            string[] result;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                result = isf.GetFileNames("Flickr\\*");
            }

            return result;
        }

        public static bool netWorkAvailable()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                //Logger.log(TAG, "netWorkAvailable()");
                return true;
            }
            return false;
        }

        public static void Object2Xml(List<Photo> selectedPhotos)
        {
            // Write to the Isolated Storage
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Data.xml", FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(selectedPhotos.GetType());
                    using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, selectedPhotos);
                    }
                }
            }
        }

        public static ObservableCollection<Photo> XmlToObject()
        {
            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("/Recorded/Data.xml", FileMode.Open))
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
    }
}
