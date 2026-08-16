namespace RemoteAccessUtil.Models
{
    public class ComparacaoGruposResultado
    {
        public required string UsuarioAlvo { get; set; }
        public required string UsuarioReferencia { get; set; }
        public required List<Grupos> GruposFaltantes { get; set; }

        public string TituloJanela => $"Grupos que *{UsuarioReferencia}* possui e faltam em *{UsuarioAlvo}*";
        public string TextoExplicativo => $"Estes são os grupos que faltam para o user {UsuarioAlvo}";
    }
}
