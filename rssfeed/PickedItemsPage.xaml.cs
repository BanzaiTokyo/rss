using rssfeed.Common;
using rssfeed.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Storage;
using System.Diagnostics;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace rssfeed
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PickedItemsPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        IList<PickedItem> selectedItems = new List<PickedItem>();
        List<string> postErrors = new List<string>();
        bool wasSuccessPost;

        public PickedItemsPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var sampleDataGroups = await PickedItemsSource.GetItemsAsync();
            this.DefaultViewModel["Items"] = sampleDataGroups;
            PickedItemsSource.viewer = lvPickedItems;
            lvPickedItems_SelectionChanged(null, null);
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Frame.BackStackDepth > 1)
                Frame.BackStack.RemoveAt(0);
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void lvPickedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lblNumberSelected.Text = string.Format("{0} of {1} selected", lvPickedItems.SelectedItems.Count, lvPickedItems.Items.Count);
            btnPost.IsEnabled = lvPickedItems.SelectedItems.Count > 0;
            btnRemove.IsEnabled = btnPost.IsEnabled;
        }

        private async void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            selectedItems.Clear();
            foreach (PickedItem item in lvPickedItems.SelectedItems)
                selectedItems.Add(item);
            foreach (PickedItem item in selectedItems)
                await PickedItemsSource.DeleteItem(item);
                //item.Status = "removed";

            await PickedItemsSource.SaveAsync();
            IsEnabled = true;
        }

        private void btnPost_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog msgbox;
            if (PickedItemsSource.scanInProgress)
            {
                msgbox = new MessageDialog("Feeds scan is in progress. Please, try again in a minute or two");
                msgbox.ShowAsync();
                return;
            }
            EnableButtons(false);
            selectedItems.Clear();
            postErrors.Clear();
            foreach (PickedItem item in lvPickedItems.SelectedItems)
                selectedItems.Add(item);
            pgsProgress.Maximum = selectedItems.Count;
            pgsProgress.Value = 0;
            pgsText.Text = "Posting...";
            msgProgress.IsOpen = true;
            PickedItemsSource.scanInProgress = true;
            for (int i = 0; i < selectedItems.Count; i++) {
                if (!msgProgress.IsOpen)
                    break;
               PickedItemsSource.PostItemToBlog(selectedItems[i], new PostProgressHandler(PostingProgress));
            }
        }

        public async void PostingProgress(string error = null)
        {
            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (error != null)
                    postErrors.Add(selectedItems[(int)pgsProgress.Value] + ":\n" + error);
                else
                    wasSuccessPost = true;
                pgsProgress.Value = pgsProgress.Value + 1;
                pgsText.Text = String.Format("Processed {0} of {1} posts", pgsProgress.Value, selectedItems.Count);
                if (pgsProgress.Value == selectedItems.Count || !msgProgress.IsOpen)
                    EndPosting();
            });
        }

        public async void EndPosting()
        {
            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                PickedItemsSource.scanInProgress = false;
                msgProgress.IsOpen = false;
                if (wasSuccessPost)
                {
                    await PickedItemsSource.SaveAsync();
                }
                lvPickedItems.SelectedItems.Clear();
                EnableButtons(true);
                MessageDialog msgbox;
                string title = "Posting finished";
                if (postErrors.Count > 0)
                {
                    string msg = "There was an error during the operation: " + String.Join(":\n", postErrors);
                    msgbox = new MessageDialog(msg, title);
                }
                else
                    msgbox = new MessageDialog(title);
                await msgbox.ShowAsync();
            });
        }

        private void GoToFeedsList(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(FeedsListPage));
        }

        private void GoToWPSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void btnScanFeeds_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog msgbox;
            if (PickedItemsSource.scanInProgress)
            {
                msgbox = new MessageDialog("Feeds scan is in progress. Please, try again in a minute or two");
                await msgbox.ShowAsync();
                return;
            }
            PickedItemsSource.scanInProgress = true;
            var Feeds = await FeedsListData.GetFeedsAsync();
            int numFeeds = ((ObservableCollection<FeedsListItem>)Feeds).Count;
            IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
            int i, numAdded = 0;
            string[] keywords = null;
            if (settings.ContainsKey("Keywords"))
                keywords = ((string)settings["Keywords"]).Split(',');

            EnableButtons(false);
            i = 0;
            msgProgress.IsOpen = true;
            pgsProgress.Maximum = numFeeds - 1;
            foreach(FeedsListItem feed in Feeds) 
            {
                if (!msgProgress.IsOpen)
                    break;
                pgsText.Text = string.Format("Read {0} of {1} feeds", i, numFeeds);
                pgsProgress.Value = i;
                IEnumerable<DataGroup> posts;
                try
                {
                    posts = await DataSource.GetGroupsAsync(feed.URL);
                }
                catch
                {
                    continue;
                }
                foreach (DataGroup post in posts) {
                    int numFound = 0;
                    string title = post.Title.ToLower();
                    foreach (string keyword in keywords)
                    {
                        if (title.IndexOf(keyword.ToLower()) >= 0)
                            numFound++;
                    }
                    Debug.WriteLine("{0} {1}", new object[] {numFound, post.Title});
                    if (numFound == keywords.Count())
                    {
                        bool added = await PickedItemsSource.AddItem(post.Title, post.Description, post.ImagePath, post.Published, post.Link, feed.Name);
                        if (added)
                            numAdded++;
                    }
                }
                i++;
            } // of feeds cycle
            if (numAdded > 0)
            {
                await PickedItemsSource.SaveAsync();
            }
            EnableButtons(true);
            PickedItemsSource.scanInProgress = false;
            msgProgress.IsOpen = false;
            msgbox = new MessageDialog(string.Format("Scan finished, {0} posts added to preview", numAdded));
            await msgbox.ShowAsync();
        }

        private void StopScan(object sender, RoutedEventArgs e)
        {
            msgProgress.IsOpen = false;
        }

        private void EnableButtons(bool enabled)
        {
            btnRemove.IsEnabled = btnPost.IsEnabled = btnFeedsList.IsEnabled = btnScanFeeds.IsEnabled = btnSettings.IsEnabled = enabled;
            if (enabled)
                lvPickedItems_SelectionChanged(null, null);
        }
    }
}