using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ComponentModel;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Web.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using CookComputing.XmlRpc;
using WP7toWordpressXMLRPC;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.UI.ViewManagement;

namespace rssfeed.Data
{
    public delegate void PostProgressHandler(string error = null); // The type

    [DataContract]
    public class PickedItem
    {
        [DataMember]
        public string Title {get; set;}
        [DataMember]
        public string Content {get; set;}
        [DataMember]
        public string Thumbnail {get; set;}
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public DateTimeOffset Timestamp { get; set; }
        [DataMember]
        public string URL { get; set; }
        [DataMember]
        public string FeedName { get; set; }

        public PickedItem(String _title, String _content, String _thumbnail, DateTimeOffset _timestamp, String _url, String _feedName)
        {
            this.Title = _title;
            this.Content = _content;
            this.Thumbnail = _thumbnail;
            this.Timestamp = _timestamp;
            this.Status = "new";
            this.URL = _url;
            this.FeedName = _feedName;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class PickedItemsSource : INotifyPropertyChanged
    {
        private const string XMLFILENAME = "PickedItems.xml";
        private static PickedItemsSource _dataSource = new PickedItemsSource();
        public static DispatcherTimer updateTimer = new DispatcherTimer();
        public static bool scanInProgress = false;
        private static DispatcherTimer cleanupTimer = new DispatcherTimer();
        private static IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
        private ObservableCollection<PickedItem> _items = new ObservableCollection<PickedItem>();
        public static ListView viewer;
        public IEnumerable<PickedItem> Items
        {
            get { return this._items.Where(item => item.Status == "new"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            if (viewer != null)
                viewer.ItemsSource = _dataSource.Items;
            //viewer.UpdateLayout();
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        static PickedItemsSource()
        {
            updateTimer.Tick += new EventHandler<object>(ScanFeeds);
            cleanupTimer.Interval = TimeSpan.FromDays(1.0);
            cleanupTimer.Tick += new EventHandler<object>(CleanDB);
            CleanDB(null, null);
            //cleanupTimer.Start();
            if (settings.ContainsKey("UpdatePeriod"))
                SetupUpdateTimer();
        }

        public PickedItemsSource()
        {
            _items.CollectionChanged += (s, e) =>
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Items"));
                }
            };
        }

        public static PickedItem GetPickedItem(int idx)
        {
            if ((idx >= 0) && (idx < _dataSource._items.Count))
                return _dataSource._items[idx];
            else
                return null;
        }

        public static async Task<IEnumerable<PickedItem>> GetItemsAsync(string filter = "new")
        {
            await _dataSource.ReadFileAsync();
            if (filter != null)
                return _dataSource._items.Where(item => item.Status == filter).OrderByDescending(item => item.Timestamp);
            else
                return _dataSource._items;
        }

        public async Task ReadFileAsync()
        {
            if (this._items.Count != 0)
                return;
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(XMLFILENAME);
                using (var inStream = await file.OpenSequentialReadAsync())
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<PickedItem>));
                    this._items = (ObservableCollection<PickedItem>)serializer.ReadObject(inStream.AsStreamForRead());
                }
            }
            catch
            {
                ;
            }
        }

