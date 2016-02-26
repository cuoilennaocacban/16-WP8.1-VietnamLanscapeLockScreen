using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SeaWallpaper.Utilities
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

        public static async Task<BitmapImage> DownloadImage(string imageLink)
        {
            WebRequest request = WebRequest.Create(new Uri(imageLink, UriKind.Absolute));
            WebResponse response = await request.GetResponseAsync();

            Stream responseStream = response.GetResponseStream();

            BitmapImage bitmapImage = new BitmapImage();
            //bitmapImage.CreateOptions = BitmapCreateOptions.None;
            bitmapImage.SetSource(responseStream);

            return bitmapImage;
        }
    }
}
