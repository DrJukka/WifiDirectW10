using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WifiDirectW10.Engine;
using WifiDirectW10.Model;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking.Sockets;
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

        public ObservableCollection<DiscoveredDevice> _discoveredDevices
        {
            get;
            private set;
        }

        StreamSocketListener _listenerSocket;
        public ObservableCollection<ConnectedDevice> _connectedDevices
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

            _connectedDevices = new ObservableCollection<ConnectedDevice>();
            _listenerSocket = null;

            lvConnectedDevices.ItemsSource = _connectedDevices;
            lvConnectedDevices.SelectionMode = ListViewSelectionMode.Single;
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
                WiFiDirectDevice ConnDevice = await WiFiDirectDevice.FromIdAsync(selItem.DeviceInfo.Id, connectionParams).AsTask(_cancellationTokenSource.Token);
                System.Diagnostics.Debug.WriteLine("Item WiFiDirectDevice.FromIdAsync");

                // Register for the ConnectionStatusChanged event handler
                ConnDevice.ConnectionStatusChanged += ConnDevice_ConnectionStatusChanged;

                var endpointPairs = ConnDevice.GetConnectionEndpointPairs();

                System.Diagnostics.Debug.WriteLine("Devices connected on L2 layer, connecting to IP Address: " + endpointPairs[0].RemoteHostName + " Port: " + Globals.strServerPort);
                // Wait for server to start listening on a socket
                await Task.Delay(2000);

                // Connect to Advertiser on L4 layer
                StreamSocket clientSocket = new StreamSocket();
                await clientSocket.ConnectAsync(endpointPairs[0].RemoteHostName, Globals.strServerPort);

                SocketReaderWriter socketRW = new SocketReaderWriter(clientSocket, null);

                string sessionId = "Session: " + Path.GetRandomFileName();
                ConnectedDevice connectedDevice = new ConnectedDevice(sessionId, ConnDevice, socketRW);
                _connectedDevices.Add(connectedDevice);

                socketRW.ReadMessage();
                socketRW.WriteMessage(sessionId);

                System.Diagnostics.Debug.WriteLine("Connected with remote side on L4 layer");
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

        private void ConnDevice_ConnectionStatusChanged(WiFiDirectDevice sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("Connection out status changed: " + sender.ConnectionStatus);
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
            try
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
                System.Diagnostics.Debug.WriteLine("Connection status : " + wfdDevice.ConnectionStatus);
                wfdDevice.ConnectionStatusChanged += WfdDevice_ConnectionStatusChanged;

                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    ConnectedDevice connectedDevice = new ConnectedDevice("Waiting for client to connect...", wfdDevice, null);
                    _connectedDevices.Add(connectedDevice);
                });

                var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

                _listenerSocket = null;
                _listenerSocket = new StreamSocketListener();
                _listenerSocket.ConnectionReceived += _listenerSocket_ConnectionReceived; ;
                await _listenerSocket.BindEndpointAsync(EndpointPairs[0].LocalHostName, Globals.strServerPort);

                System.Diagnostics.Debug.WriteLine("Devices connected on L2, listening on IP Address: " + EndpointPairs[0].LocalHostName.ToString() + " Port: " + Globals.strServerPort);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Connect operation threw an exception: " + ex.Message);
            }
        }

        private async void _listenerSocket_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Connecting to remote side on L4 layer...");
            StreamSocket serverSocket = args.Socket;
            try
            {
                SocketReaderWriter socketRW = new SocketReaderWriter(serverSocket, null);
                socketRW.ReadMessage();

                while (true)
                {
                    string sessionId = socketRW.GetCurrentMessage();
                    if (sessionId != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Connected with remote side on L4 layer");

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            for (int idx = 0; idx < _connectedDevices.Count; idx++)
                            {
                                if (_connectedDevices[idx].DisplayName.Equals("Waiting for client to connect...") == true)
                                {
                                    ConnectedDevice connectedDevice = _connectedDevices[idx];
                                    _connectedDevices.RemoveAt(idx);

                                    connectedDevice.DisplayName = sessionId;
                                    connectedDevice.SocketRW = socketRW;

                                    _connectedDevices.Add(connectedDevice);
                                    break;
                                }
                            }
                        });

                        break;
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Connection failed: " + ex.Message);
            }
        }

        private void WfdDevice_ConnectionStatusChanged(WiFiDirectDevice sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("Connection in status changed: " + sender.ConnectionStatus);
        }

        private void lvConnectedDevices_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
