namespace MCMV.Models
{
    public class InstituicaoTransparencia
    {
        public string Nome { get; set; }
        public string Documento { get; set; }
        public string Email { get; set; }

        // Para os cálculos
        public decimal TotalArrecadado { get; set; }
        public int TotalItensRecebidos { get; set; }
    }
}