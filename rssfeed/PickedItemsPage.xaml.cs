using rssfeed.Common;
using rssfeed.Data;
using System;
using System.Collections.Generic;
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
            IList<PickedItem> selectedItems = new List<PickedItem>();
            foreach (PickedItem item in lvPickedItems.SelectedItems)
                selectedItems.Add(item);
            foreach (PickedItem item in selectedItems)
                await PickedItemsSource.DeleteItem(item);

            await PickedItemsSource.SaveAsync();
            IsEnabled = true;
        }

        private void btnPost_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            IList<PickedItem> selectedItems = new List<PickedItem>();
            foreach (PickedItem item in lvPickedItems.SelectedItems)
                selectedItems.Add(item); 

            for (int i=0; i < selectedItems.Count; i++)
               PickedItemsSource.PostItemToBlog(selectedItems[i], true, i == selectedItems.Count - 1 ? new MethodHandler(EndPosting) : null);
        }

        public async void EndPosting()
        {
            //Calling the Show method of MessageDialog class  
            //which will show the MessageBox  
            lvPickedItems.SelectedItems.Clear();
            await PickedItemsSource.SaveAsync();
            IsEnabled = true;
            MessageDialog msgbox = new MessageDialog("Posting finished");
            await msgbox.ShowAsync();
        }

        private void GoToFeedsList(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(FeedsListPage));
        }

        private void GoToWPSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WPSettingsPage));
        }
    }
}
