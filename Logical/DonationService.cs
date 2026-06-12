using System;
using System.Collections.Generic;
using MCMV.Models;
using Microsoft.Extensions.Configuration; 
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

namespace MCMV.Logical
{
    public class DonationService
    {
        private readonly string _connectionString;

        public DonationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public void SalvarSolicitacao(SolicitacaoDoacao solicitacao)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Alterado os nomes das colunas para bater com o seu banco de dados correto
            const string sql = "INSERT INTO solicitacaodoacao (nome_user, descricao_necessidade, nivel_urgencia, contato) " +
                               "VALUES (@inst, @desc, @urg, @contato)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@inst", solicitacao.NomeUser);
            cmd.Parameters.AddWithValue("@desc", solicitacao.DescricaoNecessidade);
            cmd.Parameters.AddWithValue("@urg", solicitacao.NivelUrgencia);
            cmd.Parameters.AddWithValue("@contato", solicitacao.Contato);
            cmd.ExecuteNonQuery();
        }


        public void SalvarOfertaDoacao(FazerUmaDoacao doacao, string documentoUsuarioLogado)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string sql = @"INSERT INTO fazerumadoacao 
                        (Instituicao, OQueDesejaDoar, EstadoItem, PreferenciaContato, Campanha, DocumentoDoador) 
                        VALUES (@inst, @oque, @estado, @contato, @camp, @doador)";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@inst", doacao.Instituicao ?? "Nulo");
            cmd.Parameters.AddWithValue("@oque", doacao.OQueDesejaDoar ?? "Nulo");
            cmd.Parameters.AddWithValue("@estado", doacao.EstadoItem ?? "Nulo");
            cmd.Parameters.AddWithValue("@contato", doacao.Contato ?? "Nulo");
            cmd.Parameters.AddWithValue("@doador", documentoUsuarioLogado);

            if (string.IsNullOrWhiteSpace(doacao.Campanha))
            {
                cmd.Parameters.AddWithValue("@camp", DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("@camp", doacao.Campanha);
            }

            cmd.ExecuteNonQuery();
        }

        public List<CategoriaCampanhaModel> ListarCategoriasPorCampanha(int idCampanha)
        {
            var categorias = new List<CategoriaCampanhaModel>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                SELECT 
                    c.id_categoria AS Id,
                    c.id_campanha  AS CampanhaId,
                    c.nome         AS Nome,
                    c.meta         AS Meta,
                    c.unidade      AS Unidade,
                    COALESCE(SUM(
                        CASE 
                            WHEN d.OQueDesejaDoar REGEXP '^[0-9]+(\\.[0-9]+)?$' 
                            THEN CAST(d.OQueDesejaDoar AS DECIMAL(10,2))
                            ELSE 0 
                        END
                    ), 0) AS Atual
                FROM categorias_campanha_tb c
                LEFT JOIN campanhas_tb camp ON camp.id_campanha = c.id_campanha
                LEFT JOIN fazerumadoacao d 
                    ON d.Campanha = camp.nome
                    AND d.EstadoItem = c.nome
                    AND d.EstadoItem != 'Nulo'
                WHERE c.id_campanha = @id
                GROUP BY c.id_categoria, c.id_campanha, c.nome, c.meta, c.unidade";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", idCampanha);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    categorias.Add(new CategoriaCampanhaModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CampanhaId = Convert.ToInt32(reader["CampanhaId"]),
                        Nome = reader["Nome"].ToString(),
                        Meta = Convert.ToInt32(reader["Meta"]),
                        Atual = Convert.ToInt32(reader["Atual"]),
                        Unidade = reader["Unidade"].ToString()
                    });
                }
            }
            return categorias;
        }

        public MeusDadosViewModel ObterResumoUsuario(string documento)
        {
            var resumo = new MeusDadosViewModel();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string nomeCompletoUsuario = "";
            const string sqlBuscarNome = "SELECT usuario FROM user_tb WHERE documento = @doc LIMIT 1";

            using (var cmdNome = new MySqlCommand(sqlBuscarNome, conn))
            {
                cmdNome.Parameters.AddWithValue("@doc", documento.Trim());
                var resultado = cmdNome.ExecuteScalar();
                if (resultado != null)
                {
                    nomeCompletoUsuario = resultado.ToString().Trim();
                }
            }

            string primeiroNome = nomeCompletoUsuario.Split(' ')[0];

            const string sqlSolicitadas = @"
        SELECT COUNT(*) 
        FROM solicitacaodoacao 
        WHERE LOWER(nome_user) COLLATE utf8mb4_general_ci LIKE LOWER(@nomeUser) COLLATE utf8mb4_general_ci";

            const string sqlEnviadas = "SELECT COUNT(*) FROM fazerumadoacao WHERE DocumentoDoador = @doc";
            const string sqlEspontaneas = "SELECT COUNT(*) FROM fazerumadoacao WHERE DocumentoDoador = @doc AND (Campanha IS NULL OR Campanha = '' OR Campanha = 'Doação Avulsa')";
            const string sqlInst = "SELECT DISTINCT Instituicao FROM fazerumadoacao WHERE DocumentoDoador = @doc";
            const string sqlCampanhas = @"
        SELECT COUNT(DISTINCT Campanha) 
        FROM fazerumadoacao 
        WHERE DocumentoDoador = @doc 
          AND Campanha IS NOT NULL 
          AND Campanha != '' 
          AND Campanha != 'Doação Avulsa'";

            using (var cmd = new MySqlCommand(sqlSolicitadas, conn))
            {
                cmd.Parameters.AddWithValue("@nomeUser", primeiroNome + "%");
                resumo.DoacoesSolicitadas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Contabiliza as Doações Enviadas
            using (var cmd = new MySqlCommand(sqlEnviadas, conn))
            {
                cmd.Parameters.AddWithValue("@doc", documento.Trim());
                resumo.DoacoesEnviadas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Contabiliza as Doações Espontâneas
            using (var cmd = new MySqlCommand(sqlEspontaneas, conn))
            {
                cmd.Parameters.AddWithValue("@doc", documento.Trim());
                resumo.DoacoesEspontaneas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Contabiliza as Campanhas Participadas
            using (var cmd = new MySqlCommand(sqlCampanhas, conn))
            {
                cmd.Parameters.AddWithValue("@doc", documento.Trim());
                resumo.CampanhasParticipadas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Alimenta a lista de Instituições Contatadas
            using (var cmd = new MySqlCommand(sqlInst, conn))
            {
                cmd.Parameters.AddWithValue("@doc", documento.Trim());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        resumo.InstituicoesContatadas.Add(reader.GetString(0));
                    }
                }
            }

            return resumo;
        }



        public List<HistoricoDoacaoViewModel> ListarHistoricoDoacoes(string documentoInstituicao, int limite = 20)
        {
            var lista = new List<HistoricoDoacaoViewModel>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
        SELECT 
            f.DocumentoDoador AS DocumentoDoador,
            f.OQueDesejaDoar AS Quantidade,
            f.EstadoItem AS Item,
            f.Campanha AS Campanha,
            cat.unidade AS Unidade
        FROM fazerumadoacao f
        INNER JOIN user_tb ui ON ui.usuario = f.Instituicao
        LEFT JOIN campanhas_tb camp ON camp.nome = f.Campanha
        LEFT JOIN categorias_campanha_tb cat 
            ON cat.id_campanha = camp.id_campanha 
            AND cat.nome = f.EstadoItem
        WHERE ui.documento = @docInst
        ORDER BY f.id_doacao DESC
        LIMIT @limite";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@docInst", documentoInstituicao.Trim());
            cmd.Parameters.AddWithValue("@limite", limite);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = reader["Item"]?.ToString() ?? "";
                var campanha = reader["Campanha"] as string;
                var unidade = reader["Unidade"] as string;
                var docDoador = reader["DocumentoDoador"]?.ToString() ?? "";

                lista.Add(new HistoricoDoacaoViewModel
                {
                    DocumentoDoador = string.IsNullOrWhiteSpace(docDoador) || docDoador == "000000"
                        ? "Não informado"
                        : docDoador,
                    Quantidade = reader["Quantidade"].ToString() ?? "",
                    Item = (item == "Nulo" || string.IsNullOrEmpty(item)) ? "" : item,
                    Unidade = (item == "Nulo" || string.IsNullOrEmpty(item)) ? "" : (unidade ?? ""),
                    Campanha = (string.IsNullOrEmpty(campanha) || campanha == "Doação Avulsa") ? null : campanha
                });
            }

            return lista;
        }




        public List<InstituicaoTransparencia> ObterDadosPortal()
        {
            var lista = new List<InstituicaoTransparencia>();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
        SELECT 
            u.usuario as Nome, 
            u.documento as Documento,
            u.email as Email,
            (
                SELECT COUNT(*) 
                FROM fazerumadoacao f 
                WHERE f.Instituicao = u.usuario 
                  AND f.EstadoItem != 'Nulo' 
                  AND f.EstadoItem != '' 
                  AND f.EstadoItem IS NOT NULL
            ) as TotalItens,
            (
                SELECT IFNULL(SUM(
                    CAST(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(f.OQueDesejaDoar, 'R$', ''), 
                                ' ', ''), 
                            '.', ''), 
                        ',', '.') 
                    AS DECIMAL(10,2))
                ), 0)
                FROM fazerumadoacao f
                WHERE f.Instituicao = u.usuario 
                  AND (f.EstadoItem = 'Nulo' OR f.EstadoItem = '' OR f.EstadoItem IS NULL)
                  AND f.OQueDesejaDoar REGEXP '[0-9]'
            ) as TotalDinheiro
        FROM user_tb u
        WHERE u.verificaInst = 1
        ORDER BY u.usuario ASC";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var item = new InstituicaoTransparencia();
                item.Nome = reader.GetString("Nome");
                item.Documento = reader.GetString("Documento");
                item.Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email");

                item.TotalItensRecebidos = reader.GetInt32("TotalItens");
                item.TotalArrecadado = reader.GetDecimal("TotalDinheiro");

                lista.Add(item);
            }
            return lista;
        }
    }
}