using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using GG_MOW_Marketplace.Models;

namespace GG_MOW_Marketplace
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Хеширование пароля (упрощенный вариант)
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // Проверка пароля
        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }

        // Регистрация пользователя
        public bool RegisterUser(string username, string password, string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "INSERT INTO Users (Username, PasswordHash, Email) VALUES (@username, @passwordHash, @email)";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@passwordHash", HashPassword(password));
                command.Parameters.AddWithValue("@email", email);

                return command.ExecuteNonQuery() > 0;
            }
            catch (SqlException)
            {
                return false;
            }
        }

        // Авторизация пользователя
        public User Login(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM Users WHERE Username = @username";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var storedHash = reader["PasswordHash"].ToString();
                if (VerifyPassword(password, storedHash))
                {
                    return new User
                    {
                        UserId = (int)reader["UserId"],
                        Username = reader["Username"].ToString(),
                        Email = reader["Email"].ToString(),
                        RegistrationDate = (DateTime)reader["RegistrationDate"]
                    };
                }
            }
            return null;
        }

        // Получение всех товаров
        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT p.*, c.Name as CategoryName 
                FROM Products p 
                LEFT JOIN Categories c ON p.CategoryId = c.CategoryId 
                WHERE p.IsAvailable = 1 
                ORDER BY p.Name";

            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                products.Add(new Product
                {
                    ProductId = (int)reader["ProductId"],
                    Name = reader["Name"].ToString(),
                    Description = reader["Description"].ToString(),
                    Price = (decimal)reader["Price"],
                    StockQuantity = (int)reader["StockQuantity"],
                    CategoryId = reader["CategoryId"] as int? ?? 0,
                    IsAvailable = (bool)reader["IsAvailable"],
                    CategoryName = reader["CategoryName"].ToString()
                });
            }

            return products;
        }

        // Получение товара по ID
        public Product GetProductById(int productId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT p.*, c.Name as CategoryName 
                FROM Products p 
                LEFT JOIN Categories c ON p.CategoryId = c.CategoryId 
                WHERE p.ProductId = @productId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@productId", productId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Product
                {
                    ProductId = (int)reader["ProductId"],
                    Name = reader["Name"].ToString(),
                    Description = reader["Description"].ToString(),
                    Price = (decimal)reader["Price"],
                    StockQuantity = (int)reader["StockQuantity"],
                    CategoryId = reader["CategoryId"] as int? ?? 0,
                    IsAvailable = (bool)reader["IsAvailable"],
                    CategoryName = reader["CategoryName"].ToString()
                };
            }

            return null;
        }

        // Добавление товара в корзину
        public bool AddToCart(int userId, int productId, int quantity = 1)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Проверяем, есть ли уже такой товар в корзине
                var checkQuery = "SELECT * FROM Cart WHERE UserId = @userId AND ProductId = @productId";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@userId", userId);
                checkCommand.Parameters.AddWithValue("@productId", productId);

                var existingItem = checkCommand.ExecuteScalar();

                if (existingItem != null)
                {
                    // Обновляем количество
                    var updateQuery = "UPDATE Cart SET Quantity = Quantity + @quantity WHERE UserId = @userId AND ProductId = @productId";
                    using var updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@quantity", quantity);
                    updateCommand.Parameters.AddWithValue("@userId", userId);
                    updateCommand.Parameters.AddWithValue("@productId", productId);
                    return updateCommand.ExecuteNonQuery() > 0;
                }
                else
                {
                    // Добавляем новый товар
                    var insertQuery = "INSERT INTO Cart (UserId, ProductId, Quantity) VALUES (@userId, @productId, @quantity)";
                    using var insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@userId", userId);
                    insertCommand.Parameters.AddWithValue("@productId", productId);
                    insertCommand.Parameters.AddWithValue("@quantity", quantity);
                    return insertCommand.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Получение корзины пользователя
        public List<CartItem> GetUserCart(int userId)
        {
            var cartItems = new List<CartItem>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT c.*, p.Name as ProductName, p.Price 
                FROM Cart c 
                JOIN Products p ON c.ProductId = p.ProductId 
                WHERE c.UserId = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                cartItems.Add(new CartItem
                {
                    CartId = (int)reader["CartId"],
                    UserId = (int)reader["UserId"],
                    ProductId = (int)reader["ProductId"],
                    ProductName = reader["ProductName"].ToString(),
                    Quantity = (int)reader["Quantity"],
                    Price = (decimal)reader["Price"]
                });
            }

            return cartItems;
        }

        // Удаление товара из корзины
        public bool RemoveFromCart(int cartId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = "DELETE FROM Cart WHERE CartId = @cartId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@cartId", cartId);

            return command.ExecuteNonQuery() > 0;
        }

        // Очистка корзины
        public bool ClearCart(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = "DELETE FROM Cart WHERE UserId = @userId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            return command.ExecuteNonQuery() > 0;
        }

        // Получение всех ПВЗ
        public List<PickupPoint> GetAllPickupPoints()
        {
            var points = new List<PickupPoint>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT * FROM PickupPoints WHERE IsActive = 1 ORDER BY City, Address";
            using var command = new SqlCommand(query, connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                points.Add(new PickupPoint
                {
                    PointId = (int)reader["PointId"],
                    Address = reader["Address"].ToString(),
                    City = reader["City"].ToString(),
                    WorkingHours = reader["WorkingHours"].ToString(),
                    IsActive = (bool)reader["IsActive"]
                });
            }

            return points;
        }

        // Создание заказа
        public int CreateOrder(int userId, decimal totalAmount, int pickupPointId, List<CartItem> cartItems)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Создаем заказ
                var orderQuery = @"
                    INSERT INTO Orders (UserId, TotalAmount, PickupPointId) 
                    OUTPUT INSERTED.OrderId
                    VALUES (@userId, @totalAmount, @pickupPointId)";

                using var orderCommand = new SqlCommand(orderQuery, connection, transaction);
                orderCommand.Parameters.AddWithValue("@userId", userId);
                orderCommand.Parameters.AddWithValue("@totalAmount", totalAmount);
                orderCommand.Parameters.AddWithValue("@pickupPointId", pickupPointId);

                var orderId = (int)orderCommand.ExecuteScalar();

                // Добавляем товары в заказ
                foreach (var item in cartItems)
                {
                    var itemQuery = @"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price) 
                        VALUES (@orderId, @productId, @quantity, @price)";

                    using var itemCommand = new SqlCommand(itemQuery, connection, transaction);
                    itemCommand.Parameters.AddWithValue("@orderId", orderId);
                    itemCommand.Parameters.AddWithValue("@productId", item.ProductId);
                    itemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("@price", item.Price);
                    itemCommand.ExecuteNonQuery();

                    // Обновляем количество товара на складе
                    var updateStockQuery = "UPDATE Products SET StockQuantity = StockQuantity - @quantity WHERE ProductId = @productId";
                    using var updateCommand = new SqlCommand(updateStockQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                    updateCommand.Parameters.AddWithValue("@productId", item.ProductId);
                    updateCommand.ExecuteNonQuery();
                }

                // Очищаем корзину
                var clearCartQuery = "DELETE FROM Cart WHERE UserId = @userId";
                using var clearCommand = new SqlCommand(clearCartQuery, connection, transaction);
                clearCommand.Parameters.AddWithValue("@userId", userId);
                clearCommand.ExecuteNonQuery();

                transaction.Commit();
                return orderId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        // Получение заказов пользователя
        public List<Order> GetUserOrders(int userId, string sortBy = "newest")
        {
            var orders = new List<Order>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var orderByClause = sortBy.ToLower() switch
            {
                "oldest" => "ORDER BY OrderDate ASC",
                _ => "ORDER BY OrderDate DESC"
            };

            var query = $@"
                SELECT o.*, pp.Address as PickupPointAddress 
                FROM Orders o 
                JOIN PickupPoints pp ON o.PickupPointId = pp.PointId 
                WHERE o.UserId = @userId 
                {orderByClause}";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                orders.Add(new Order
                {
                    OrderId = (int)reader["OrderId"],
                    UserId = (int)reader["UserId"],
                    OrderDate = (DateTime)reader["OrderDate"],
                    TotalAmount = (decimal)reader["TotalAmount"],
                    Status = reader["Status"].ToString(),
                    PickupPointId = (int)reader["PickupPointId"],
                    PickupPointAddress = reader["PickupPointAddress"].ToString()
                });
            }

            return orders;
        }

        // Получение деталей заказа
        public List<OrderItem> GetOrderItems(int orderId)
        {
            var items = new List<OrderItem>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT oi.*, p.Name as ProductName 
                FROM OrderItems oi 
                JOIN Products p ON oi.ProductId = p.ProductId 
                WHERE oi.OrderId = @orderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", orderId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new OrderItem
                {
                    OrderItemId = (int)reader["OrderItemId"],
                    OrderId = (int)reader["OrderId"],
                    ProductId = (int)reader["ProductId"],
                    ProductName = reader["ProductName"].ToString(),
                    Quantity = (int)reader["Quantity"],
                    Price = (decimal)reader["Price"]
                });
            }

            return items;
        }

        // Проверка существования пользователя
        public bool UserExists(string username, string email)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT COUNT(*) FROM Users WHERE Username = @username OR Email = @email";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);

            return (int)command.ExecuteScalar() > 0;
        }
    }
}