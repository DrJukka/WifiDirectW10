using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiDirectW10;
using WifiDirectW10.Engine;
using Windows.Devices.WiFiDirect;

namespace WifiDirectW10.Model
{
    public delegate void messageSentHandler(ConnectedDevice device, string message, string error);
    public delegate void gotMessageHandler(ConnectedDevice device, string message, string error);
    public delegate void deviceDisconnectedHandler(ConnectedDevice device);

    public class ConnectedDevice : INotifyPropertyChanged
    {
        private SocketReaderWriter socketRW;
        private WiFiDirectDevice wfdDevice;
        private string displayName = "";

        public ConnectedDevice(string displayName, WiFiDirectDevice wfdDevice, SocketReaderWriter socketRW)
        {
            this.socketRW = socketRW;
            if (this.socketRW != null)
            {
                socketRW.messageEventHandler += SocketRW_messageEventHandler;
            }
            this.wfdDevice = wfdDevice;
            if (this.wfdDevice != null) {
                this.wfdDevice.ConnectionStatusChanged += WfdDevice_ConnectionStatusChanged;
            }
            this.displayName = displayName;
        }

        private void WfdDevice_ConnectionStatusChanged(WiFiDirectDevice sender, object args)
        {
            if ((sender.ConnectionStatus == WiFiDirectConnectionStatus.Disconnected) && (deviceDisconnected != null))
            {
                deviceDisconnected(this);
            }
        }

        private void SocketRW_messageEventHandler(bool incoming, string message, string error)
        {
            if (incoming)
            {
                if (gotMessage != null)
                {
                    gotMessage(this,message, error);
                }

                return;
            }
            //outgoing
            if (messageSent != null)
            {
                messageSent(this, message, error);
            }
        }

        private ConnectedDevice() { }

        public SocketReaderWriter SocketRW
        {
            get
            {
                return socketRW;
            }

            set
            {
                if (socketRW != null)
                {
                    socketRW.messageEventHandler -= SocketRW_messageEventHandler;
                }
                socketRW = value;
                if (socketRW != null)
                {
                    socketRW.messageEventHandler += SocketRW_messageEventHandler;
                }
            }
        }

        public WiFiDirectDevice WfdDevice
        {
            get
            {
                return wfdDevice;
            }

            set
            {
                if (wfdDevice != null)
                {
                    wfdDevice.ConnectionStatusChanged -= WfdDevice_ConnectionStatusChanged;
                }
                wfdDevice = value;
                if (wfdDevice != null)
                {
                    wfdDevice.ConnectionStatusChanged += WfdDevice_ConnectionStatusChanged;
                }
            }
        }

        public override string ToString()
        {
            return displayName;
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
            }
        }

        public string Icon
        {
            get
            {
                return "Assets/StoreLogo.png";
            }
        }

        public string Name
        {
            get
            {
                return displayName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event deviceDisconnectedHandler deviceDisconnected;
        public event messageSentHandler messageSent;
        public event gotMessageHandler gotMessage;
    }
}