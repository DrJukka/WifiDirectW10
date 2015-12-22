using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WifiDirectW10.Engine;
using WifiDirectW10.Model;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
namespace WifiDirectW10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DeviceWatcher _deviceWatcher;
        ReaderWriterLockSlim _discoveryRWLock;
        bool _fWatcherStarted;

        CancellationTokenSource _cancellationTokenSource;

        bool _WDAdvertiserStarted;
        private WiFiDirectAdvertisementPublisher _publisher;
        private WiFiDirectConnectionListener _listener;

        private Task discoveredDevice;

        public ObservableCollection<DiscoveredDevice> _discoveredDevices
        {
            get;
            private set;
        }

        public MainPage()
        {
            this.InitializeComponent();
            _discoveryRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _fWatcherStarted = false;
            _discoveredDevices = new ObservableCollection<DiscoveredDevice>();
          
            lvDiscoveredDevices.ItemsSource = _discoveredDevices;
            lvDiscoveredDevices.SelectionMode = ListViewSelectionMode.Single;
        }

        private void btnWatcher_Click(object sender, RoutedEventArgs e)
        {
            if (_fWatcherStarted == false)
            {
                SearchProgress.Visibility = Visibility.Visible;
                btnWatcher.Content = "Stop Watcher";
                _fWatcherStarted = true;
                _discoveredDevices.Clear();
                _deviceWatcher = null;

                String deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.AssociationEndpoint);
                System.Diagnostics.Debug.WriteLine("deviceSelector : " + deviceSelector);

                // Get all WiFiDirect devices that are advertising and in range
               /* DeviceInformationCollection devInfoCollection = await DeviceInformation.FindAllAsync(deviceSelector);
                System.Diagnostics.Debug.WriteLine("devInfoCollection : ");

                _discoveryRWLock.EnterWriteLock();
                
                foreach (DeviceInformation info in devInfoCollection) {
                    System.Diagnostics.Debug.WriteLine("Found : " + info.Name);
                    var deviceInfoDisplay = new DiscoveredDevice(info);
                    _discoveredDevices.Add(deviceInfoDisplay);
                }
                _discoveryRWLock.ExitWriteLock();*/

                //String deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.AssociationEndpoint);
                //WiFiDirectDeviceSelectorType.DeviceInterface : WiFiDirectDeviceSelectorType.);
                
                System.Diagnostics.Debug.WriteLine("deviceSelector : " + deviceSelector);
                _deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);

                _deviceWatcher.Added += _deviceWatcher_Added;
                _deviceWatcher.Removed += _deviceWatcher_Removed;
                _deviceWatcher.Updated += _deviceWatcher_Updated;
                _deviceWatcher.EnumerationCompleted += _deviceWatcher_EnumerationCompleted;
                _deviceWatcher.Stopped += _deviceWatcher_Stopped;

                _deviceWatcher.Start();
            }
            else
            {
                btnWatcher.Content = "Start Watcher";
                _fWatcherStarted = false;

                _deviceWatcher.Added -= _deviceWatcher_Added;
                _deviceWatcher.Removed -= _deviceWatcher_Removed;
                _deviceWatcher.Updated -= _deviceWatcher_Updated;
                _deviceWatcher.EnumerationCompleted -= _deviceWatcher_EnumerationCompleted;
                _deviceWatcher.Stopped -= _deviceWatcher_Stopped;

                _deviceWatcher.Stop();
            }
        }

        private async void _deviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _discoveryRWLock.EnterWriteLock();

                var deviceInfoDisplay = new DiscoveredDevice(args);
                _discoveredDevices.Add(deviceInfoDisplay);

                _discoveryRWLock.ExitWriteLock();
            });
        }

        private async void _deviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _discoveryRWLock.EnterWriteLock();

                foreach (DiscoveredDevice devInfodisplay in _discoveredDevices)
                {
                    if (devInfodisplay.DeviceInfo.Id == args.Id)
                    {
                        _discoveredDevices.Remove(devInfodisplay);
                        break;
                    }
                }

                _discoveryRWLock.ExitWriteLock();
            });
        }

        private async void _deviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _discoveryRWLock.EnterWriteLock();

                for (int idx = 0; idx < _discoveredDevices.Count; idx++)
                {
                    DiscoveredDevice devInfodisplay = _discoveredDevices[idx];
                    if (devInfodisplay.DeviceInfo.Id == args.Id)
                    {
                        devInfodisplay.DeviceInfo.Update(args);
                        break;
                    }
                }
            });
        }

        private async void _deviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SearchProgress.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine("_deviceWatcher_Stopped : ");
            });
        }

        private async void _deviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SearchProgress.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine("_deviceWatcher_EnumerationCompleted : ");
            });
        }

        private async void lvDiscoveredDevices_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                DiscoveredDevice selItem = (DiscoveredDevice)e.ClickedItem;
                System.Diagnostics.Debug.WriteLine("Item clicked");

                WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
                connectionParams.GroupOwnerIntent = Convert.ToInt16("1");
               // connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;

                System.Diagnostics.Debug.WriteLine("connectionParams");

                _cancellationTokenSource = new CancellationTokenSource();
                System.Diagnostics.Debug.WriteLine("CancellationTokenSource");

                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(selItem.DeviceInfo.Id, connectionParams).AsTask(_cancellationTokenSource.Token);
                System.Diagnostics.Debug.WriteLine("Item WiFiDirectDevice.FromIdAsync");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("FromIdAsync was canceled by user : ");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Connect operation threw an exception: " + ex.Message);
            }

            _cancellationTokenSource = null;
        }

        private void btnAdvertiser_Click(object sender, RoutedEventArgs e)
        {
            if (_publisher != null)
            {
                _listener.ConnectionRequested -= _listener_ConnectionRequested;
                _publisher.Stop();
                _publisher = null;
                _listener = null;
            }

            if (_WDAdvertiserStarted == false)
            {
                btnAdvertiser.Content = "Stop Advertiser";
                _WDAdvertiserStarted = true;

                _publisher = new WiFiDirectAdvertisementPublisher();
                _publisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Intensive;

                _listener = new WiFiDirectConnectionListener();
                _listener.ConnectionRequested += _listener_ConnectionRequested;

                _publisher.Start();
            }
            else
            {
                btnAdvertiser.Content = "Start Advertiser";
                _WDAdvertiserStarted = false;
            }

        }

        private async void _listener_ConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs args)
        {

            WiFiDirectConnectionRequest ConnectionRequest = args.GetConnectionRequest();

            DeviceInformation devInfo = ConnectionRequest.DeviceInformation;

            System.Diagnostics.Debug.WriteLine("Name : " + devInfo.Name + ", Id: " + devInfo.Id);

            foreach (String key in devInfo.Properties.Keys)
            {
                System.Diagnostics.Debug.WriteLine("key : " + key);
                Object tmpObject = null;
                System.Diagnostics.Debug.WriteLine("try-get : " + devInfo.Properties.TryGetValue(key, out tmpObject));
                if (tmpObject != null)
                {
                    System.Diagnostics.Debug.WriteLine("Object type " + tmpObject.GetType().ToString());
                }
            }

            var tcsWiFiDirectDevice = new TaskCompletionSource<WiFiDirectDevice>();
            var wfdDeviceTask = tcsWiFiDirectDevice.Task;

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Connecting to " + ConnectionRequest.DeviceInformation.Name + "...");

                    WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
                    connectionParams.GroupOwnerIntent = Convert.ToInt16("9");

                    // IMPORTANT: FromIdAsync needs to be called from the UI thread
                    tcsWiFiDirectDevice.SetResult(await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id, connectionParams));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FromIdAsync task threw an exception: " + ex.ToString());
                }
            });

            WiFiDirectDevice wfdDevice = await wfdDeviceTask;

            // Prompt the user to accept/reject the connection request
            // If rejected, exit

     /*       // Connect to the remote device
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
            connectionParams.GroupOwnerIntent = Convert.ToInt16("1");

            WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id, connectionParams);

            //WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id);

            // Get the local and remote IP addresses
            var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();
            */

            // Establish standard WinRT socket with above IP addresses
        }
    }
}
