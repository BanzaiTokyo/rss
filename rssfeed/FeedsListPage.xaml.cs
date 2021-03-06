﻿using rssfeed.Common;
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
using System.Diagnostics;
using System.Collections.ObjectModel;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace rssfeed
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedsListPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public FeedsListPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            this.navigationHelper.LoadState += navigationHelper_LoadState;
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
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var feeds = await FeedsListData.GetFeedsAsync();
            this.DefaultViewModel["Items"] = feeds;
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
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            lvFeeds.UpdateLayout();
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddFeedPage));
        }

        private void lvFeeds_ItemClick(object sender, ItemClickEventArgs e)
        {
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(lvFeeds);
            int idx = ((ObservableCollection<FeedsListItem>)this.defaultViewModel["Items"]).IndexOf((FeedsListItem)e.ClickedItem);
            lvFeeds.SelectedIndex = idx;
            flyoutBase.ShowAt((FrameworkElement)lvFeeds.ContainerFromIndex(idx));
        }

        private async void ViewFeedButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource.Clear();
            bool error = false;
            IsEnabled = false;
            IEnumerable<DataGroup> feedContent = null;
            try
            {
                feedContent = await DataSource.GetGroupsAsync(((FeedsListItem)lvFeeds.SelectedItem).URL);
            }
            catch
            {
                error = true;
            }
            IsEnabled = true;
            if (error)
            {
                MessageDialog msgbox = new MessageDialog("Error Reading feed");
                await msgbox.ShowAsync();
            }
            else if (feedContent.Count() == 0)
            {
                MessageDialog msgbox = new MessageDialog("There are no new records");
                await msgbox.ShowAsync();
            }
            else
            {
                int idx = lvFeeds.SelectedIndex;
                Frame.Navigate(typeof(SingleFeedPage), lvFeeds.SelectedItem);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = lvFeeds.SelectedIndex;
            Frame.Navigate(typeof(AddFeedPage), idx);
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await FeedsListData.DeleteFeed(lvFeeds.SelectedIndex);
        }

    }
}
