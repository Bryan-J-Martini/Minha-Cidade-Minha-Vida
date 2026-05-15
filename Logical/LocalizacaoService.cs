using MCMV.Data;
using MCMV.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace MCMV.Logical
{
    public class LocalizacaoService
    {
        private readonly Database _db;
        private readonly HttpClient _http;

        public LocalizacaoService(Database db, HttpClient http)
        {
            _db = db;
            _http = http;

            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "MinhaCidadeMinhaVida/1.0 (bryanmartini@alunos.fho.edu.br)"
            );
        }

        private async Task<(double lat, double lng)?> GeocodeAsync(string endereco)
        {

            var token = "";

            var url =
                    $"https://api.mapbox.com/geocoding/v5/mapbox.places/" +
                    $"{Uri.EscapeDataString(endereco)}.json" +
                    $"?access_token={token}" +
                    $"&country=br" +
                    $"&limit=1" +
                    $"&types=address";

            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);


            var features = doc.RootElement.GetProperty("features");

            if (features.GetArrayLength() == 0)
                return null;

            var center = features[0].GetProperty("center");

            var placeName = features[0].GetProperty("place_name").GetString();
            Console.WriteLine("Mapbox interpretou como:");
            Console.WriteLine(placeName);

            var lng = center[0].GetDouble();
            var lat = center[1].GetDouble();

            return (lat, lng);
        }

        public async Task<List<object>> ObterCampanhasNoMapaAsync()
        {
            var campanhas = new List<object>();
            var lista = new List<Localidade>();

            using var con = _db.GetConnection();
            con.Open();

            var cmd = new MySqlCommand(
                @"SELECT nome, rua, cep, numero, bairro
                  FROM campanhas_tb", con);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Localidade
                {
                    Nome = reader.GetString("nome"),
                    Cep = reader.GetString("cep"),
                    Bairro = reader.GetString("bairro"),
                    Rua = reader.GetString("rua"),
                    Numero = reader.GetString("numero")
                });
            }

            reader.Close();

            foreach (var camp in lista)
            {
                (double lat, double lng)? coords = null;

                // ✅ 1️⃣ Rua + Número + Cidade (mais preciso)
                if (!string.IsNullOrWhiteSpace(camp.Rua) &&
                    !string.IsNullOrWhiteSpace(camp.Numero))
                {
                    coords = await GeocodeAsync(
                        $"{camp.Rua}, {camp.Numero}, Rio Claro, SP, Brasil");
                }

                // ✅ 2️⃣ Rua + Bairro + Cidade
                if (coords == null &&
                    !string.IsNullOrWhiteSpace(camp.Rua) &&
                    !string.IsNullOrWhiteSpace(camp.Bairro))
                {
                    coords = await GeocodeAsync(
                        $"{camp.Rua}, {camp.Bairro}, Rio Claro, SP, Brasil");
                }

                //✅ 3️⃣ CEP puro (fallback final)
                if (coords == null &&
                    !string.IsNullOrWhiteSpace(camp.Cep))
                {
                    coords = await GeocodeAsync(
                        $"{camp.Cep}, Brasil");
                }

                // ❌ Nada encontrado → ignora
                if (coords == null)
                    continue;


                double lat = coords.Value.lat;
                double lng = coords.Value.lng;

                campanhas.Add(new
                {
                    nome = camp.Nome,
                    lat = coords.Value.lat,
                    lng = coords.Value.lng,
                    bairro = camp.Bairro
                });

                await Task.Delay(1000); // rate limit Nominatim
            }

            return campanhas;
        }
    }
}