using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleTest
{
    class Program
    {
        static string multicastGroupIpStr = "234.5.6.7";
        static string localIp = "192.168.1.109";
        static int remoteUdpPort = 7269;
        static int tcpPort = 7270;
        static volatile bool isUdpSending = true;
        static volatile bool isUdpReceiving = true;
        static volatile bool isTcpListening = true;
        static void Main(string[] args)
        {
            int sendPort = 7269;
            int receivePort = 7270;
            string type = Console.ReadLine();
            if (type == "1")
            {
                receiveProcess(receivePort);
            }
            else
            {
                sendProcess(sendPort, receivePort);
            }
            Console.Read();
        }

        private static void sendProcess(int sendPort, int receivePort)
        {
            string sendPath = @"D:\Download\nginx 1.11.11.1 Lion.zip";
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(localIp, receivePort);
                Console.WriteLine("[{0}][{1}:{2}] connected.", DateTime.Now.ToLongTimeString(), localIp, receivePort);
                byte[] buffer = new byte[1024];
                NetworkStream stream = client.GetStream();
                using (FileStream fs = new FileStream(sendPath, FileMode.Open))
                {
                    int bytesSend = 0;
                    int bytesRead = 0;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        bytesSend += bytesRead;
                        Console.WriteLine("[{0}][{1}] bytes sent.", DateTime.Now.ToLongTimeString(), bytesSend);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void receiveProcess(int receivePort)
        {
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
            TcpListener listener = new TcpListener(IPAddress.Parse(localIp), receivePort);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("[{0}][{1}] connected.", DateTime.Now.ToLongTimeString(), client.Client.RemoteEndPoint);
            byte[] buffer = new byte[1024];
            using (FileStream fs = new FileStream(savePath, FileMode.Create))
            {
                NetworkStream stream = client.GetStream();
                int bytesReceived = 0;
                int bytesRead = 0;
                try
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                        bytesReceived += bytesRead;
                        Console.WriteLine("[{0}][{1}] bytes received.", DateTime.Now.ToLongTimeString(), bytesReceived);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("[{0}][{1}] disconnected.", DateTime.Now.ToLongTimeString(), client.Client.RemoteEndPoint);
                }

            }
        }

        static void udpSend(object messageObj)
        {
            Console.WriteLine("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name);
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
                Console.WriteLine("Send {0}", message);
                Thread.Sleep(1000);
            }
            Console.WriteLine("UDP send stopped");
        }

        static void udpReceive()
        {
            Console.WriteLine("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name);

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
                Console.WriteLine("Receive [{0} {1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), remoteIpEndPoint.ToString(), message);
                Thread tcpConnectThread = new Thread(tcpConnect);
                tcpConnectThread.Start(remoteIpEndPoint.Address.ToString());
            }
            Console.WriteLine("UDP receive stopped");
        }

        static void tcpReceive()
        {
            Console.WriteLine("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name);

            TcpListener listener = new TcpListener(IPAddress.Parse(localIp), tcpPort);
            listener.Start();


            while (isTcpListening)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("{0} is connected", client.Client.RemoteEndPoint);

                NetworkStream stream = client.GetStream();

                Thread readNetworkStreamThread = new Thread(readFromNetworkStream);
                readNetworkStreamThread.Start(stream);
                isUdpSending = false;

                while (true)
                {
                    string responseStr = "la la la";
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(responseStr);
                    stream.Write(responseBuffer, 0, responseBuffer.Length);
                    Thread.Sleep(1000);
                }


            }
            Console.WriteLine("TCP receive stopped");

        }

        private static void readFromNetworkStream(object streamObj)
        {
            Console.WriteLine("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name);

            NetworkStream stream = streamObj as NetworkStream;
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);
                }
            }
            catch (Exception)
            {

                Console.WriteLine("Client is disconnected");
                return;
            }


        }

        static void tcpConnect(object remoteIpObj)
        {
            Console.WriteLine("[Debug] Current method: {0}", System.Reflection.MethodBase.GetCurrentMethod().Name);

            string remoteIp = remoteIpObj as string;
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse(remoteIp), tcpPort);
            using (NetworkStream stream = client.GetStream())
            {
                string message = "Hello world";
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);

                //client.Close();
                //return;

                while (true)
                {
                    readFromNetworkStream(stream);
                }
            }
            //client.Close();
        }
    }
}
