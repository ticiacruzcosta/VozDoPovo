using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using VozDoPovo.Data;

namespace VozDoPovo.Models
{
    [method: SetsRequiredMembers]
    public class PerfilModel
    {
        public int id { get; set; }

        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 25 caracteres.")]
        public required string nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "A senha deve ter entre 3 e 25 caracteres.")]
        [DataType(DataType.Password)]
        public required string senha { get; set; } = string.Empty;

        public IdCargos cargo { get; set; }

        public DateTime criada { get; set; }

        public DateTime ultimoLogin { get; set; }

        public string? foto { get; set; }
    }
}
