using Microsoft.AspNetCore.Mvc;
using MCMV.Data;
using MCMV.Logical;
using MCMV.Models;
using System.Text.RegularExpressions;

namespace MCMV.Controllers
{
    public class HomeController : Controller
    {
        private readonly LoginService _loginService;
        private readonly RegisterService _registerService;
        private readonly DonationService _donationService;

        public HomeController(LoginService loginService, RegisterService registerService, DonationService donationService)
        {
            _loginService = loginService;
            _registerService = registerService;
            _donationService = donationService;
        }

        // Login
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string documento, string senha)
        {
            bool valido = _loginService.ValidarLogin(documento, senha);

            if (valido)
            {
                string tipo = _loginService.ObterTipoUsuario(documento);

                if (tipo == "CPF")
                {
                    return RedirectToAction("IndexUsuario");
                }
                else if (tipo == "CNPJ")
                {
                    return RedirectToAction("IndexInstituicao");
                }
            }

            ViewBag.Erro = "CPF/CNPJ ou senha inválidos";
            return View();
        }

        //Cadastro
        [HttpGet]
        public IActionResult Cadastro() => View();

        [HttpPost]
        public IActionResult Cadastro(string user, string identific, string email, string senha, string confirmarSenha)
        {
            string padraoEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email ?? "", padraoEmail))
            {
                ViewBag.Erro = "E-mail inválido!";
                return View();
            }

            if (senha != confirmarSenha)
            {
                ViewBag.Erro = "As senhas não coincidem!";
                return View();
            }

            if (!Validador.ValidarDocumento(identific))
            {
                ViewBag.Erro = "CPF ou CNPJ inválido!";
                return View();
            }

            if (_registerService.UsuarioExiste(user))
            {
                ViewBag.Erro = "Este nome de usuário já está em uso.";
                return View();
            }

            _registerService.CriarUsuario(user, senha, email ?? "", identific);

            TempData["MensagemSucesso"] = "Usuário criado com sucesso!";
            return RedirectToAction("Login");
        }

        //tela inicial
        public IActionResult IndexUsuario() => View();

        public IActionResult IndexInstituicao() => View();


        [HttpGet]
        public IActionResult PrecisaDeDoacao()
        {
            return View();
        }

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
        public IActionResult FacaUmaDoacao()
        {
            return View();
        }

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
    }
}