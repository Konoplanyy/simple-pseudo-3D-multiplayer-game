using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private TcpListener listener;
    private List<TcpClient> clients = new List<TcpClient>();
    private object lockObj = new object();

    public Server(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine($"Сервер запущено на порту {((IPEndPoint)listener.LocalEndpoint).Port}.");

        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                lock (lockObj)
                {
                    clients.Add(client);
                }
                Console.WriteLine("Клієнт підключено.");
                Task.Run(() => HandleClient(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при підключенні: {ex.Message}");
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Отримано: {message}");

                BroadcastMessage(message, client);
            }
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"IOException: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
        finally
        {
            lock (lockObj)
            {
                clients.Remove(client);
            }
            client.Close();
            Console.WriteLine("Клієнт відключено.");
        }
    }

    private void BroadcastMessage(string message, TcpClient sender)
    {
        byte[] responseData = Encoding.UTF8.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                if (client != sender)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(responseData, 0, responseData.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка при відправці даних: {ex.Message}");
                    }
                }
            }
        }
    }

    static void Main(string[] args)
    {
        int port = 12345;
        Server server = new Server(port);
        server.Start();
    }
}
