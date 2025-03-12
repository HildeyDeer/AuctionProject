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
                        Category TEXT NOT NULL,
                        EndTime DATETIME NOT NULL,
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
            await stream.WriteAsync(responseData, 0, responseData.Length); // Отправляем ответ только отправителю

            // Рассылаем всем, кроме отправителя
            if (request.StartsWith("CHAT") || request.StartsWith("BID") || request.StartsWith("ADD_AUCTION"))
            {
                BroadcastMessage(response, client);
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
            // Проверяем, что не передаются параметры
            if (parts.Length != 1)
            {
                return "ERROR|Неверный формат запроса";
            }

            using var conn1 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn1.Open();
            using var cmd1 = new SQLiteCommand(conn1);

            cmd1.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Category, A.EndTime 
                         FROM Auctions A 
                         JOIN Owners O ON A.OwnerId = O.Id 
                         WHERE A.Status = 'Pending'";

            using var reader = cmd1.ExecuteReader();
            List<string> auctions = new();

            while (reader.Read())
            {
                string name = reader.GetString(0);
                string owner = reader.GetString(1);
                double startPrice = reader.GetDouble(2);
                string category = reader.GetString(3);
                string endTime = reader.GetString(4);

                auctions.Add($"{name},{owner},{startPrice},{category},{endTime}");
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

        // Запрос деталей аукциона (по названию)
        if (command == "GET_AUCTION_DETAILS" && parts.Length == 2)
        {
            string auctionName = parts[1];

            cmd.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Description, A.Category, A.EndTime, A.Status
                    FROM Auctions A 
                    JOIN Owners O ON A.OwnerId = O.Id 
                    WHERE A.Name = @name";
            cmd.Parameters.AddWithValue("@name", auctionName);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                
                string name = reader.GetString(0);
                string owner = reader.GetString(1);
                double startPrice = reader.GetDouble(2);
                string description = reader.IsDBNull(3) ? "Нет описания" : reader.GetString(3);
                string category = reader.GetString(4);
                string endTime = reader.GetString(5);
                string status = reader.GetString(6);


                return $"AUCTION_DETAILS|{name}|{owner}|{startPrice}|{description}|{category}|{endTime}|{status}";
            }

            return "ERROR|Аукцион не найден";
        }

        // Обработка ставки
        if (command == "BID" && parts.Length == 4)
        {
            string auctionName = parts[1]; // Исправлено: теперь строка
            string username = parts[2];
            double bidAmount = double.Parse(parts[3]);

            // Обновляем ставку в базе данных
            cmd.CommandText = "UPDATE Auctions SET StartPrice = @bid WHERE Name = @name";
            cmd.Parameters.AddWithValue("@bid", bidAmount);
            cmd.Parameters.AddWithValue("@name", auctionName);
            cmd.ExecuteNonQuery();

            // Получаем обновленные данные из БД
            cmd.CommandText = "SELECT StartPrice FROM Auctions WHERE Name = @name";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@name", auctionName);
            object newBidObj = cmd.ExecuteScalar();

            if (newBidObj != null)
            {
                double newBid = Convert.ToDouble(newBidObj);
                string bidResponse = $"BID_UPDATE|{auctionName}|{username}|{newBid}";
                BroadcastMessage(bidResponse); // Рассылаем обновленные данные клиентам
                return bidResponse;
            }
            else
            {
                return "ERROR|Аукцион не найден";
            }
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

        if (command == "FILTER_BY_CATEGORY" && parts.Length == 2)
        {
            string category = parts[1];

            using var conn2 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn2.Open();
            using var cmd2 = new SQLiteCommand(conn2);

            cmd2.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Category, A.EndTime 
                         FROM Auctions A 
                         JOIN Owners O ON A.OwnerId = O.Id 
                         WHERE A.Status = 'Pending' AND A.Category = @category";
            cmd2.Parameters.AddWithValue("@category", category);

            using var reader2 = cmd2.ExecuteReader();
            List<string> filteredAuctions = new();

            while (reader2.Read())
            {
                string name = reader2.GetString(0);
                string owner = reader2.GetString(1);
                double startPrice = reader2.GetDouble(2);
                string categoryName = reader2.GetString(3);
                string endTime = reader2.GetString(4);

                filteredAuctions.Add($"{name},{owner},{startPrice},{categoryName},{endTime}");
            }

            return filteredAuctions.Count > 0 ? $"AUCTIONS|{string.Join(";", filteredAuctions)}" : "AUCTIONS|EMPTY";
        }

        // Закрытие аукциона
        if (command == "CLOSE_AUCTION" && parts.Length == 2)
        {
            string auctionName = parts[1];

            cmd.CommandText = "UPDATE Auctions SET Status = 'Closed' WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", auctionName);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                return $"SUCCESS|Аукцион {auctionName} закрыт";
            }
            else
            {
                return "ERROR|Аукцион не найден";
            }
        }


        return "ERROR|Неизвестная команда";
    }


    private static async void BroadcastMessage(string message, TcpClient excludeClient = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        List<TcpClient> disconnectedClients = new();

        foreach (var client in clients)
        {
            if (client == excludeClient) continue;

            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch
            {
                disconnectedClients.Add(client);
            }
        }

        foreach (var client in disconnectedClients)
        {
            clients.Remove(client);
        }
    }


}