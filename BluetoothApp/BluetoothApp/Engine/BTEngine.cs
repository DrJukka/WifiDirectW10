

using System;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading;
using Windows.Foundation;
using Windows.System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BluetoothApp.Engine
{
    public delegate void ObexConnectionStatusChanged(bool connected, string error);
    public delegate void ObexErrorCallback(string error);
    public delegate void ObexMessage(string message);

    public class BTEngine
    {
        public event ObexConnectionStatusChanged ObexConnectionStatusChanged;
        public event ObexErrorCallback ObexErrorCallback;
        public event ObexMessage ObexMessage;

        const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32
        const uint SERVICE_VERSION = 200;

        // provider & listener are used for incoming connections
        private RfcommServiceProvider _provider;
        private StreamSocketListener _listener;
        private IAsyncAction receivingThread;
        private CancellationTokenSource cancellationTokenSource;
        private bool cancelReceiving = false;

        //seleted device used for connecting out
        private RfcommDeviceService _deviceService;

        //this is either outgoing or incoming socket connection
        private StreamSocket _streamSocket;

        private static BTEngine _instance = new BTEngine();

        public static BTEngine Instance
        {
            get { return _instance; }
        }

        public StreamSocket Socket
        {
            get { return _streamSocket; }
        }

        public RfcommDeviceService SelectedDevice
        {
            get { return _deviceService; }
            set { _deviceService = value; }
        }

        public async void InitializeReceiver()
        {
            // Initialize the provider for the hosted RFCOMM service // RfcommServiceId FromUuid(Guid uuid);
            _provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.ObexObjectPush);

            // Create a listener for this service and start listening
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += Listener_ConnectionReceived;

            System.Diagnostics.Debug.WriteLine("_provider.ServiceId.AsString(): " + _provider.ServiceId.AsString());

            await _listener.BindServiceNameAsync(_provider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start advertising
            InitializeServiceSdpAttributes(_provider);

            // "Unable to cast object of type 'Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider' to type 'Windows.Devices.Bluetooth.Rfcomm.IRfcommServiceProvider2'
            // only with laptop, not with phone
            //_provider.StartAdvertising(_listener,true);
            _provider.StartAdvertising(_listener);

        }

        public void DeInitialize()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            cancelReceiving = true;

            _provider.StopAdvertising();
            _provider = null;

            _listener.Dispose();
            _listener = null;

            _streamSocket.Dispose();
            _streamSocket = null;
        }

        public async void ConnectToDevice(RfcommDeviceService device)
        {
            //connect the socket   
            try
            {
                _streamSocket = new StreamSocket();

                System.Diagnostics.Debug.WriteLine("Connec to :" + device.ConnectionHostName + ", service : " + device.ConnectionServiceName);

                await _streamSocket.ConnectAsync(
                 device.ConnectionHostName,
                 device.ConnectionServiceName,
                 SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
            
                if (ObexConnectionStatusChanged != null)
                {
                    ObexConnectionStatusChanged(true, null);
                }
                ReceiveData();
            }
            catch (Exception ex)
            {
                if (ObexConnectionStatusChanged != null)
                {
                    ObexConnectionStatusChanged(false, "Cannot connect bluetooth device:" + ex.Message);
                }
            }
        }

        public async void SendData(string message)
        {
            if (_streamSocket == null || string.IsNullOrEmpty(message))
            {
                return;
            }
            try
            {
                DataWriter dwriter = new DataWriter(_streamSocket.OutputStream);
                UInt32 len = dwriter.MeasureString(message);
                dwriter.WriteUInt32(len);
                dwriter.WriteString(message);
                await dwriter.StoreAsync();
                await dwriter.FlushAsync();
            }
            catch (Exception ex)
            {
                if (ObexErrorCallback != null)
                {
                    ObexErrorCallback("Sending data from Bluetooth encountered error!" + ex.Message);
                }
            }
        }

        public void ReceiveData()
        {
            if (_streamSocket == null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            receivingThread = ThreadPool.RunAsync(async (s) =>
                 {
                     while (!cancelReceiving)
                     {
                         DataReader dreader = new DataReader(_streamSocket.InputStream);
                         uint sizeFieldCount = await dreader.LoadAsync(sizeof(uint)).AsTask(cancellationTokenSource.Token);

                         if (sizeFieldCount != sizeof(uint))
                         {
                             if (sizeFieldCount == 0)
                             {
                                 if (ObexConnectionStatusChanged != null) {
                                     ObexConnectionStatusChanged(false,null);
                                 }
                                 break;
                             }
                             continue;
                         }

                         uint stringLength;
                         uint actualStringLength;

                         try
                         {
                             stringLength = dreader.ReadUInt32();
                             actualStringLength = await dreader.LoadAsync(stringLength).AsTask(cancellationTokenSource.Token);

                             if (stringLength != actualStringLength)
                             {
                                  continue;
                             }
                             string text = dreader.ReadString(actualStringLength);

                             if (ObexMessage != null)
                             {
                                 ObexMessage(text);
                             }

                         }
                         catch (Exception ex)
                         {
                             if (ObexErrorCallback != null)
                             {
                                 ObexErrorCallback("Reading data from Bluetooth encountered error!" + ex.Message);
                             }
                         }
                     }
                 }, WorkItemPriority.High);
        }

        private void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            StreamSocket socket = args.Socket;
           
            _streamSocket = socket;

            if (ObexConnectionStatusChanged != null)
            {
                ObexConnectionStatusChanged(true, null);
            }                       
            ReceiveData();
        }

        public async Task<bool> IsCompatibleVersion(RfcommDeviceService service)
        {
            System.Diagnostics.Debug.WriteLine("IsCompatibleVersion");

            //currently the advertisement from RfcommServiceProvider appears not being visible on the other end, 
            // thus we can not check the values here
            // when this is fixed, we must remove the return true from here
            return true;
            /*
            try {
                IReadOnlyDictionary<System.UInt32, IBuffer> attributes = await service.GetSdpRawAttributesAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);

                IBuffer attribute = attributes[SERVICE_VERSION_ATTRIBUTE_ID];
                DataReader reader = DataReader.FromBuffer(attribute);

                System.Diagnostics.Debug.WriteLine("reading");

                // The first byte contains the attribute's type
                byte attributeType = reader.ReadByte();
                if (attributeType == SERVICE_VERSION_ATTRIBUTE_TYPE)
                {
                    System.Diagnostics.Debug.WriteLine("We are ok");
                    // The remainder is the data
                    uint version = reader.ReadUInt32();
                    return version >= SERVICE_VERSION;
                }
                System.Diagnostics.Debug.WriteLine("IsCompatibleVersion not good: " + attributeType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("IsCompatibleVersion - Exception " + ex.Message);

                
            }
            return false;*/
        }

        private void InitializeServiceSdpAttributes(RfcommServiceProvider provider)
        {
            DataWriter writer = new DataWriter();
                                                                                                                                                                                                                                                                                                     
            // First write the attribute type
            writer.WriteByte(SERVICE_VERSION_ATTRIBUTE_TYPE);
            // Then write the data
            writer.WriteUInt32(SERVICE_VERSION);

            IBuffer data = writer.DetachBuffer();
            provider.SdpRawAttributes.Add(SERVICE_VERSION_ATTRIBUTE_ID, data);
        }
    }
}
