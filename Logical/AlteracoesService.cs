using MCMV.Data;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCMV.Logical
{
    public class AlteracoesService
    {
        private readonly Database _db;

        // Construtor: O ASP.NET vai entregar o Database configurado aqui
        public AlteracoesService(Database db)
        {
            _db = db;
        }

        public bool mudarValidacaoInstituicao(string cnpj)
        {
            try
            {
                using (var con = _db.GetConnection())
                {
                    con.Open();
                    string query = "UPDATE user_tb SET verificaInst = true WHERE documento = @cnpj";

                    using (var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@cnpj", cnpj);
                        int linhasAfetadas = cmd.ExecuteNonQuery();

                        // Retorna true se pelo menos uma linha foi alterada
                        return linhasAfetadas > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Não foi possivel atualizar");
                return false;
            }
        }
        public bool? JaEstaVerificada(string cnpj)
        {
            try
            {
                using (var con = _db.GetConnection())
                {
                    con.Open();

                    string query = @"SELECT verificaInst 
                             FROM user_tb 
                             WHERE documento = @cnpj
                             LIMIT 1;";

                    using (var cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@cnpj", cnpj);

                        object? result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return null; // não achou o CNPJ

                        // MySQL geralmente guarda boolean como TINYINT(1)
                        return Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch (MySqlException)
            {
                Console.WriteLine("Não foi possível averiguar");
                return null;
            }
        }
    }
}