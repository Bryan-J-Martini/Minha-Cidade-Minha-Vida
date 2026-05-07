using MCMV.Data;
using MCMV.Logical;
using MCMV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Mysqlx.Expr;
using System.Text.RegularExpressions;

namespace MCMV.Controllers
{
    public class HomeController : Controller
    {
        private readonly LoginService _loginService;
        private readonly RegisterService _registerService;
        private readonly DonationService _donationService;
        private readonly AlteracoesService _alteracoesService;

        public HomeController(LoginService loginService, RegisterService registerService, DonationService donationService, AlteracoesService alteracoesService)
        {
            _loginService = loginService;
            _registerService = registerService;
            _donationService = donationService;
            _alteracoesService = alteracoesService;
        }

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string documento, string senha)
        {
            if (string.IsNullOrEmpty(documento))
            {
                ViewBag.Erro = "Por favor, preencha o campo de documento.";
                return View();
            }

            string docLimpo = new string(documento.Where(char.IsDigit).ToArray());

            bool valido = _loginService.ValidarLogin(docLimpo, senha);

            if (valido)
            {
                string tipo = _loginService.ObterTipoUsuario(docLimpo);
                //aqui está o documento guardado na sessão
                HttpContext.Session.SetString("Documento", docLimpo);

                return tipo == "CPF" ? RedirectToAction("IndexUsuario") : RedirectToAction("IndexInstituicao");
            }

            ViewBag.Erro = "CPF/CNPJ ou senha inválidos";
            return View();
        }

        // --- CADASTRO ---

        [HttpGet]
        public IActionResult Cadastro() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Cadastro(string user, string identific, bool isInstit, string email, string senha, string confirmarSenha, IFormFile documentoInstituicao)
        {
            // 1. Limpeza radical: remove tudo que não for número
            string docLimpo = new string(identific.Where(char.IsDigit).ToArray());

            // 2. Validação simplificada de tamanho (CPF=11, CNPJ=14)
            if (ValidadoresService.ValidarDocumento(docLimpo) == false)
            {
                ViewBag.Erro = "O documento deve ter 11 dígitos (CPF) ou 14 dígitos (CNPJ).";
                return View("Cadastro");
            }

            bool instituicaoVerificada = await ValidadoresService.VerificarInstituicao(docLimpo);

            // 4. Verificar duplicidade e salvar
            if (_registerService.UsuarioExiste(docLimpo))
            {
                ViewBag.Erro = "Este CPF ou CNPJ já está cadastrado.";
                return View("Cadastro");
            }

            _registerService.CriarUsuario(user, senha, email, docLimpo, instituicaoVerificada);

            TempData["MensagemSucesso"] = "Cadastro realizado com sucesso!";
            return RedirectToAction("Login");
        }

        // --- OUTRAS ROTAS ---
        public IActionResult IndexUsuario() => View();



        //----Instituição----
        [HttpGet]
        public IActionResult IndexInstituicao() => View();


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FazerVerificacao(IFormFile imagem)
        {
            //string cnpjlimpo = new string((cnpj ?? "").Where(char.IsDigit).ToArray());
            string urlModal = Url.Action(nameof(IndexInstituicao), "Home") + "#modal-verificacao";
            var docSessao = HttpContext.Session.GetString("Documento");

            if(string.IsNullOrWhiteSpace(docSessao))
                return RedirectToAction("Login");


            string cnpj = new string(docSessao.Where(char.IsDigit).ToArray());

            if (cnpj.Length != 14)
            {
                TempData["VerifTipo"] = "erro";
                TempData["VerifMensagem"] = "Apenas instituições (CNPJ) podem se verificar.";
                return Redirect(urlModal);
            }


            //2) verifica se existe e se já está verificada
            bool? jaVerificada = _alteracoesService.JaEstaVerificada(cnpj);

            if (jaVerificada == null)
            {
                TempData["VerifTipo"] = "erro";
                TempData["VerifMensagem"] = "CNPJ não encontrado/cadastrado.";
                return Redirect(urlModal);
            }

            if (jaVerificada == true)
            {
                TempData["VerifTipo"] = "aviso"; 
                TempData["VerifMensagem"] = "Esta instituição já está verificada ✅";
                return Redirect(urlModal);
            }

            if (imagem == null || imagem.Length == 0)
            {
                TempData["VerifTipo"] = "erro";
                TempData["VerifMensagem"] = "Imagem não enviada";
                return Redirect(urlModal);
            }

            bool ok = _alteracoesService.mudarValidacaoInstituicao(cnpj);

            if (!ok)
            {
                TempData["VerifTipo"] = "erro";
                TempData["VerifMensagem"] = "Não foi possível atualizar. Verifique se o CNPJ está cadastrado.";
                return Redirect(urlModal);
            }


            TempData["VerifTipo"] = ok ? "sucesso" : "erro";
            TempData["VerifMensagem"] = ok ? "Verificação realizada com sucesso!" : "Não foi possível atualizar a verificação.";

            return Redirect(urlModal);
        }


        public IActionResult VerInstituicoes()
        {
            var instituicoes = _registerService.ListarInstituicoes();
            return View(instituicoes);
        }

        [HttpGet]
        public IActionResult PrecisaDeDoacao() => View();

        [HttpPost]
        public IActionResult EnviarSolicitacao(SolicitacaoDoacao solicitacao)
        {
            if (ModelState.IsValid)
            {
                _donationService.SalvarSolicitacao(solicitacao);
                TempData["MensagemSucesso"] = "Solicitação enviada com sucesso!";
                return RedirectToAction("IndexUsuario");
            }
            return View("PrecisaDeDoacao", solicitacao);
        }

        [HttpGet]
        public IActionResult FacaUmaDoacao() => View();

        [HttpPost]
        public IActionResult EnviarDoacao(FazerUmaDoacao doacao)
        {
            if (ModelState.IsValid)
            {
                _donationService.SalvarOfertaDoacao(doacao);
                TempData["MensagemSucesso"] = "Oferta de doação enviada com sucesso!";
                return RedirectToAction("IndexUsuario");
            }
            return View("FacaUmaDoacao", doacao);
        }

        //Saindo da Sessão

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}