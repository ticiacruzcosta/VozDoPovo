using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace VozDoPovo.Controllers
{
    public class LerPlacaController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LerPlacaController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Enviar(IFormFile frame)
        {
            if (frame == null || frame.Length == 0)
            {
                return BadRequest(new { mensagem = "Nenhum frame recebido." });
            }

            string apiToken = _configuration["PlateRecognizer:ApiToken"] ?? "511bbb1efabebb18e7d019980a9b00c3ce85fac0";
            string apiUrl = _configuration["PlateRecognizer:ApiUrl"] ?? "https://api.platerecognizer.com/v1/plate-reader/";

            using var form = new MultipartFormDataContent();
            using var stream = frame.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            form.Add(fileContent, "upload", frame.FileName);
            form.Add(new StringContent("br"), "regions");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiToken);

            var response = await client.PostAsync(apiUrl, form);

            if (!response.IsSuccessStatusCode)
            {
                string erroDetalhe = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    mensagem = $"Falha na leitura da placa ({response.StatusCode}).",
                    detalhe = erroDetalhe
                });
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            JsonNode? node = JsonNode.Parse(jsonString);

            if (node?["results"] is JsonArray results && results.Count > 0 && results[0] is JsonNode result)
            {
                return Ok(new
                {
                    placa = result["plate"]?.GetValue<string>(),
                    x1 = result["box"]?["xmin"]?.GetValue<int>(),
                    y1 = result["box"]?["ymin"]?.GetValue<int>(),
                    x2 = result["box"]?["xmax"]?.GetValue<int>(),
                    y2 = result["box"]?["ymax"]?.GetValue<int>()
                });
            }

            return BadRequest(new { mensagem = "Nenhuma placa identificada na imagem." });
        }
    }
}
