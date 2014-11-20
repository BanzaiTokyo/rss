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
using Windows.Storage;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace rssfeed
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private IPropertySet settings = ApplicationData.Current.LocalSettings.Values;

        public SettingsPage()
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
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

            if (settings.ContainsKey("BlogURL"))
                txtBlogURL.Text = (string)settings["BlogURL"];
            if (settings.ContainsKey("Username"))
                txtUsername.Text = (string)settings["Username"];
            if (settings.ContainsKey("Password"))
                txtPassword.Password = (string)settings["Password"];
            if (settings.ContainsKey("Keywords"))
                txtKeywords.Text = ((string)settings["Keywords"]).Replace(",", ", ");
            if (settings.ContainsKey("UpdatePeriod"))
                UpdatePeriod.Value = (double)settings["UpdatePeriod"];

            lblErrorMessage.Visibility = Visibility.Collapsed;
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
        }

        #endregion

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            string error = string.Empty;
            Uri uri = null;

            List<string> keywords = new List<string>();
            foreach (string keyword in txtKeywords.Text.Split(','))
            {
                string s = keyword.Trim();
                if (s.Length > 0)
                    keywords.Add(s);
            }
            if (keywords.Count == 0)
            {
                error = "You must specify some keywords in order to pick records from RSS feeds";
            }

            if (string.IsNullOrEmpty(txtBlogURL.Text) || !Uri.TryCreate(txtBlogURL.Text, UriKind.Absolute, out uri))
            {
                error = "Blog URL must be valid URL";
            }
            else if (string.IsNullOrEmpty(txtUsername.Text))
            {
                error = "Username cannot be empty";
            }

            if (!string.IsNullOrEmpty(error))
            {
                lblErrorMessage.Visibility = Visibility.Visible;
                lblErrorMessage.Text = error;
                return;
            }

            if (!txtBlogURL.Text.EndsWith("/"))
                txtBlogURL.Text += "/";
            settings["BlogURL"] = txtBlogURL.Text;
            settings["Username"] = txtUsername.Text;
            settings["Password"] = txtPassword.Password;
            settings["Keywords"] = String.Join(",", keywords);
            settings["UpdatePeriod"] = UpdatePeriod.Value;
            PickedItemsSource.SetupUpdateTimer();

            if (Frame.CanGoBack)
                Frame.GoBack();
            else
            {
                Frame.Navigate(typeof(PickedItemsPage));
                Frame.BackStack.Clear();
            }
        }

        private void UpdatePeriod_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (txtDays != null)
                txtDays.Text = string.Format("{0} minutes", e.NewValue);
        }
    }
}
