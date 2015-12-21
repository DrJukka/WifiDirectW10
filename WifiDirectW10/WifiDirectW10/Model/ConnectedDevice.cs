using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiDirectW10;
using Windows.Devices.WiFiDirect;

public class ConnectedDevice : INotifyPropertyChanged
{
    private SocketReaderWriter socketRW;
    private WiFiDirectDevice wfdDevice;
    private string displayName = "";

    public ConnectedDevice(string displayName, WiFiDirectDevice wfdDevice, SocketReaderWriter socketRW)
    {
        this.socketRW = socketRW;
        this.wfdDevice = wfdDevice;
        this.displayName = displayName;
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
            socketRW = value;
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
            wfdDevice = value;
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

    public event PropertyChangedEventHandler PropertyChanged;
}
