using System.Collections.Generic;

namespace MCMV.Models
{
    public class MeusDadosViewModel
    {
        public string Nome { get; set; }
        public string Documento { get; set; }
        public string Email { get; set; }

        public int DoacoesEnviadas { get; set; }
        public int CampanhasParticipadas { get; set; }
        public int DoacoesSolicitadas { get; set; }
        public int DoacoesEspontaneas { get; set; }
        public List<string> InstituicoesContatadas { get; set; } = new List<string>();
    }
}