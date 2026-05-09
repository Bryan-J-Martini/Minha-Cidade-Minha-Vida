using MCMV.Data;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MCMV.Logical
{
    public class ValidadoresService
    {
        private readonly Database _db;

        public ValidadoresService(Database db)
        {
            _db = db;
        }

        private static readonly HttpClient http = new HttpClient
        {
            BaseAddress = new System.Uri("https://mapaosc.ipea.gov.br/api/")
        };

        public static bool ValidarDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento)) return false;

            // Remove máscara: "33.208.204/0001-41" vira "33208204000141" (14 caracteres)
            var apenasDigits = new string(documento.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(apenasDigits)) return false;

            // Validação de CPF (11 dígitos)
            if (apenasDigits.Length == 11)
            {
                if (apenasDigits.Distinct().Count() == 1) return false;

                int[] nums = apenasDigits.Select(c => c - '0').ToArray();
                int sum = 0;
                for (int i = 0; i < 9; i++) sum += nums[i] * (10 - i);

                int rem = sum % 11;
                int dv1 = (rem < 2) ? 0 : 11 - rem;
                if (nums[9] != dv1) return false;

                sum = 0;
                for (int i = 0; i < 10; i++) sum += nums[i] * (11 - i);

                rem = sum % 11;
                int dv2 = (rem < 2) ? 0 : 11 - rem;
                return nums[10] == dv2;
            }

            // CORREÇÃO AQUI: Validação de CNPJ deve ser 14 dígitos após a limpeza
            if (apenasDigits.Length == 14)
            {
                if (apenasDigits.Distinct().Count() == 1) return false;

                int[] nums = apenasDigits.Select(c => c - '0').ToArray();
                int[] weights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
                int[] weights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

                int sum = 0;
                for (int i = 0; i < 12; i++) sum += nums[i] * weights1[i];

                int rem = sum % 11;
                int dv1 = (rem < 2) ? 0 : 11 - rem;
                if (nums[12] != dv1) return false;

                sum = 0;
                for (int i = 0; i < 13; i++) sum += nums[i] * weights2[i];

                rem = sum % 11;
                int dv2 = (rem < 2) ? 0 : 11 - rem;
                return nums[13] == dv2;
            }

            return false;
        }
        public static async Task<bool> VerificarInstituicao(string documento)
        {
            string cnpj = Regex.Replace(documento, @"\D", "");

            if (cnpj.Length != 14)
                return false;

            try
            {
                var response = await http.GetAsync($"osc?ft_identificador_osc={cnpj}");

                if (!response.IsSuccessStatusCode)
                    return false;

                var json = await response.Content.ReadAsStringAsync();

                using var resultado = JsonDocument.Parse(json);
                var root = resultado.RootElement;

                return root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0;
            }
            catch
            {
                // Em caso de erro na API (timeout ou fora do ar), você decide se barra 
                // ou se permite o cadastro. Aqui retornaremos false para segurança.
                return false;
            }
        }
    }
}