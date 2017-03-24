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

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _init();
        }

        private void Window_Closed(object sender, EventArgs e)
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
            _onMessageReceived += MainWindow_onMessageReceived;
        }

        private void MainWindow_onMessageReceived(string message)
        {
            _showDebugInfo(message);
            if(message == "")
            {
                return;
            }
            try
            {
                Dispatcher.Invoke(() => {
                    // https://msdn.microsoft.com/zh-cn/library/aa686001.aspx
                    Clipboard.SetText(message);

                });


            }
            catch (Exception ex)
            {

                _showDebugInfo(ex.Message);
            }
        }

        private void _startButton_Click(object sender, RoutedEventArgs e)
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

        private void _showDebugInfo(string message)
        {
            Dispatcher.Invoke(() =>
            {
                debugTextBox.AppendText(message + Environment.NewLine);
            });
        }

        private void _udpSend(object messageObj)
        {
            _showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));
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
                _showDebugInfo(string.Format("Send {0}", message));
                Thread.Sleep(1000);
            }
            _showDebugInfo("UDP send stopped");
        }

        private void _udpReceive()
        {
            _showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

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
                _showDebugInfo(string.Format("Receive [{0} {1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), remoteIpEndPoint.ToString(), message));
                Thread tcpConnectThread = new Thread(_tcpConnect);
                tcpConnectThread.Start(remoteIpEndPoint.Address.ToString());
            }
            _showDebugInfo("UDP receive stopped");
        }

        private void _tcpReceive()
        {
            _showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

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
                _showDebugInfo(string.Format("{0} is connected", client.Client.RemoteEndPoint));
                Thread readThread = new Thread(_readFromClient);
                readThread.Start(client);
                _isUdpSending = false;

                _sendMessageToClient(client, "lalala");


            }
            _showDebugInfo("TCP receive stopped");

        }

        private bool _sendMessageToClient(TcpClient client, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception)
            {
                _showDebugInfo(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                connectedClients.Remove(remoteIpEndPoint.Address.ToString());
                client.Close();
                return false;
            }
        }

        private void _readFromClient(object clientObj)
        {
            _showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

            TcpClient client = clientObj as TcpClient;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            while (true)
            {
                try
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _onMessageReceived(message);
                    }
                }
                catch (Exception)
                {
                    _showDebugInfo(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    connectedClients.Remove(remoteIpEndPoint.Address.ToString());
                    client.Close();
                    return;
                }
            }


        }

        private void _tcpConnect(object remoteIpObj)
        {
            _showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

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


        private void _sendMessageButton_Click(object sender, RoutedEventArgs e)
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
            if(data.GetDataPresent(DataFormats.Text))
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
    }
}
