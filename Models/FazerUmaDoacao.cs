namespace MCMV.Models
{
    public class FazerUmaDoacao
    {
        public int Id { get; set; }
        public string? Instituicao { get; set; }
        public string? OQueDesejaDoar { get; set; }
        public string? Campanha { get; set; } 
        public string? EstadoItem { get; set; }
        public string? Contato { get; set; }
    }

    public class HistoricoDoacaoViewModel
    {
        public string DocumentoDoador { get; set; } = "";
        public string Item { get; set; } = "";
        public string Quantidade { get; set; } = "";
        public string Unidade { get; set; } = "";
        public string? Campanha { get; set; }
    }
}
