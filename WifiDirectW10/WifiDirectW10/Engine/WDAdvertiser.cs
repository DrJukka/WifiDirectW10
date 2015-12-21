using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Devices.WiFiDirect.Services;
using Windows.UI.Core;

namespace WifiDirectW10.Engine
{
    public class WDAdvertiser : IDisposable
    {
        private WiFiDirectAdvertisementPublisher _publisher;
        private WiFiDirectConnectionListener _listener;
        public WDAdvertiser()
        {
            _publisher = new WiFiDirectAdvertisementPublisher();
            _publisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Intensive;

            _listener = new WiFiDirectConnectionListener();
            _listener.ConnectionRequested += _listener_ConnectionRequested;

            _publisher.Start();
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

                // Prompt the user to accept/reject the connection request
                // If rejected, exit

                // Connect to the remote device
                WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id);

                // Get the local and remote IP addresses
                var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

            
            // Establish standard WinRT socket with above IP addresses
        }

        public void Dispose()
        {
            _listener.ConnectionRequested -= _listener_ConnectionRequested;
            _publisher.Stop();
        }
    }
}
