using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace WifiDirectW10.Model
{
    public class DiscoveredDevice : INotifyPropertyChanged
    {
        private DeviceInformation _item;

        public string Name
        {
            get
            {
                return _item.Name;
            }
        }

        public string Icon
        {
            get
            {
                Object deviceIcon = null;
                if (_item.Properties.TryGetValue("System.Devices.Icon", out deviceIcon))
                {
                    if (deviceIcon == null)
                    {
                        return (String)deviceIcon;
                    }
                }
                if (_item.Properties.TryGetValue("System.Devices.GlyphIcon", out deviceIcon))
                {
                    if (deviceIcon == null)
                    {
                        return (String)deviceIcon;
                    }
                }
                System.Diagnostics.Debug.WriteLine("no icons found for : " + Name);

                return "Assets/StoreLogo.png";
            }
        }

        public DiscoveredDevice(DeviceInformation deviceInfoIn)
        {
            _item = deviceInfoIn;

            foreach (String key in deviceInfoIn.Properties.Keys)
            {
                System.Diagnostics.Debug.WriteLine("key : " + key);
                Object tmpObject = null;
                System.Diagnostics.Debug.WriteLine("try-get : " + deviceInfoIn.Properties.TryGetValue(key,out tmpObject));
                if (tmpObject != null)
                {
                    System.Diagnostics.Debug.WriteLine("Object type " + tmpObject.GetType().ToString());
                }
            }
        }

        public DeviceInformation DeviceInfo
        {
            get
            {
                return _item;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
