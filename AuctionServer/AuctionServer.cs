using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AuctionServer
{
    private const int Port = 5001;  // Порт для аукциона
    private const string DbPath = "C:\\Users\\user\\Downloads\\Project TEST\\Project TEST\\AuctionServer\\AuctionDB.db"; // Файл базы данных SQLite
    private static List<TcpClient> clients = new();

    public static async Task Main()
    {
        InitDatabase(); // Создание базы данных при старте
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
        listener.Start();
        Console.WriteLine($"Auction Server запущен на порту {Port}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clients.Add(client);
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static void InitDatabase()
    {
        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        // Таблица пользователей
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Users (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT UNIQUE NOT NULL,
                                Password TEXT NOT NULL
                            );";
        cmd.ExecuteNonQuery();

        // Таблица владельцев аукционов
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Owners (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT UNIQUE NOT NULL,
                                Password TEXT NOT NULL,
                                PermissionKey TEXT NOT NULL
                            );";
        cmd.ExecuteNonQuery();

        // Таблица аукционов
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Auctions (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                OwnerId INTEGER NOT NULL,
                                Name TEXT NOT NULL,
                                Description TEXT,
                                StartPrice REAL NOT NULL,
                                Status TEXT DEFAULT 'Pending',
                                FOREIGN KEY (OwnerId) REFERENCES Owners(Id)
                            );";
        cmd.ExecuteNonQuery();
    }

    private static async Task HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Получено: {request}");
            string response = ProcessRequest(request);

            byte[] responseData = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseData, 0, responseData.Length);

            // Рассылаем всем клиентам, если это сообщение в чат, ставка или добавление аукциона
            if (request.StartsWith("CHAT") || request.StartsWith("BID") || request.StartsWith("ADD_AUCTION"))
            {
                BroadcastMessage(response);
            }
        }

        clients.Remove(client);
    }

    private static string ProcessRequest(string request)
    {
        string[] parts = request.Split('|');
        string command = parts[0];

        // Обработка команды GET_AUCTIONS отдельно
        if (command == "GET_AUCTIONS")
        {
            if (parts.Length != 1) // Для GET_AUCTIONS только один параметр
            {
                return "ERROR|Неверный формат запроса";
            }

            using var conn1 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn1.Open();
            using var cmd1 = new SQLiteCommand(conn1);

            cmd1.CommandText = "SELECT A.Name, O.Username, A.StartPrice FROM Auctions A JOIN Owners O ON A.OwnerId = O.Id WHERE A.Status = 'Pending'";

            using var reader = cmd1.ExecuteReader();
            List<string> auctions = new();

            while (reader.Read())
            {
                string name = reader.GetString(0);
                string owner = reader.GetString(1);
                double startPrice = reader.GetDouble(2);
                auctions.Add($"{name},{owner},{startPrice}");
            }

            return auctions.Count > 0 ? $"AUCTIONS|{string.Join(";", auctions)}" : "AUCTIONS|EMPTY";
        }

        // Для других команд
        if (parts.Length < 2)
        {
            return "ERROR|Неверный формат запроса";
        }

        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        // Обработка ставки
        if (command == "BID" && parts.Length == 4)
        {
            int auctionId = int.Parse(parts[1]);
            string username = parts[2];
            double bidAmount = double.Parse(parts[3]);

            cmd.CommandText = "UPDATE Auctions SET StartPrice = @bid WHERE Id = @id";
            cmd.Parameters.AddWithValue("@bid", bidAmount);
            cmd.Parameters.AddWithValue("@id", auctionId);
            cmd.ExecuteNonQuery();

            return $"BID_UPDATE|{auctionId}|{username}|{bidAmount}";
        }

        if (command == "GET_OWNER_ID" && parts.Length == 2)
        {
            string owner = parts[1];

            cmd.CommandText = "SELECT Id FROM Owners WHERE Username = @username";
            cmd.Parameters.AddWithValue("@username", owner);
            object result = cmd.ExecuteScalar();

            if (result != null)
            {
                int ownerId = Convert.ToInt32(result);
                return $"OWNER_ID|{ownerId}";
            }
            else
            {
                return "ERROR|Владелец не найден";
            }
        }

        if (command == "ADD_AUCTION" && parts.Length == 6)
        {
            string owner = parts[1];
            if (!int.TryParse(parts[2], out int ownerId))
            {
                return "ERROR|Некорректный ID владельца";
            }

            string name = parts[3];
            string description = parts[4];
            if (!double.TryParse(parts[5], out double startPrice))
            {
                return "ERROR|Некорректная цена";
            }

            cmd.CommandText = "INSERT INTO Auctions (OwnerId, Name, Description, StartPrice) VALUES (@ownerId, @name, @description, @startPrice)";
            cmd.Parameters.AddWithValue("@ownerId", ownerId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@startPrice", startPrice);

            try
            {
                cmd.ExecuteNonQuery();
                return $"SUCCESS|Аукцион {name} добавлен";
            }
            catch (Exception ex)
            {
                return $"ERROR|Ошибка БД: {ex.Message}";
            }
        }

        // Обработка сообщений в чате (с префиксом владельца, если это владелец)
        if (command == "CHAT" && parts.Length == 4)
        {
            string username = parts[1];
            bool isOwner = parts[2] == "OWNER"; // Проверка, является ли отправитель владельцем
            string message = parts[3];

            string chatMessage = isOwner ? $"CHAT|Владелец {username}: {message}" : $"CHAT|{username}: {message}";
            return chatMessage;
        }

        return "ERROR|Неизвестная команда";
    }


    private static void BroadcastMessage(string message, TcpClient excludeClient = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        List<TcpClient> disconnectedClients = new();

        foreach (var client in clients)
        {
            if (client == excludeClient) continue; // Исключаем отправителя

            try
            {
                client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch
            {
                disconnectedClients.Add(client);
            }
        }

        // Удаляем отключившихся клиентов
        foreach (var client in disconnectedClients)
        {
            clients.Remove(client);
        }
    }
}