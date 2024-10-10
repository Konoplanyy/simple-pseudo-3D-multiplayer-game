using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private TcpClient client;
    private NetworkStream stream;

    public void Connect(string ip, int port)
    {
        client = new TcpClient(ip, port);
        stream = client.GetStream();
        Console.WriteLine("Підключено до сервера.");

        // Потік для отримання даних
        Thread receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
    }

    public void SendData(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    private void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Отримано з сервера: {data}");
        }
    }

    public void Close()
    {
        stream.Close();
        client.Close();
    }

    static void Main(string[] args)
    {
        Client client = new Client();
        client.Connect("127.0.0.1", 12345);

        
        // Надсилаємо тестові дані
        while (true)
        {
            client.SendData("6,235844,6,9118056,-3,9583979");

        }

        Console.ReadLine();
        client.Close();
    }
}
