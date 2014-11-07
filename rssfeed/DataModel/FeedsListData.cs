using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Windows.Storage;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;

namespace rssfeed.Data
{
    class Integer
    {
        public int Value;
        public Integer(int _value)
        {
            this.Value = _value;
        }
    }

    [DataContract]
    class FeedsListItem {
        private string _Name;
        private string _SiteURL;
        private string _URL;
        
        [DataMember]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        [DataMember]
        public string SiteURL
        {
            get { return _SiteURL; }
            set { _SiteURL = value; }
        }
        [DataMember]
        public string URL
        {
            get { return _URL; }
            set { _URL = value; }
        }

        public FeedsListItem(String _name, String _siteurl, String _url)
        {
            this.Name = _name;
            this.SiteURL = _siteurl;
            this.URL = _url;
        }
        public override string ToString()
        {
            return this.Name;
        }
    }

    class FeedsListData : INotifyPropertyChanged
    {
        private const string XMLFILENAME = "FeedsList.xml";
        private static FeedsListData _dataSource = new FeedsListData();
        private ObservableCollection<FeedsListItem> _feeds = new ObservableCollection<FeedsListItem>();
        public ObservableCollection<FeedsListItem> Feeds
        {
            get { return this._feeds; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void PropChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public static FeedsListItem GetFeedItem(int idx)
        {
            if ((idx >= 0) && (idx < _dataSource.Feeds.Count))
                return _dataSource.Feeds[idx];
            else
                return null;
        }

        public static async Task<IEnumerable<FeedsListItem>> GetFeedsAsync()
        {
            await _dataSource.ReadFileAsync();
            return _dataSource.Feeds;
        }

        public async Task ReadFileAsync()
        {
            if (this._feeds.Count != 0)
                return;
            StorageFile file = null;
            bool fileNotFound = false;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(XMLFILENAME);
            }
            catch
            {
                fileNotFound = true;
            }
            if (fileNotFound)
            {
                Uri dataUri = new Uri("ms-appx:///DataModel/" + XMLFILENAME);
                file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            }
            using (var inStream = await file.OpenSequentialReadAsync())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<FeedsListItem>));
                this._feeds = (ObservableCollection<FeedsListItem>)serializer.ReadObject(inStream.AsStreamForRead());
            }
        }

        private async Task SaveAsync()
        {
            var serializer = new DataContractSerializer(typeof(ObservableCollection<FeedsListItem>));
            MemoryStream sessionData = new MemoryStream();
            serializer.WriteObject(sessionData, this.Feeds);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(XMLFILENAME, CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                sessionData.Seek(0, SeekOrigin.Begin);
                await sessionData.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            _dataSource.PropChanged("Feeds");
        }

        public static async Task SetFeedAsync(String _name, String _siteurl, String _url, int idx)
        {
            FeedsListItem feed = new FeedsListItem(_name, _siteurl, _url);
            if ((idx >= 0) && (idx < _dataSource.Feeds.Count))
                _dataSource.Feeds[idx] = feed;
            else
                _dataSource.Feeds.Add(feed);
            await _dataSource.SaveAsync();
        }

        public static async Task DeleteFeed(int idx)
        {
            if ((idx >= 0) && (idx < _dataSource.Feeds.Count))
            {
                _dataSource.Feeds.RemoveAt(idx);
                await _dataSource.SaveAsync();
            }
        }
    }
}
