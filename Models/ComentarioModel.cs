using System.Diagnostics.CodeAnalysis;

namespace VozDoPovo.Models
{
    [method: SetsRequiredMembers]
    public class ComentarioModel()
    {
        public int id { get; set; }
        public int idPost { get; set; }
        public required string mensagem { get; set; } = string.Empty;
        public DateTime data { get; set; }
    }
}
