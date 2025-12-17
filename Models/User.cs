namespace Lab1.Models;

public class User
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public decimal Balance { get; set; }
    public List<int> Orders { get; set; } = new();
}

