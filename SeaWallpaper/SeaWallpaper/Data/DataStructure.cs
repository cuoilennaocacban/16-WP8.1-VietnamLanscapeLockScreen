using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SeaWallpaper.Data
{
    public class WallPaperImage
    {
        public string imageUrl { get; set; }
    }

    public class FlickrImage
    {
        public string name { get; set; }
        public BitmapImage thumbnail { get; set; }
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
    }

    public class Photoset
    {
        public string id { get; set; }
        public string primary { get; set; }
        public string owner { get; set; }
        public string ownername { get; set; }
        public List<Photo> photo { get; set; }
        public int page { get; set; }
        public string per_page { get; set; }
        public string perpage { get; set; }
        public int pages { get; set; }
        public int total { get; set; }
    }

    public class RootObject
    {
        public Photoset photoset { get; set; }
        public string stat { get; set; }
    }
}
