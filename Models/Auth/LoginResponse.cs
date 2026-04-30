namespace mysystem_user_api.Models.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public AuthUserDto User { get; set; } = new();
    }

    public class AuthUserDto
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public List<string> Roles { get; set; } = [];
    }
}
