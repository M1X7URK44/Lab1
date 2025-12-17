namespace Lab1.Models;

public class ProjectState
{
    public List<User> Users { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public Dictionary<string, string> Sessions { get; set; } = new(); // sessionId -> login
    private int _nextOrderId = 1;

    public ProjectState()
    {
        InitializeData();
    }

    private void InitializeData()
    {
        // Инициализация пользователей
        Users.Add(new User
        {
            Login = "admin",
            Password = "admin",
            IsAdmin = true,
            Balance = 0
        });

        Users.Add(new User
        {
            Login = "user",
            Password = "1234",
            IsAdmin = false,
            Balance = 100
        });

        // Инициализация товаров
        Products.Add(new Product
        {
            Id = 1,
            Name = "Ноутбук",
            Description = "Мощный ноутбук для работы и игр",
            Price = 50000,
            Stock = 5
        });

        Products.Add(new Product
        {
            Id = 2,
            Name = "Смартфон",
            Description = "Современный смартфон с отличной камерой",
            Price = 30000,
            Stock = 10
        });

        Products.Add(new Product
        {
            Id = 3,
            Name = "Наушники",
            Description = "Беспроводные наушники с шумоподавлением",
            Price = 5000,
            Stock = 20
        });

        Products.Add(new Product
        {
            Id = 4,
            Name = "Клавиатура",
            Description = "Механическая клавиатура для геймеров",
            Price = 3500,
            Stock = 15
        });
    }

    public int GetNextOrderId()
    {
        return _nextOrderId++;
    }
}

