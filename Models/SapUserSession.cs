namespace RemoteAccessUtil.Models
{
    public class SapUserSession
    {
        public required string DestinationName { get; set; }
        public required string Client { get; set; }
        public required string User { get; set; }
        public required string Password { get; set; }
        public string Language { get; set; } = "PT";
        public bool LembrarSenha { get; set; }
    }
}