        public static async Task SaveAsync()
        {
            var serializer = new DataContractSerializer(typeof(ObservableCollection<PickedItem>));
            MemoryStream sessionData = new MemoryStream();
            serializer.WriteObject(sessionData, _dataSource._items);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(XMLFILENAME, CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                sessionData.Seek(0, SeekOrigin.Begin);
                await sessionData.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            _dataSource.OnPropertyChanged("Items");
        }

        public static async Task<bool> AddItem(String _title, String _content, String _imagePath, DateTimeOffset _timestamp, String _url, String _feedName)
        {
            if (_dataSource._items.Where(itm => itm.Title == _title).ToList().Count > 0)
                return false;
            try
            {
                string filename = rssfeed.Data.Hash.GetStableHash(_title).ToString();
                HttpClient client = new HttpClient();
                var buffer = await client.GetBufferAsync(new Uri(_imagePath));
                MemoryStream readStream = new MemoryStream(buffer.ToArray());
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream.AsRandomAccessStream());
                var pixels = await decoder.GetPixelDataAsync();
                StorageFile savedPicture = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                IRandomAccessStream writeStream = await savedPicture.OpenAsync(FileAccessMode.ReadWrite);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, writeStream);
                encoder.SetPixelData(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore,
                            decoder.PixelWidth, decoder.PixelHeight,
                            decoder.DpiX, decoder.DpiY,
                            pixels.DetachPixelData());
                await encoder.FlushAsync();
                writeStream.Dispose();
                _imagePath = savedPicture.Path;
            }
            catch
            {
                _imagePath = string.Empty;
            }
            PickedItem item = new PickedItem(_title, _content, _imagePath, _timestamp, _url, _feedName);
            _dataSource._items.Add(item);
            return true;
        }

        public static async Task DeleteItem(PickedItem item)
        {
            if (item != null)
            {
                try
                {
                    string filename = Path.GetFileName(item.Thumbnail);
                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                    if (file != null)
                        await file.DeleteAsync();
                }
                catch { ;}
                _dataSource._items.Remove(item);
            }
        }

        public static async void PostItemToBlog(PickedItem item, PostProgressHandler Progress)
        {
            WordpressWrapper proxy = new WordpressWrapper();
            proxy.Url = (string)settings["BlogURL"];
            if (!proxy.Url.EndsWith("xmlrpc.php"))
                proxy.Url += "xmlrpc.php";

            bool error = false;
            XmlRpcStruct picture = new XmlRpcStruct();
            try
            {
                var filename = Path.GetFileName(item.Thumbnail);
                var file = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename);
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                picture.Add("bits", bytes);
                picture.Add("name", filename + ".jpg");
                picture.Add("type", "image/jpg");
            }
            catch
            {
                error = true;
            }
            if (error)
                PostContentToBlog(proxy, item, null, Progress);
            else
                PostPictureToBlog(proxy, item, picture, Progress);
            /*
            proxy.BeginGetRecentPosts(0, (string)settings["Username"], (string)settings["Password"], 1, async asr =>
            {
                Debug.WriteLine("q1");
                /*await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Debug.WriteLine("q2");
                    try
                    {
                        Debug.WriteLine("q3");
                        var result = proxy.EndGetRecentPosts(asr);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("q4");
                        // Handle exception here
                    }
                });

            });*/
            Debug.WriteLine("PostItem out");
        }

        private static void PostPictureToBlog(WordpressWrapper wp, PickedItem item, XmlRpcStruct picture, PostProgressHandler Progress)
        {
            wp.BeginNewMediaObject(0, (string)settings["Username"], (string)settings["Password"], picture, asr =>
            {
                XmlRpcStruct resultMediaObject;
                try
                {
                    resultMediaObject = wp.EndNewMediaObject(asr);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error mediaObject {0}", ex);
                    Progress(ex.Message);
                    return;
                }
                PostContentToBlog(wp, item, resultMediaObject["id"], Progress);
            }); // of BeginNewMediaObject
        }

        private static void PostContentToBlog(WordpressWrapper wp, PickedItem item, object ThumbnailID, PostProgressHandler Progress)
        {
            XmlRpcStruct post = new XmlRpcStruct();
            post.Add("dateCreated", item.Timestamp.DateTime);
            post.Add("description", item.Content + " <a href=\""+item.URL+"\">read more</a>");
            post.Add("title", item.Title);
            if (ThumbnailID != null)
                post.Add("wp_post_thumbnail", ThumbnailID);
            wp.BeginNewPost(0, (string)settings["Username"], (string)settings["Password"], post, true, async asr1 =>
            {
                string resultPost;
                try
                {
                    resultPost = wp.EndNewPost(asr1);
                }
                catch (Exception ex)
                {
                    Progress(ex.Message);
                    return;
                }
                Debug.WriteLine(resultPost);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    item.Status = "posted";
                    Progress();
                });
            }); // of BeginNewPost
        }

        public static void SetupUpdateTimer() {
            if (updateTimer.IsEnabled)
                updateTimer.Stop();
            updateTimer.Interval = TimeSpan.FromMinutes((double)settings["UpdatePeriod"]); //FromDays((double)settings["UpdatePeriod"]);
            updateTimer.Start();
        }

        public static void CleanDB(object sender, object e)
        {
            if (_dataSource._items.Count == 0)
                return;
            IEnumerable<PickedItem> toDelete = _dataSource._items.Where(item => DateTimeOffset.UtcNow.Subtract(item.Timestamp).Days > 31);
            bool needSave = toDelete.Count() > 0;
            foreach (PickedItem item in toDelete)
                _dataSource._items.Remove(item);
            if (needSave)
                lock (_dataSource._items)
                {
                    SaveAsync();
                }
        }

        public static async void ScanFeeds(object sender, object e)
        {
            if (scanInProgress)
                return;
            scanInProgress = true;
            StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;
            progressbar.ProgressValue = 0;
            progressbar.Text = "Scanning RSS feeds...";
            progressbar.ShowAsync(); 
            var Feeds = await FeedsListData.GetFeedsAsync();
            int numFeeds = ((ObservableCollection<FeedsListItem>)Feeds).Count;
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
            int numAdded = 0;
            string[] keywords = null;

            if (settings.ContainsKey("Keywords"))
                keywords = ((string)settings["Keywords"]).Split(',');
            int feedsPassed = -1;
            foreach (FeedsListItem feed in Feeds)
            {
                feedsPassed++;
                progressbar.ProgressValue = (double)feedsPassed / Feeds.Count();
                IEnumerable<DataGroup> posts;
                try
                {
                    posts = await DataSource.GetGroupsAsync(feed.URL);
                }
                catch
                {
                    continue;
                }
                foreach (DataGroup post in posts)
                {
                    int numFound = 0;
                    string title = post.Title.ToLower();
                    foreach (string keyword in keywords)
                    {
                        if (title.IndexOf(keyword.ToLower()) >= 0)
                            numFound++;
                    }
                    Debug.WriteLine("{0} {1}", new object[] { numFound, post.Title });
                    if (numFound == keywords.Count())
                    {
                        bool added = await AddItem(post.Title, post.Description, post.ImagePath, post.Published, post.Link, feed.Name);
                        if (added)
                            numAdded++;
                    }
                }
            } // of feeds cycle
            if (numAdded > 0)
                lock (_dataSource._items)
                {
                    SaveAsync();
                }
            progressbar.HideAsync();
            scanInProgress = false;
        }
    }
}