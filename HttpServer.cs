using System.Net;
using System.Net.Sockets;
using System.Text;
using Lab1.Models;

namespace Lab1;

public class HttpServer
{
    private readonly TcpListener _listener;
    private readonly Router _router;
    private readonly ProjectState _state;
    private bool _isRunning;

    public HttpServer(int port, ProjectState state, Router router)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _router = router;
        _state = state;
    }

    public void Start()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine($"Сервер запущен на порту {((IPEndPoint)_listener.LocalEndpoint).Port}");
        Console.WriteLine("Ожидание подключений...");

        while (_isRunning)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
                }
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        Console.WriteLine("Сервер остановлен");
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var request = ReadRequest(stream);

                if (request != null)
                {
                    Console.WriteLine($"\n=== Запрос ===");
                    Console.WriteLine($"{request.Method} {request.Path}");
                    Console.WriteLine($"Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
                    if (!string.IsNullOrEmpty(request.Body))
                    {
                        Console.WriteLine($"Body: {request.Body}");
                    }

                    var response = _router.HandleRequest(request, _state);
                    SendResponse(stream, response);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке клиента: {ex.Message}");
        }
    }

    private HttpRequest? ReadRequest(NetworkStream stream)
    {
        try
        {
            var buffer = new byte[4096];
            var totalBytesRead = 0;
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            if (bytesRead == 0)
                return null;

            totalBytesRead = bytesRead;
            var requestText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            // Парсим заголовки для получения Content-Length
            var headerEnd = requestText.IndexOf("\r\n\r\n");
            if (headerEnd == -1)
                return null;

            var headersText = requestText.Substring(0, headerEnd);
            var contentLength = 0;
            
            foreach (var line in headersText.Split("\r\n"))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var lengthStr = line.Substring("Content-Length:".Length).Trim();
                    int.TryParse(lengthStr, out contentLength);
                    break;
                }
            }

            // Если есть тело запроса, читаем его полностью
            if (contentLength > 0)
            {
                // Вычисляем позицию начала тела в байтах
                var headerEndBytes = Encoding.UTF8.GetByteCount(requestText.Substring(0, headerEnd + 4));
                var bodyBytesRead = totalBytesRead - headerEndBytes;
                
                if (bodyBytesRead < contentLength)
                {
                    var remainingBytes = contentLength - bodyBytesRead;
                    var bodyBuffer = new byte[remainingBytes];
                    var bodyBytesReceived = 0;
                    
                    while (bodyBytesReceived < remainingBytes)
                    {
                        var read = stream.Read(bodyBuffer, bodyBytesReceived, remainingBytes - bodyBytesReceived);
                        if (read == 0) break;
                        bodyBytesReceived += read;
                    }
                    
                    if (bodyBytesReceived > 0)
                    {
                        var newBuffer = new byte[totalBytesRead + bodyBytesReceived];
                        Array.Copy(buffer, 0, newBuffer, 0, totalBytesRead);
                        Array.Copy(bodyBuffer, 0, newBuffer, totalBytesRead, bodyBytesReceived);
                        buffer = newBuffer;
                        totalBytesRead += bodyBytesReceived;
                        requestText = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                    }
                }
            }

            return ParseRequest(requestText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении запроса: {ex.Message}");
            return null;
        }
    }

    private HttpRequest? ParseRequest(string requestText)
    {
        var lines = requestText.Split("\r\n");
        if (lines.Length == 0)
            return null;

        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2)
            return null;

        var method = requestLine[0];
        var path = requestLine[1].Split('?')[0]; // Убираем query параметры для простоты
        var headers = new Dictionary<string, string>();
        var body = string.Empty;

        var i = 1;
        // Парсинг заголовков
        while (i < lines.Length && !string.IsNullOrEmpty(lines[i]))
        {
            var headerLine = lines[i];
            var colonIndex = headerLine.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = headerLine.Substring(0, colonIndex).Trim();
                var value = headerLine.Substring(colonIndex + 1).Trim();
                headers[key] = value;
            }
            i++;
        }

        // Парсинг тела запроса
        if (i < lines.Length - 1)
        {
            body = string.Join("\r\n", lines.Skip(i + 1));
        }

        return new HttpRequest
        {
            Method = method,
            Path = path,
            Headers = headers,
            Body = body
        };
    }

    private void SendResponse(NetworkStream stream, HttpResponse response)
    {
        try
        {
            var responseText = BuildResponse(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseText);
            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке ответа: {ex.Message}");
        }
    }

    private string BuildResponse(HttpResponse response)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"HTTP/1.1 {response.StatusCode} {response.StatusText}");
        
        // Добавляем Content-Length если его нет
        if (!response.Headers.ContainsKey("Content-Length"))
        {
            var bodyBytes = Encoding.UTF8.GetByteCount(response.Body);
            response.Headers["Content-Length"] = bodyBytes.ToString();
        }
        
        // Добавляем Connection: close для простоты
        if (!response.Headers.ContainsKey("Connection"))
        {
            response.Headers["Connection"] = "close";
        }
        
        foreach (var header in response.Headers)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }
        
        sb.AppendLine();
        sb.Append(response.Body);
        
        return sb.ToString();
    }
}

public class HttpRequest
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
}

public class HttpResponse
{
    public int StatusCode { get; set; } = 200;
    public string StatusText { get; set; } = "OK";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;

    public HttpResponse()
    {
        Headers["Content-Type"] = "text/html; charset=utf-8";
    }
}

