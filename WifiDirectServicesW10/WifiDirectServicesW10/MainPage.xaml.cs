using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using WifiDirectServicesW10.Engine;
using WifiDirectServicesW10.Model;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WifiDirectServicesW10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WDServiceAdvertiser _advertiser;
        bool _WDAdvertiserStarted;
        ReaderWriterLockSlim _discoveryRWLock;
        public ObservableCollection<DiscoveredDevice> _discoveredDevices
        {
            get;
            private set;
        }

        public MainPage()
        {
            this.InitializeComponent();
            _discoveryRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

            _discoveredDevices = new ObservableCollection<DiscoveredDevice>();

            lvDiscoveredDevices.ItemsSource = _discoveredDevices;
            lvDiscoveredDevices.SelectionMode = ListViewSelectionMode.Single;
        }

        private async void btnWatcher_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    string serviceSelector = WiFiDirectService.GetSelector("drjukka_wifiservice");

                    System.Diagnostics.Debug.WriteLine("deviceSelector : " + serviceSelector);

                    List<string> additionalProperties = new List<string>();
                    additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceAddress");
                    additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceName");
                    additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceInformation");
                    additionalProperties.Add("System.Devices.WiFiDirectServices.AdvertisementId");
                    additionalProperties.Add("System.Devices.WiFiDirectServices.ServiceConfigMethods");

                    // Get all WiFiDirect services that are advertising and in range
                    DeviceInformationCollection deviceInfoCollection = await DeviceInformation.FindAllAsync(serviceSelector, additionalProperties);

                    _discoveryRWLock.EnterWriteLock();

                    foreach (DeviceInformation info in deviceInfoCollection)
                    {
                        System.Diagnostics.Debug.WriteLine("Found : " + info.Name);
                        var deviceInfoDisplay = new DiscoveredDevice(info);
                        _discoveredDevices.Add(deviceInfoDisplay);
                    }
                    _discoveryRWLock.ExitWriteLock();

                    System.Diagnostics.Debug.WriteLine("done -- ");

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Failed to discover services: {0}", ex.Message));
                    //throw ex;
                }
            });
        }

        private async void btnAdvertiser_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,  () =>
            {
                if (_advertiser != null)
                {
                    _advertiser.Dispose();
                    _advertiser = null;
                }

                if (_WDAdvertiserStarted == false)
                {
                    btnAdvertiser.Content = "Stop Advertiser";
                    _WDAdvertiserStarted = true;

                    _advertiser = new WDServiceAdvertiser();
                }
                else
                {
                    btnAdvertiser.Content = "Start Advertiser";
                    _WDAdvertiserStarted = false;
                }
            });
        }

        private async void lvDiscoveredDevices_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                DiscoveredDevice selItem = (DiscoveredDevice)e.ClickedItem;
                System.Diagnostics.Debug.WriteLine("Item clicked");

                // Get a Service Seeker object
                WiFiDirectService Service = await WiFiDirectService.FromIdAsync(selItem.DeviceInfo.Id);
                System.Diagnostics.Debug.WriteLine("WiFiDirectService");

                // Connect to the Advertiser
                WiFiDirectServiceSession Session = await Service.ConnectAsync();
                System.Diagnostics.Debug.WriteLine("WiFiDirectServiceSession");

                // Get the local and remote IP addresses
                var EndpointPairs = Session.GetConnectionEndpointPairs();
                System.Diagnostics.Debug.WriteLine("EndpointPairs");
            });
        }
    }
}
