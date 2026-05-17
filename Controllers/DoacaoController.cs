using MCMV.Data;
using MCMV.Logical;
using MCMV.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace MCMV.Controllers
{
    public class DoacaoController : Controller
    {
        private readonly Database _db;
        private readonly IConfiguration _configuration;
        private readonly DonationService _donationService;
        public DoacaoController(Database db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
            _donationService = new DonationService(configuration);
        }

        [HttpGet]
        public IActionResult PrecisaDeDoacao()
        {
            return View("~/Views/Home/PrecisaDeDoacao.cshtml");
        }

        [HttpPost]
        public IActionResult EnviarSolicitacao(SolicitacaoDoacao solicitacao)
        {
            _donationService.SalvarSolicitacao(solicitacao);

            TempData["MensagemSucesso"] = "Solicitação de doação enviada com sucesso!";

            
            return RedirectToAction("PrecisaDeDoacao");
        }


        [HttpGet]
        public IActionResult FazerUmaDoacao()
        {

            var registerService = new RegisterService(_db, _configuration);
            var listaReal = registerService.ListarInstituicoes();

            ViewBag.Instituicoes = listaReal;

            return View();
        }

        [HttpPost]
        public IActionResult EnviarDoacao(FazerUmaDoacao doacao)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string documentoUsuarioLogado = HttpContext.Session.GetString("Documento") ?? "000000";

                    _donationService.SalvarOfertaDoacao(doacao, documentoUsuarioLogado);

                    TempData["MensagemSucesso"] = "Oferta de doação enviada com sucesso!";

                    return RedirectToAction("FacaUmaDoacao", "Home");
                }
                catch (System.Exception ex)
                {
                    ViewBag.Erro = "Erro ao processar doação: " + ex.Message;
                }
            }

            var registerService = new RegisterService(_db, _configuration);
            ViewBag.Instituicoes = registerService.ListarInstituicoes();

            return View("~/Views/Home/FacaUmaDoacao.cshtml", doacao);
        }

        [HttpGet]
        public JsonResult BuscarCampanhasPorInstituicao(string nomeInstituicao)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            string cnpjInstituicao = "";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT documento FROM user_tb WHERE LOWER(usuario) = LOWER(@nome) LIMIT 1";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nome", nomeInstituicao.Trim());
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cnpjInstituicao = reader["documento"].ToString() ?? "";
                        }
                    }
                }
            }

            var registerService = new RegisterService(_db, _configuration);
            var listaCampanhas = new List<CampanhaModel>();

            if (!string.IsNullOrEmpty(cnpjInstituicao))
            {
                listaCampanhas = registerService.ListarCampanhasPorInstituicao(cnpjInstituicao.Trim());
            }

            return Json(listaCampanhas);
        }
    }
}