using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Interop;
using L_ShareAssistant.Util;

namespace L_ShareAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    //TODO Cloud Clipboard
    public partial class MainWindow : Window
    {

        private string _multicastGroupIpStr = "234.5.6.7";
        private string _localIp;
        private int _remoteUdpPort = 7269;
        private int _tcpPort = 7270;
        private volatile bool _isUdpSending = true;
        private volatile bool _isUdpReceiving = true;
        private volatile bool _isTcpListening = true;
        private bool _isStarted = false;
        private const int MAX_UDP_SEND = 10;
        private IntPtr _windowHandle;
        private const int COPY_KEYCODE = 100;
        private delegate void _messageReceive(string message);
        private event _messageReceive _onMessageReceived;


        private Dictionary<string, TcpClient> connectedClients;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void _windowSourceInitialized(object sender, EventArgs e)
        {
            _init();
        }

        private void _windowClosed(object sender, EventArgs e)
        {
            HotKey.UnregisterHotKey(_windowHandle, 100);
            _isUdpSending = false;
            _isUdpReceiving = false;
            _isTcpListening = false;
            Environment.Exit(0);
        }

        private void _init()
        {
            connectedClients = new Dictionary<string, TcpClient>();
            _windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(_windowHandle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
            HotKey.RegisterHotKey(_windowHandle, 100, HotKey.KeyModifiers.WindowsKey, System.Windows.Forms.Keys.C);
            _onMessageReceived += _mainWindowOnMessageReceived;
        }

        private void _mainWindowOnMessageReceived(string message)
        {
            DebugHelper.MethodDebug(message);
            if (message == "")
            {
                return;
            }
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // https://msdn.microsoft.com/zh-cn/library/aa686001.aspx
                    Clipboard.SetText(message);

                });


            }
            catch (Exception ex)
            {

                DebugHelper.MethodDebug(ex.Message);
            }
        }

        private void _startButtonClick(object sender, RoutedEventArgs e)
        {
            _isStarted = true;
            Thread udpReceiveThread = new Thread(_udpReceive);
            Thread udpSendThread = new Thread(_udpSend);
            Thread tcpReceiveThread = new Thread(_tcpReceive);

            _localIp = localIpTextBox.Text;

            udpReceiveThread.Start();
            tcpReceiveThread.Start();
            udpSendThread.Start("hello world");
        }



        private void _udpSend(object messageObj)
        {
            DebugHelper.MethodDebug("Entering");
            UdpClient client = new UdpClient(0);
            string message = messageObj as string;
            int i = 0;
            client.JoinMulticastGroup(IPAddress.Parse(_multicastGroupIpStr), IPAddress.Parse(_localIp));
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            while (_isUdpSending)
            {
                i++;
                if (i >= MAX_UDP_SEND)
                {
                    _isUdpSending = false;
                }
                client.Send(messageBytes, messageBytes.Length, _multicastGroupIpStr, _remoteUdpPort);
                DebugHelper.MethodDebug(string.Format("Send {0}", message));
                Thread.Sleep(1000);
            }
            DebugHelper.MethodDebug("Exiting");

        }

        private void _udpReceive()
        {
            DebugHelper.MethodDebug("Entering");
            UdpClient client = new UdpClient(_remoteUdpPort);
            client.JoinMulticastGroup(IPAddress.Parse(_multicastGroupIpStr), IPAddress.Parse(_localIp));
            IPEndPoint remoteIpEndPoint = null;
            while (_isUdpReceiving)
            {
                byte[] bytesReceived = client.Receive(ref remoteIpEndPoint);
                string remoteIpStr = remoteIpEndPoint.Address.ToString();
                if (remoteIpStr == _localIp || connectedClients.ContainsKey(remoteIpStr))
                {
                    continue;
                }
                string message = Encoding.UTF8.GetString(bytesReceived);
                DebugHelper.MethodDebug(string.Format("Receive [{0}] {1}", remoteIpEndPoint.ToString(), message));
                Thread tcpConnectThread = new Thread(_tcpConnect);
                tcpConnectThread.Start(remoteIpEndPoint.Address.ToString());
            }
            DebugHelper.MethodDebug("Exiting");
        }

        private void _tcpReceive()
        {
            DebugHelper.MethodDebug("Entering");

            TcpListener listener = new TcpListener(IPAddress.Parse(_localIp), _tcpPort);
            listener.Start();


            while (_isTcpListening)
            {
                TcpClient client = listener.AcceptTcpClient();
                IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                try
                {
                    connectedClients.Add(remoteIpEndPoint.Address.ToString(), client);
                }
                catch (Exception)
                {
                    client.Close();
                    continue;
                }
                DebugHelper.MethodDebug(string.Format("{0} is connected", client.Client.RemoteEndPoint));
                Thread readThread = new Thread(_readFromClient);
                readThread.Start(client);
                _isUdpSending = false;

                _sendMessageToClient(client, "lalala");


            }
            DebugHelper.MethodDebug("Exiting");

        }

        private bool _sendMessageToClient(TcpClient client, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            return _sendDataToClient(client, buffer);
        }

        private bool _sendDataToClient(TcpClient client, byte[] buffer)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception)
            {
                DebugHelper.MethodDebug(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                connectedClients.Remove(remoteIpEndPoint.Address.ToString());
                client.Close();
                return false;
            }
        }

        private void _readFromClient(object clientObj)
        {
            DebugHelper.MethodDebug("Entering");

            TcpClient client = clientObj as TcpClient;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            while (true)
            {
                try
                {
                    string tempFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
                    using (System.IO.FileStream fs = new System.IO.FileStream(tempFilePath, System.IO.FileMode.Create))
                    {
                        int offset = 0;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            //string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            //_onMessageReceived(message);

                            fs.Write(buffer, offset, bytesRead);
                            //offset += bytesRead;
                        }
                    }


                }
                catch (Exception)
                {
                    DebugHelper.MethodDebug(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    connectedClients.Remove(remoteIpEndPoint.Address.ToString());
                    client.Close();
                    return;
                }
            }


        }

        private void _tcpConnect(object remoteIpObj)
        {
            DebugHelper.MethodDebug("Entering");

            string remoteIp = remoteIpObj as string;
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse(remoteIp), _tcpPort);
            IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            try
            {
                connectedClients.Add(remoteIpEndPoint.Address.ToString(), client);
            }
            catch (Exception)
            {
                client.Close();
                return;
            }

            NetworkStream stream = client.GetStream();
            string message = "Hello world";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
            _readFromClient(client);
        }


        private void _sendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var client in connectedClients.Values)
            {
                _sendMessageToClient(client, messageTextBox.Text);
            }
        }


        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            const int WM_HOTKEY = 0x0312;

            //按快捷键 
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case COPY_KEYCODE:
                            this._copyToRemoteClients();
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void _copyToRemoteClients()
        {
            Clipboard.Clear();
            System.Windows.Forms.SendKeys.SendWait("^c");

            IDataObject data = Clipboard.GetDataObject();
            if (data.GetDataPresent(DataFormats.Text))
            {
                string message = data.GetData(DataFormats.Text) as string;
                if (message == "")
                {
                    return;
                }
                foreach (var client in connectedClients.Values)
                {
                    _sendMessageToClient(client, message);
                }
            }

        }

        private void chooseFileButtonClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            var dialogResult = fileDialog.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                filePathTextBox.Text = fileDialog.FileName;
            }
        }

        private void sendFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(filePathTextBox.Text))
            {
                return;
            }
            using (System.IO.FileStream fs = new System.IO.FileStream(filePathTextBox.Text, System.IO.FileMode.Open))
            {
                byte[] buffer = new byte[1024];
                int offset = 0;
                int readLength;
                while ((readLength = fs.Read(buffer, offset, buffer.Length)) > 0)
                {
                    //offset += readLength;
                    foreach (var client in connectedClients.Values)
                    {
                        _sendDataToClient(client, buffer);
                    }
                }
            }
        }
    }
}
