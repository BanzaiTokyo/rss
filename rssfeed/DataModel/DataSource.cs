using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Syndication;

namespace rssfeed.Data
{
    /// <summary>
    /// group data model.
    /// </summary>
    public class DataGroup
    {
        public DataGroup(String uniqueId, String title, String link, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Link = link;
            this.Description = description;
            this.ImagePath = imagePath;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Link { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public sealed class DataSource
    {
        private static DataSource _dataSource = new DataSource();

        private ObservableCollection<DataGroup> _groups = new ObservableCollection<DataGroup>();
        public ObservableCollection<DataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<DataGroup>> GetGroupsAsync()
        {
            await _dataSource.GetSampleDataAsync();

            return _dataSource.Groups;
        }

        public static async Task<DataGroup> GetGroupAsync(string uniqueId)
        {
            await _dataSource.GetSampleDataAsync();
            var matches = _dataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }


        public string Strip(string text)
        {
            text = text.Replace("&#160;", string.Empty);
            return Regex.Replace(text, @"<(.|\n)*?>", string.Empty);
        }



        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;


            SyndicationClient client = new SyndicationClient();
            Uri feedUri = new Uri("http://feeds.feedburner.com/TechCrunch/");
            var feed = await client.RetrieveFeedAsync(feedUri);

            foreach (SyndicationItem item in feed.Items)
            {
                string data = string.Empty;
                if (feed.SourceFormat == SyndicationFormat.Atom10)
                {
                    data = item.Content.Text;
                }
                else if (feed.SourceFormat == SyndicationFormat.Rss20)
                {
                    data = item.Summary.Text;
                }

                Regex regx = new Regex("http://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?.(?:jpg|bmp|gif|png)", RegexOptions.IgnoreCase);
                string filePath = regx.Match(data).Value;
                data = Strip(data);

                //if (item.Id != null && item.Title.Text != null && item.Links[0].Uri !=null)
                //{
                DataGroup group = new DataGroup(item.Id,
                                                item.Title.Text,
                                                item.Links[0].Uri.ToString(),
                                                filePath.Replace("small", "large"),
                                                data);//).Split(new string[] { "/&gt;" }, StringSplitOptions.None)[1].ToString());

                this.Groups.Add(group);
                //}




            }
        }
    }
}