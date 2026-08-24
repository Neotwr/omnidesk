namespace OmniDesk.Models
{
    public class ServiceAccountCredentials
    {
        public required string Usuario { get; set; }
        public required string Senha { get; set; }
        public bool LembrarSenha { get; set; } = true;
    }
}
