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

namespace L_ShareAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string multicastGroupIpStr = "234.5.6.7";
        private string localIp;
        private int remoteUdpPort = 7269;
        private int tcpPort = 7270;
        private volatile bool isUdpSending = true;
        private volatile bool isUdpReceiving = true;
        private volatile bool isTcpListening = true;
        private bool isStarted = false;

        private Dictionary<IPEndPoint, TcpClient> connectedClients;


        public MainWindow()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            connectedClients = new Dictionary<IPEndPoint, TcpClient>();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            isStarted = true;
            Thread udpReceiveThread = new Thread(udpReceive);
            Thread udpSendThread = new Thread(udpSend);
            Thread tcpReceiveThread = new Thread(tcpReceive);

            localIp = localIpTextBox.Text;

            udpReceiveThread.Start();
            tcpReceiveThread.Start();
            udpSendThread.Start("hello world");
        }

        private void showDebugInfo(string message)
        {
            Dispatcher.Invoke(() =>
            {
                debugTextBox.AppendText(message + Environment.NewLine);
            });
        }

        private void udpSend(object messageObj)
        {
            showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));
            UdpClient client = new UdpClient(0);
            string message = messageObj as string;
            int i = 0;
            client.JoinMulticastGroup(IPAddress.Parse(multicastGroupIpStr), IPAddress.Parse(localIp));
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            while (isUdpSending)
            {
                i++;
                if (i >= 10)
                {
                    isUdpSending = false;
                }
                client.Send(messageBytes, messageBytes.Length, multicastGroupIpStr, remoteUdpPort);
                showDebugInfo(string.Format("Send {0}", message));
                Thread.Sleep(1000);
            }
            showDebugInfo("UDP send stopped");
        }

        private void udpReceive()
        {
            showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

            UdpClient client = new UdpClient(remoteUdpPort);
            client.JoinMulticastGroup(IPAddress.Parse(multicastGroupIpStr), IPAddress.Parse(localIp));
            IPEndPoint remoteIpEndPoint = null;
            while (isUdpReceiving)
            {
                byte[] bytesReceived = client.Receive(ref remoteIpEndPoint);
                if (remoteIpEndPoint.Address.ToString() == localIp)
                {
                    continue;
                }
                string message = Encoding.UTF8.GetString(bytesReceived);
                showDebugInfo(string.Format("Receive [{0} {1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), remoteIpEndPoint.ToString(), message));
                Thread tcpConnectThread = new Thread(tcpConnect);
                tcpConnectThread.Start(remoteIpEndPoint.Address.ToString());
            }
            showDebugInfo("UDP receive stopped");
        }

        private void tcpReceive()
        {
            showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

            TcpListener listener = new TcpListener(IPAddress.Parse(localIp), tcpPort);
            listener.Start();


            while (isTcpListening)
            {
                TcpClient client = listener.AcceptTcpClient();
                connectedClients.Add(client.Client.RemoteEndPoint as IPEndPoint, client);
                showDebugInfo(string.Format("{0} is connected", client.Client.RemoteEndPoint));
                Thread readThread = new Thread(readFromClient);
                readThread.Start(client);
                isUdpSending = false;

                sendMessageToClient(client, "lalala");


            }
            showDebugInfo("TCP receive stopped");

        }

        private bool sendMessageToClient(TcpClient client, string message)
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
                showDebugInfo(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                connectedClients.Remove(client.Client.RemoteEndPoint as IPEndPoint);
                client.Close();
                return false;
            }
        }

        private void readFromClient(object clientObj)
        {
            showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

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
                        showDebugInfo(message);
                    }
                }
                catch (Exception)
                {
                    showDebugInfo(string.Format("Client [{0}] is disconnected", client.Client.RemoteEndPoint.ToString()));
                    connectedClients.Remove(client.Client.RemoteEndPoint as IPEndPoint);
                    client.Close();
                    return;
                }
            }


        }

        private void tcpConnect(object remoteIpObj)
        {
            showDebugInfo(string.Format("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name));

            string remoteIp = remoteIpObj as string;
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse(remoteIp), tcpPort);
            connectedClients.Add(client.Client.RemoteEndPoint as IPEndPoint, client);
            NetworkStream stream = client.GetStream();
            string message = "Hello world";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
            readFromClient(client);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isUdpSending = false;
            isUdpReceiving = false;
            isTcpListening = false;
            Environment.Exit(0);
        }

        private void sendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var client in connectedClients.Values)
            {
                sendMessageToClient(client, messageTextBox.Text);
            }
        }
    }
}
