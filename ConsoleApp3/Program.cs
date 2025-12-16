using System;
using System.Collections.Generic;
using System.Linq;
using GG_MOW_Marketplace.Models;

namespace GG_MOW_Marketplace
{
    class Program
    {
        private static DatabaseHelper _dbHelper;
        private static User _currentUser;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Подключение к базе данных
            _dbHelper = new DatabaseHelper(@"Server=(localdb)\MSSQLLocalDB;Database=GG_MOW_Marketplace;Integrated Security=true;");

            ShowMainMenu();
        }

        static void ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════╗");
                Console.WriteLine("║         ДОБРО ПОЖАЛОВАТЬ В GG.MOW             ║");
                Console.WriteLine("║       Маркетплейс нового поколения           ║");
                Console.WriteLine("╠════════════════════════════════════════════════╣");
                Console.WriteLine("║ 1. Просмотр товаров                           ║");
                Console.WriteLine("║ 2. Регистрация                                ║");
                Console.WriteLine("║ 3. Вход в аккаунт                             ║");
                if (_currentUser != null)
                {
                    Console.WriteLine($"║ Текущий пользователь: {_currentUser.Username,-24} ║");
                    Console.WriteLine("║ 4. Корзина товаров                           ║");
                    Console.WriteLine("║ 5. Мои заказы                                ║");
                    Console.WriteLine("║ 6. Выйти из аккаунта                         ║");
                }
                Console.WriteLine("║ 0. Выход из программы                         ║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.Write("\nВыберите действие: ");

                var choice = Console.ReadLine();

                if (_currentUser == null)
                {
                    switch (choice)
                    {
                        case "1":
                            BrowseProducts();
                            break;
                        case "2":
                            Register();
                            break;
                        case "3":
                            Login();
                            break;
                        case "0":
                            Console.WriteLine("\nСпасибо за использование GG.MOW!");
                            return;
                        default:
                            Console.WriteLine("\nНеверный выбор. Нажмите любую клавишу...");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    switch (choice)
                    {
                        case "1":
                            BrowseProducts();
                            break;
                        case "4":
                            ShowCart();
                            break;
                        case "5":
                            ShowOrders();
                            break;
                        case "6":
                            _currentUser = null;
                            Console.WriteLine("\nВы вышли из аккаунта. Нажмите любую клавишу...");
                            Console.ReadKey();
                            break;
                        case "0":
                            Console.WriteLine("\nСпасибо за использование GG.MOW!");
                            return;
                        default:
                            Console.WriteLine("\nНеверный выбор. Нажмите любую клавишу...");
                            Console.ReadKey();
                            break;
                    }
                }
            }
        }

        static void BrowseProducts()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                   КАТАЛОГ ТОВАРОВ                     ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

                var products = _dbHelper.GetAllProducts();

                if (products.Count == 0)
                {
                    Console.WriteLine("Товары временно отсутствуют.");
                }
                else
                {
                    Console.WriteLine($"{"ID",-5} {"Название",-30} {"Цена",-15} {"В наличии",-10} Категория");
                    Console.WriteLine(new string('═', 90));

                    foreach (var product in products)
                    {
                        Console.WriteLine($"{product.ProductId,-5} {product.Name,-30} {product.Price,-15:C2} {product.StockQuantity,-10} {product.CategoryName}");

                        if (product.Description.Length > 0)
                        {
                            Console.WriteLine($"     Описание: {product.Description}");
                        }
                        Console.WriteLine();
                    }
                }

