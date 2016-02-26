using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VietnamLanscape.Annotations;

namespace VietnamLanscape.Data
{
    public class Photo : INotifyPropertyChanged
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private int downloadProgress;

        public int DownloadProgress
        {
            get { return downloadProgress; }
            set
            {
                if (value == downloadProgress) return;
                downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
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
