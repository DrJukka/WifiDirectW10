using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WifiDirectServicesW10.Model;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WifiDirectServicesW10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TryConnectPage : Page
    {
        public static TryConnectPage Current;

        private WiFiAdapter _firstAdapter;
        public ObservableCollection<WiFiNetworkDisplay> _resultCollection
        {
            get;
            private set;
        }
        public TryConnectPage()
        {
            this.InitializeComponent();
            Current = this;
            _resultCollection = new ObservableCollection<WiFiNetworkDisplay>();
        }

        private async void scanButton_Click(object sender, RoutedEventArgs e)
        {
            if(_firstAdapter == null)
            {
                _firstAdapter = await prepareFirstAdapter();
            }

            if (_firstAdapter != null)
            {
                await _firstAdapter.ScanAsync();
                DisplayNetworkReport(_firstAdapter.NetworkReport);
            }
        }

        private async Task<WiFiAdapter> prepareFirstAdapter()
        {
            WiFiAdapter ret = null;
            // RequestAccessAsync must have been called at least once by the app before using the API
            // Calling it multiple times is fine but not necessary
            // RequestAccessAsync must be called from the UI thread
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                ShowErrorDialog("WiFiAdapter.RequestAccessAsync returned not allowed.", "Access denied");
            }
            else
            {
                DataContext = this;

                var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (result.Count >= 1)
                {
                    System.Diagnostics.Debug.WriteLine("Found " + result.Count + " wifi interfaces ");
                    ret = await WiFiAdapter.FromIdAsync(result[0].Id);
                }
                else
                {
                    ShowErrorDialog("No WiFi Adapters detected on this machine", "Missing HW");
                }
            }

            return ret;
        }

        private async void DisplayNetworkReport(WiFiNetworkReport report)
        {
            _resultCollection.Clear();

            System.Diagnostics.Debug.WriteLine("Add " + report.AvailableNetworks.Count + " wifi APs");
            foreach (var network in report.AvailableNetworks)
            {
                var networkDisplay = new WiFiNetworkDisplay(network, _firstAdapter);
                await networkDisplay.UpdateConnectivityLevel();
                _resultCollection.Add(networkDisplay);
            }
        }

        private async void ShowErrorDialog(string message, string title)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(message, title);
                await dialog.ShowAsync();
            });
        }
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedNetwork = ResultsListView.SelectedItem as WiFiNetworkDisplay;
            if (selectedNetwork == null || _firstAdapter == null)
            {
                ShowErrorDialog("Please select network to connect to.", "Network not selected");
                return;
            }

            WiFiReconnectionKind reconnectionKind = WiFiReconnectionKind.Manual;
            WiFiConnectionResult result;
            if (selectedNetwork.AvailableNetwork.SecuritySettings.NetworkAuthenticationType == Windows.Networking.Connectivity.NetworkAuthenticationType.Open80211)
            {
                System.Diagnostics.Debug.WriteLine("Connecting without password");
                result = await _firstAdapter.ConnectAsync(selectedNetwork.AvailableNetwork, reconnectionKind);
            }
            else
            {
                // Only the password portion of the credential need to be supplied
                var credential = new PasswordCredential();

                // Make sure Credential.Password property is not set to an empty string. 
                // Otherwise, a System.ArgumentException will be thrown.
                // The default empty password string will still be passed to the ConnectAsync method,
                // which should return an "InvalidCredential" error
                if (!string.IsNullOrEmpty(NetworkKey.Text))
                {
                    credential.Password = NetworkKey.Text;
                }

                if (!string.IsNullOrEmpty(ssdiBox.Text))
                {
                    System.Diagnostics.Debug.WriteLine("Connecting with password & SSID ");
                    result = await _firstAdapter.ConnectAsync(selectedNetwork.AvailableNetwork, reconnectionKind, credential, ssdiBox.Text);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Connecting with password");
                    result = await _firstAdapter.ConnectAsync(selectedNetwork.AvailableNetwork, reconnectionKind, credential);
                }
            }

            if (result.ConnectionStatus == WiFiConnectionStatus.Success)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Successfully connected to {0}.", selectedNetwork.Ssid));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Could not connect to {0}. Error: {1}", selectedNetwork.Ssid, result.ConnectionStatus));
            }
        }

        private void ResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            WiFiNetworkDisplay selItem = (WiFiNetworkDisplay)e.ClickedItem;

            WiFiAvailableNetwork anw = selItem.AvailableNetwork;

            System.Diagnostics.Debug.WriteLine("is WifiDirect" + anw.IsWiFiDirect + ", netowrk kind : " + anw.NetworkKind);

            //NetworkAuthenticationType autType = anw.SecuritySettings.NetworkAuthenticationType;
            //NetworkEncryptionType encType = anw.SecuritySettings.NetworkEncryptionType;

        }
    }
}
