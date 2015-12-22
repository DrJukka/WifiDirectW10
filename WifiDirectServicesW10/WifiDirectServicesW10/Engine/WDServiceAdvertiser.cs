using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect.Services;

namespace WifiDirectServicesW10.Engine
{
    class WDServiceAdvertiser : IDisposable
    {
        WiFiDirectServiceAdvertiser _advertiser;

        public WDServiceAdvertiser()
        {
            List<WiFiDirectServiceConfigurationMethod> configMethods = new List<WiFiDirectServiceConfigurationMethod>();
            configMethods.Add(WiFiDirectServiceConfigurationMethod.PinDisplay);

            WiFiDirectServiceStatus status = WiFiDirectServiceStatus.Available;


            // Create a Service Advertiser
            _advertiser = new WiFiDirectServiceAdvertiser("drjukka_wifiservice");
            _advertiser.AutoAcceptSession = true;
            _advertiser.PreferGroupOwnerMode = false;
            _advertiser.ServiceStatus = status;
            _advertiser.CustomServiceStatusCode = 0xaa;

            // Service information can be up to 65000 bytes.
            // Service Seeker may explicitly discover this by specifying a short buffer that is a subset of this buffer.
            // If seeker portion matches, then entire buffer is returned, otherwise, the service information is not returned to the seeker
            // This sample uses a string for the buffer but it can be any data
         /*   if (serviceInfo != null && serviceInfo.Length > 0)
            {
                using (var tempStream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                {
                    using (var serviceInfoDataWriter = new Windows.Storage.Streams.DataWriter(tempStream))
                    {
                        serviceInfoDataWriter.WriteString(serviceInfo);
                        advertiser.ServiceInfo = serviceInfoDataWriter.DetachBuffer();
                    }
                }
            }
            else */
            {
                _advertiser.ServiceInfo = null;
            }

            // This is a buffer of up to 144 bytes that is sent to the seeker in case the connection is "deferred" (i.e. not auto-accepted)
            // This buffer will be sent when auto-accept is false, or if a PIN is required to complete the connection
            // For the sample, we use a string, but it can contain any data
    /*        if (deferredServiceInfo != null && deferredServiceInfo.Length > 0)
            {
                using (var tempStream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                {
                    using (var deferredSessionInfoDataWriter = new Windows.Storage.Streams.DataWriter(tempStream))
                    {
                        deferredSessionInfoDataWriter.WriteString(deferredServiceInfo);
                        advertiser.DeferredSessionInfo = deferredSessionInfoDataWriter.DetachBuffer();
                    }
                }
            }
            else*/
            {
                _advertiser.DeferredSessionInfo = null;
            }

            // The advertiser supported configuration methods
            // Valid values are PIN-only (either keypad entry, display, or both), or PIN (keypad entry, display, or both) and WFD Services default
            // WFD Services Default config method does not require explicit PIN entry and offers a more seamless connection experience
            // Typically, an advertiser will support PIN display (and WFD Services Default), and a seeker will connect with either PIN entry or WFD Services Default
            if (configMethods != null)
            {
                _advertiser.PreferredConfigurationMethods.Clear();
                foreach (var configMethod in configMethods)
                {
                    _advertiser.PreferredConfigurationMethods.Add(configMethod);
                }
            }

            // Advertiser may also be discoverable by a prefix of the service name. Must explicitly specify prefixes allowed here.
           /* if (prefixList != null && prefixList.Count > 0)
            {
                advertiser.ServiceNamePrefixes.Clear();
                foreach (var prefix in prefixList)
                {
                    advertiser.ServiceNamePrefixes.Add(prefix);
                }
            }*/

            // Register for session requests from Seeker(s)
            _advertiser.SessionRequested += _advertiser_SessionRequested;

            // Start the advertiser
            _advertiser.Start();



        }

        private void _advertiser_SessionRequested(WiFiDirectServiceAdvertiser sender, WiFiDirectServiceSessionRequestedEventArgs args)
        {
            WiFiDirectServiceSessionRequest request = args.GetSessionRequest();

            DeviceInformation devInfo = request.DeviceInformation;

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


        }

        public void Dispose()
        {
            
            try
            {
                if (_advertiser.AdvertisementStatus == WiFiDirectServiceAdvertisementStatus.Started)
                {
                    _advertiser.Stop();
                }
            }
            catch (Exception)
            {
                // Stop can throw if it is already stopped or stopping, ignore
            }

            _advertiser.SessionRequested -= _advertiser_SessionRequested;
 //           _advertiser.AdvertisementStatusChanged -= OnAdvertisementStatusChanged;
   //         _advertiser.AutoAcceptSessionConnected -= OnAutoAcceptSessionConnected;
        }
    }
}

