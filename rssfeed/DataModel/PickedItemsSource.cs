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
using System.Diagnostics;

namespace rssfeed.Data
{
    public delegate void MethodHandler(); // The type

    [DataContract]
    class PickedItem
    {
        [DataMember]
        public string Title {get; set;}
        [DataMember]
        public string Content {get; set;}
        [DataMember]
        public string Thumbnail {get; set;}

        public PickedItem(String _title, String _content, String _thumbnail)
        {
            this.Title = _title;
            this.Content = _content;
            this.Thumbnail = _thumbnail;
        }
        public override string ToString()
        {
            return this.Title;
        }
    }

    class PickedItemsSource : INotifyPropertyChanged
    {
        private const string XMLFILENAME = "PickedItems.xml";
        private static PickedItemsSource _dataSource = new PickedItemsSource();
        private ObservableCollection<PickedItem> _items = new ObservableCollection<PickedItem>();
        public ObservableCollection<PickedItem> Items
        {
            get { return this._items; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void PropChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        
        public static PickedItem GetPickedItem(int idx)
        {
            if ((idx >= 0) && (idx < _dataSource.Items.Count))
                return _dataSource.Items[idx];
            else
                return null;
        }

        public static async Task<IEnumerable<PickedItem>> GetItemsAsync()
        {
            await _dataSource.ReadFileAsync();
            return _dataSource.Items;
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
            serializer.WriteObject(sessionData, _dataSource.Items);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(XMLFILENAME, CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                sessionData.Seek(0, SeekOrigin.Begin);
                await sessionData.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            _dataSource.PropChanged("Items");
        }

        public static async Task AddItem(String _title, String _content, String _imagePath)
        {
            if (_dataSource.Items.Where(itm => itm.Title == _title).ToList().Count > 0)
                return;

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
            PickedItem item = new PickedItem(_title, _content, savedPicture.Path);
            _dataSource.Items.Add(item);
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

        public static async void PostItemToBlog(PickedItem item, bool DeleteAfterPost, MethodHandler Finalize = null)
        {
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;

            WordpressWrapper proxy = new WordpressWrapper();
            proxy.Url = (string)settings["BlogURL"];
            var filename = Path.GetFileName(item.Thumbnail);
            var file = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename);
            byte[] bytes = new byte[file.Length];
            file.Read(bytes, 0, (int)file.Length);
            XmlRpcStruct picture = new XmlRpcStruct();
            picture.Add("bits", bytes);
            picture.Add("name", filename + ".jpg");
            picture.Add("type", "image/jpg");
            proxy.BeginNewMediaObject(0, (string)settings["Username"], (string)settings["Password"], picture, asr =>
            {
                XmlRpcStruct resultMediaObject;
                try
                {
                    resultMediaObject = proxy.EndNewMediaObject(asr);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error mediaObject {0}", ex);
                    if (Finalize != null)
                    {
                        Debug.WriteLine("Finalizing");
                        Finalize();
                    }
                    return;
                }
                XmlRpcStruct post = new XmlRpcStruct();
                post.Add("dateCreated", DateTime.Now);
                post.Add("description", item.Content);
                post.Add("title", item.Title);
                post.Add("wp_post_thumbnail", resultMediaObject["id"]);
                proxy.BeginNewPost(0, (string)settings["Username"], (string)settings["Password"], post, true, async asr1 =>
                {
                    string resultPost;
                    try
                    {
                        resultPost = proxy.EndNewPost(asr1);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error newPost {0}", ex);
                        if (Finalize != null)
                        {
                            Debug.WriteLine("Finalizing");
                            Finalize();
                        }
                        return;
                    }
                    Debug.WriteLine(resultPost);
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (DeleteAfterPost)
                        {
                            Debug.WriteLine("Deleting {0}", item.Title);
                                await DeleteItem(item);
                        }
                        if (Finalize != null)
                        {
                            Debug.WriteLine("Finalizing");
                            Finalize();
                        }
                    });
                }); // of BeginNewPost
            }); // of BeginNewMediaObject
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
    }
}
