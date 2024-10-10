using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Try1_None_library
{
    public class Multiplayer
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;
        private volatile bool running;

        public float RemotePlayerX { get; private set; }
        public float RemotePlayerY { get; private set; }
        public float RemotePlayerA { get; private set; }

        public void Connect(string ip, int port)
        {
            try
            {
                client = new TcpClient(ip, port);
                stream = client.GetStream();
                running = true;
                receiveThread = new Thread(ReceiveData);
                receiveThread.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IOException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception: {ex.Message}");
            }
        }

        public void SendData(float x, float y, float a)
        {
            try
            {
                string data = $"{x}:{y}:{a}";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IOException during SendData: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception during SendData: {ex.Message}");
            }
        }

        private void ReceiveData()
        {
            byte[] buffer = new byte[1024];

            while (running)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] parts = data.Split(':');

                        if (parts.Length == 3 &&
                            float.TryParse(parts[0], out float x) &&
                            float.TryParse(parts[1], out float y) &&
                            float.TryParse(parts[2], out float a))
                        {
                            RemotePlayerX = x;
                            RemotePlayerY = y;
                            RemotePlayerA = a;
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IOException during ReceiveData: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected exception during ReceiveData: {ex.Message}");
                    break;
                }
            }
        }

        public void Close()
        {
            running = false;
            receiveThread?.Join();
            stream?.Close();
            client?.Close();
        }
    }
}
