namespace MCMV.Models
{
    public class UserViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;


        public bool IsVerificada { get; set; }
    }
}