                if (_currentUser != null)
                {
                    Console.WriteLine("\n════════════════════════════════════════════════════════");
                    Console.WriteLine("1. Добавить товар в корзину");
                    Console.WriteLine("2. Вернуться в главное меню");
                    Console.Write("Выберите действие: ");

                    var choice = Console.ReadLine();

                    if (choice == "1")
                    {
                        Console.Write("\nВведите ID товара для добавления в корзину: ");
                        if (int.TryParse(Console.ReadLine(), out int productId))
                        {
                            Console.Write("Введите количество (по умолчанию 1): ");
                            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity < 1)
                                quantity = 1;

                            if (_dbHelper.AddToCart(_currentUser.UserId, productId, quantity))
                            {
                                Console.WriteLine("Товар добавлен в корзину!");
                            }
                            else
                            {
                                Console.WriteLine("Ошибка при добавлении товара в корзину.");
                            }
                            Console.WriteLine("Нажмите любую клавишу...");
                            Console.ReadKey();
                        }
                    }
                    else if (choice == "2")
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("\n════════════════════════════════════════════════════════");
                    Console.WriteLine("Для покупок необходимо войти в систему.");
                    Console.WriteLine("Нажмите любую клавишу для возврата...");
                    Console.ReadKey();
                    break;
                }
            }
        }

        static void Register()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║               РЕГИСТРАЦИЯ                     ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            Console.Write("Введите имя пользователя: ");
            var username = Console.ReadLine();

            Console.Write("Введите email: ");
            var email = Console.ReadLine();

            Console.Write("Введите пароль: ");
            var password = Console.ReadLine();

            Console.Write("Подтвердите пароль: ");
            var confirmPassword = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("\nВсе поля обязательны для заполнения!");
                Console.ReadKey();
                return;
            }

            if (password != confirmPassword)
            {
                Console.WriteLine("\nПароли не совпадают!");
                Console.ReadKey();
                return;
            }

            if (_dbHelper.UserExists(username, email))
            {
                Console.WriteLine("\nПользователь с таким именем или email уже существует!");
                Console.ReadKey();
                return;
            }

            if (_dbHelper.RegisterUser(username, password, email))
            {
                Console.WriteLine("\nРегистрация успешна! Теперь вы можете войти в систему.");
            }
            else
            {
                Console.WriteLine("\nОшибка при регистрации. Возможно, пользователь уже существует.");
            }

            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void Login()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║                   ВХОД                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            Console.Write("Имя пользователя: ");
            var username = Console.ReadLine();

            Console.Write("Пароль: ");
            var password = Console.ReadLine();

            _currentUser = _dbHelper.Login(username, password);

            if (_currentUser != null)
            {
                Console.WriteLine($"\nДобро пожаловать, {_currentUser.Username}!");
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
            }
            else
            {
                Console.WriteLine("\nНеверное имя пользователя или пароль!");
                Console.WriteLine("Нажмите любую клавишу...");
            }

            Console.ReadKey();
        }

        static void ShowCart()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                       КОРЗИНА                         ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

                var cartItems = _dbHelper.GetUserCart(_currentUser.UserId);

                if (cartItems.Count == 0)
                {
                    Console.WriteLine("Ваша корзина пуста.");
                }
                else
                {
                    decimal total = 0;
                    Console.WriteLine($"{"ID корзины",-12} {"Товар",-30} {"Цена",-10} {"Кол-во",-8} {"Сумма",-12}");
                    Console.WriteLine(new string('═', 82));

                    foreach (var item in cartItems)
                    {
                        Console.WriteLine($"{item.CartId,-12} {item.ProductName,-30} {item.Price,-10:C2} {item.Quantity,-8} {item.Total,-12:C2}");
                        total += item.Total;
                    }

                    Console.WriteLine(new string('═', 82));
                    Console.WriteLine($"{"ИТОГО:",60} {total,12:C2}");
                }

                Console.WriteLine("\n════════════════════════════════════════════════════════");
                Console.WriteLine("1. Удалить товар из корзины");
                Console.WriteLine("2. Оформить заказ");
                Console.WriteLine("3. Очистить корзину");
                Console.WriteLine("4. Вернуться в главное меню");
                Console.Write("Выберите действие: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        if (cartItems.Count > 0)
                        {
                            Console.Write("Введите ID товара в корзине для удаления: ");
                            if (int.TryParse(Console.ReadLine(), out int cartId))
                            {
                                if (_dbHelper.RemoveFromCart(cartId))
                                    Console.WriteLine("Товар удален из корзины!");
                                else
                                    Console.WriteLine("Ошибка при удалении товара.");
                                Console.ReadKey();
                            }
                        }
                        break;
                    case "2":
                        if (cartItems.Count > 0)
                        {
                            CreateOrder(cartItems);
                        }
                        break;
                    case "3":
                        if (cartItems.Count > 0)
                        {
                            Console.Write("Вы уверены, что хотите очистить корзину? (y/n): ");
                            if (Console.ReadLine().ToLower() == "y")
                            {
                                if (_dbHelper.ClearCart(_currentUser.UserId))
                                {
                                    Console.WriteLine("Корзина очищена!");
                                    Console.ReadKey();
                                }
                            }
                        }
                        break;
                    case "4":
                        return;
                }
            }
        }

        static void CreateOrder(List<CartItem> cartItems)
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 ОФОРМЛЕНИЕ ЗАКАЗА                     ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            // Выбор ПВЗ
            var pickupPoints = _dbHelper.GetAllPickupPoints();

            if (pickupPoints.Count == 0)
            {
                Console.WriteLine("Нет доступных пунктов выдачи.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Доступные пункты выдачи заказов (ПВЗ):\n");
            for (int i = 0; i < pickupPoints.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {pickupPoints[i]}");
            }

            Console.Write("\nВыберите номер ПВЗ: ");
            if (!int.TryParse(Console.ReadLine(), out int pointIndex) || pointIndex < 1 || pointIndex > pickupPoints.Count)
            {
                Console.WriteLine("Неверный выбор ПВЗ.");
                Console.ReadKey();
                return;
            }

            var selectedPoint = pickupPoints[pointIndex - 1];

            Console.WriteLine($"\nВыбран ПВЗ: {selectedPoint}");

            // Подтверждение заказа
            Console.WriteLine("\n════════════════════════════════════════════════════════");
            decimal total = cartItems.Sum(item => item.Total);
            Console.WriteLine($"Общая сумма заказа: {total:C2}");

            Console.Write("\nПодтвердить заказ? (y/n): ");
            if (Console.ReadLine().ToLower() == "y")
            {
                try
                {
                    int orderId = _dbHelper.CreateOrder(_currentUser.UserId, total, selectedPoint.PointId, cartItems);
                    Console.WriteLine($"\nЗаказ #{orderId} успешно оформлен!");
                    Console.WriteLine($"Заберите заказ по адресу: {selectedPoint.Address}");
                    Console.WriteLine($"Часы работы: {selectedPoint.WorkingHours}");
                }
                catch (Exception)
                {
                    Console.WriteLine("\nОшибка при оформлении заказа. Проверьте наличие товаров на складе.");
                }
            }
            else
            {
                Console.WriteLine("\nОформление заказа отменено.");
            }

            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void ShowOrders()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                     МОИ ЗАКАЗЫ                        ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

                Console.WriteLine("Сортировка:");
                Console.WriteLine("1. Сначала новые");
                Console.WriteLine("2. Сначала старые");
                Console.Write("Выберите сортировку: ");

                var sortChoice = Console.ReadLine();
                var sortBy = sortChoice == "2" ? "oldest" : "newest";

                var orders = _dbHelper.GetUserOrders(_currentUser.UserId, sortBy);

                if (orders.Count == 0)
                {
                    Console.WriteLine("\nУ вас пока нет заказов.");
                }
                else
                {
                    Console.WriteLine($"\n{"ID заказа",-10} {"Дата",-20} {"Сумма",-12} {"Статус",-15} ПВЗ");
                    Console.WriteLine(new string('═', 100));

                    foreach (var order in orders)
                    {
                        Console.WriteLine($"{order.OrderId,-10} {order.OrderDate:dd.MM.yyyy HH:mm,-20} {order.TotalAmount,-12:C2} {order.Status,-15} {order.PickupPointAddress}");
                    }

                    Console.WriteLine("\n════════════════════════════════════════════════════════");
                    Console.Write("Введите ID заказа для просмотра деталей (0 для возврата): ");

                    if (int.TryParse(Console.ReadLine(), out int orderId) && orderId > 0)
                    {
                        ShowOrderDetails(orderId);
                    }
                    else if (orderId == 0)
                    {
                        break;
                    }
                }

                Console.WriteLine("\nНажмите любую клавишу для возврата...");
                Console.ReadKey();
                break;
            }
        }

        static void ShowOrderDetails(int orderId)
        {
            Console.Clear();
            Console.WriteLine($"╔════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                ДЕТАЛИ ЗАКАЗА #{orderId,-10}              ║");
            Console.WriteLine($"╚════════════════════════════════════════════════════════╝\n");

            var items = _dbHelper.GetOrderItems(orderId);
            var orders = _dbHelper.GetUserOrders(_currentUser.UserId);
            var order = orders.FirstOrDefault(o => o.OrderId == orderId);

            if (order != null)
            {
                Console.WriteLine($"Дата заказа: {order.OrderDate:dd.MM.yyyy HH:mm}");
                Console.WriteLine($"Статус: {order.Status}");
                Console.WriteLine($"ПВЗ: {order.PickupPointAddress}");
                Console.WriteLine($"Общая сумма: {order.TotalAmount:C2}");
            }

            if (items.Count > 0)
            {
                Console.WriteLine("\nСостав заказа:");
                Console.WriteLine(new string('═', 70));
                Console.WriteLine($"{"Товар",-30} {"Цена",-12} {"Кол-во",-8} {"Сумма",-12}");
                Console.WriteLine(new string('═', 70));

                foreach (var item in items)
                {
                    Console.WriteLine($"{item.ProductName,-30} {item.Price,-12:C2} {item.Quantity,-8} {item.Total,-12:C2}");
                }
            }

            Console.WriteLine("\nНажмите любую клавишу для возврата...");
            Console.ReadKey();
        }
    }
}