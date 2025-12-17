using Lab1.Models;

namespace Lab1.Controllers;

public static class ActionController
{
    public static HttpResponse HandleAction(HttpRequest request, ProjectState state)
    {
        var formData = ParseFormData(request.Body);
        var action = formData.GetValueOrDefault("action", "");

        var currentUser = GetCurrentUser(request, state);
        if (currentUser == null)
        {
            return new HttpResponse
            {
                StatusCode = 401,
                StatusText = "Unauthorized",
                Body = "<html><body><h1>Необходима авторизация</h1><a href=\"/login\">Войти</a></body></html>"
            };
        }

        return action switch
        {
            "buy" => HandleBuy(formData, currentUser, state),
            "add_product" => HandleAddProduct(formData, currentUser, state),
            "update_product" => HandleUpdateProduct(formData, currentUser, state),
            "add_balance" => HandleAddBalance(formData, currentUser, state),
            _ => new HttpResponse
            {
                StatusCode = 400,
                StatusText = "Bad Request",
                Body = "<html><body><h1>Неизвестное действие</h1></body></html>"
            }
        };
    }

    private static HttpResponse HandleBuy(Dictionary<string, string> formData, User user, ProjectState state)
    {
        if (!int.TryParse(formData.GetValueOrDefault("product_id", ""), out var productId) ||
            !int.TryParse(formData.GetValueOrDefault("quantity", "1"), out var quantity))
        {
            return ErrorResponse("Неверные параметры заказа");
        }

        var product = state.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
        {
            return ErrorResponse("Товар не найден");
        }

        if (product.Stock < quantity)
        {
            return ErrorResponse("Недостаточно товара на складе");
        }

        var totalPrice = product.Price * quantity;
        if (user.Balance < totalPrice)
        {
            return ErrorResponse("Недостаточно средств на балансе");
        }

        // Создание заказа
        var order = new Order
        {
            Id = state.GetNextOrderId(),
            UserLogin = user.Login,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = quantity,
                    Price = product.Price
                }
            },
            TotalPrice = totalPrice
        };

        state.Orders.Add(order);
        user.Orders.Add(order.Id);
        user.Balance -= totalPrice;
        product.Stock -= quantity;

        return SuccessResponse($"Заказ #{order.Id} успешно создан! Сумма: {totalPrice:C}");
    }

    private static HttpResponse HandleAddProduct(Dictionary<string, string> formData, User user, ProjectState state)
    {
        if (!user.IsAdmin)
        {
            return ErrorResponse("Только администратор может добавлять товары");
        }

        if (!decimal.TryParse(formData.GetValueOrDefault("price", ""), out var price) ||
            !int.TryParse(formData.GetValueOrDefault("stock", ""), out var stock))
        {
            return ErrorResponse("Неверные параметры товара");
        }

        var name = formData.GetValueOrDefault("name", "");
        var description = formData.GetValueOrDefault("description", "");

        if (string.IsNullOrEmpty(name))
        {
            return ErrorResponse("Название товара обязательно");
        }

        var newId = state.Products.Any() ? state.Products.Max(p => p.Id) + 1 : 1;
        var product = new Product
        {
            Id = newId,
            Name = name,
            Description = description,
            Price = price,
            Stock = stock
        };

        state.Products.Add(product);

        return SuccessResponse($"Товар '{name}' успешно добавлен!");
    }

    private static HttpResponse HandleUpdateProduct(Dictionary<string, string> formData, User user, ProjectState state)
    {
        if (!user.IsAdmin)
        {
            return ErrorResponse("Только администратор может изменять товары");
        }

        if (!int.TryParse(formData.GetValueOrDefault("product_id", ""), out var productId))
        {
            return ErrorResponse("Неверный ID товара");
        }

        var product = state.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
        {
            return ErrorResponse("Товар не найден");
        }

        if (formData.ContainsKey("name"))
        {
            product.Name = formData["name"];
        }
        if (formData.ContainsKey("description"))
        {
            product.Description = formData["description"];
        }
        if (formData.ContainsKey("price") && decimal.TryParse(formData["price"], out var price))
        {
            product.Price = price;
        }
        if (formData.ContainsKey("stock") && int.TryParse(formData["stock"], out var stock))
        {
            product.Stock = stock;
        }

        return SuccessResponse($"Товар '{product.Name}' успешно обновлён!");
    }

    private static HttpResponse HandleAddBalance(Dictionary<string, string> formData, User user, ProjectState state)
    {
        if (!user.IsAdmin)
        {
            return ErrorResponse("Только администратор может пополнять баланс");
        }

        var targetLogin = formData.GetValueOrDefault("target_login", "");
        if (string.IsNullOrEmpty(targetLogin))
        {
            return ErrorResponse("Не указан пользователь для пополнения баланса");
        }

        var targetUser = state.Users.FirstOrDefault(u => u.Login == targetLogin);
        if (targetUser == null)
        {
            return ErrorResponse("Пользователь не найден");
        }

        if (!decimal.TryParse(formData.GetValueOrDefault("amount", ""), out var amount) || amount <= 0)
        {
            return ErrorResponse("Неверная сумма пополнения");
        }

        targetUser.Balance += amount;

        return SuccessResponse($"Баланс пользователя {targetLogin} пополнен на {amount:C}. Текущий баланс: {targetUser.Balance:C}");
    }

    private static User? GetCurrentUser(HttpRequest request, ProjectState state)
    {
        if (request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            var sessionId = ExtractSessionId(cookieHeader);
            if (!string.IsNullOrEmpty(sessionId) && state.Sessions.TryGetValue(sessionId, out var login))
            {
                return state.Users.FirstOrDefault(u => u.Login == login);
            }
        }
        return null;
    }

    private static string? ExtractSessionId(string cookieHeader)
    {
        var cookies = cookieHeader.Split(';');
        foreach (var cookie in cookies)
        {
            var parts = cookie.Trim().Split('=');
            if (parts.Length == 2 && parts[0].Trim() == "sessionId")
            {
                return parts[1].Trim();
            }
        }
        return null;
    }

    private static Dictionary<string, string> ParseFormData(string body)
    {
        var result = new Dictionary<string, string>();
        var pairs = body.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                // Заменяем + на пробелы перед декодированием (application/x-www-form-urlencoded формат)
                var key = parts[0].Replace('+', ' ');
                var value = parts[1].Replace('+', ' ');
                result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
        }
        return result;
    }

    private static HttpResponse SuccessResponse(string message)
    {
        var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""refresh"" content=""3;url=/status"">
    <title>Успех</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            text-align: center;
            padding: 50px;
            background-color: #f5f5f5;
        }}
        .success {{
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
            display: inline-block;
        }}
    </style>
</head>
<body>
    <div class=""success"">
        <h1>Успешно!</h1>
        <p>{message}</p>
        <p>Перенаправление на страницу статуса...</p>
    </div>
</body>
</html>";

        return new HttpResponse
        {
            StatusCode = 200,
            StatusText = "OK",
            Body = html
        };
    }

    private static HttpResponse ErrorResponse(string message)
    {
        var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Ошибка</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            text-align: center;
            padding: 50px;
            background-color: #f5f5f5;
        }}
        .error {{
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
            display: inline-block;
        }}
        a {{
            color: #333;
            text-decoration: none;
            display: inline-block;
            margin-top: 20px;
            padding: 10px 20px;
            background-color: #333;
            color: white;
            border-radius: 3px;
        }}
    </style>
</head>
<body>
    <div class=""error"">
        <h1>Ошибка</h1>
        <p>{message}</p>
        <a href=""/status"">Вернуться</a>
    </div>
</body>
</html>";

        return new HttpResponse
        {
            StatusCode = 400,
            StatusText = "Bad Request",
            Body = html
        };
    }
}

