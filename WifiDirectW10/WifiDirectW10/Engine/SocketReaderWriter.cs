using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using System.ComponentModel;
using Windows.Devices.WiFiDirect;

namespace WifiDirectW10.Engine
{
    public class SocketReaderWriter : IDisposable
    {
        DataReader _dataReader;
        DataWriter _dataWriter;
        StreamSocket _streamSocket;
        private MainPage _rootPage;
        string _currentMessage;

        public SocketReaderWriter(StreamSocket socket, MainPage mainPage)
        {
            _dataReader = new DataReader(socket.InputStream);
            _dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
            _dataReader.ByteOrder = ByteOrder.LittleEndian;

            _dataWriter = new DataWriter(socket.OutputStream);
            _dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
            _dataWriter.ByteOrder = ByteOrder.LittleEndian;

            _streamSocket = socket;
            _rootPage = mainPage;
            _currentMessage = null;
        }

        public void Dispose()
        {
            _dataReader.Dispose();
            _dataWriter.Dispose();
            _streamSocket.Dispose();
        }

        public async void WriteMessage(string message)
        {
            try
            {
                _dataWriter.WriteUInt32(_dataWriter.MeasureString(message));
                _dataWriter.WriteString(message);
                await _dataWriter.StoreAsync();
                System.Diagnostics.Debug.WriteLine("Sent message: " + message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WriteMessage threw exception: " + ex.Message);
            }
        }

        public async void ReadMessage()
        {
            try
            {
                UInt32 bytesRead = await _dataReader.LoadAsync(sizeof(UInt32));
                if (bytesRead > 0)
                {
                    // Determine how long the string is.
                    UInt32 messageLength = _dataReader.ReadUInt32();
                    bytesRead = await _dataReader.LoadAsync(messageLength);
                    if (bytesRead > 0)
                    {
                        // Decode the string.
                        _currentMessage = _dataReader.ReadString(messageLength);
                        System.Diagnostics.Debug.WriteLine("Got message: " + _currentMessage);

                        ReadMessage();
                    }
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Socket was closed!");
            }
        }

        public string GetCurrentMessage()
        {
            return _currentMessage;
        }
    }
}
