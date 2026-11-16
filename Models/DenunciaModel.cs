using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace VozDoPovo.Models
{
    [method: SetsRequiredMembers]
    public class DenunciaModel()
    {
        public int id { get; set; }
        public int perfil { get; set; }
        public byte likes { get; set; } = 0;
        public byte dislikes { get; set; } = 0;
        public string? imagem { get; set; }

        [Required(ErrorMessage = "Descreva o problema da denúncia.")]
        public required string contexto { get; set; } = string.Empty;

        public DateTime data { get; set; }
        public bool anonimo { get; set; }
        public bool arquivada { get; set; }

        public double latitude { get; set; } = -8.769666;
        public double longitude { get; set; } = -63.884296;

        [NotMapped]
        public virtual IFormFile? arquivoImg { get; set; }

        [NotMapped]
        public virtual List<ComentarioModel>? comentarios { get; set; }

        [NotMapped]
        public virtual int comments { get; set; } = 0;

        [NotMapped]
        public virtual string nome { get; set; } = "Cidadão";

        [NotMapped]
        public virtual string? mensagem { get; set; }
    }

    public class JsonDenuncia
    {
        public int id { get; set; }
    }
}
