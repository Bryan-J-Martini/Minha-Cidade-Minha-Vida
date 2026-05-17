using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace MCMV.Data
{
    // Classe que gerencia a conexão com o banco de dados
    public class Database
    {
        private readonly string _connectionString;

        // Ele recebe o 'IConfiguration', que é o serviço do ASP.NET que lê arquivos de configuração.
        public Database(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

    }
}