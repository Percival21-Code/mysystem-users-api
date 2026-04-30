namespace mysystem_user_api.Models.Auth;

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}