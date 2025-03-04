using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

class LoadBalancer
{
    private List<IPEndPoint> _auctionServers = new();  // Список серверов аукционов
    private int _nextServerIndex = 0;                 // Индекс для Round Robin

    public LoadBalancer()
    {
        _auctionServers.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001)); // Сервер 1
        _auctionServers.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002)); // Сервер 2
    }

    public async Task StartAsync(int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Load Balancer запущен на порту {port}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        IPEndPoint targetServer = GetNextServer();
        Console.WriteLine($"Перенаправление клиента на {targetServer}");

        using TcpClient server = new TcpClient();
        await server.ConnectAsync(targetServer.Address, targetServer.Port);

        using NetworkStream clientStream = client.GetStream();
        using NetworkStream serverStream = server.GetStream();

        // Двусторонняя передача данных
        Task clientToServer = clientStream.CopyToAsync(serverStream);
        Task serverToClient = serverStream.CopyToAsync(clientStream);

        await Task.WhenAny(clientToServer, serverToClient);
    }

    private IPEndPoint GetNextServer()
    {
        IPEndPoint server = _auctionServers[_nextServerIndex];
        _nextServerIndex = (_nextServerIndex + 1) % _auctionServers.Count;
        return server;
    }

    public static async Task Main()
    {
        LoadBalancer lb = new LoadBalancer();
        await lb.StartAsync(5000);
    }
}