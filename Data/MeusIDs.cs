using System.Security.Claims;

namespace VozDoPovo.Data
{
    public static class MeusIDs
    {
        public const string Sucesso = "a";
        public const string Falha = "b";
    }

    public enum IdCargos : byte
    {
        Membro = 1,
        Admin = 2
    }

    public static class ClaimsHelper
    {
        public static bool EstaLogado(this ClaimsPrincipal user)
        {
            return user.Identity?.IsAuthenticated ?? false;
        }

        public static string? ObterNomeUsuario(this ClaimsPrincipal user)
        {
            return user.Identity?.Name;
        }

        public static string ObterCargo(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "Convidado";
        }

        public static int ObterIdUsuario(this ClaimsPrincipal user)
        {
            var valor = user.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(valor, out int id) ? id : 0;
        }
    }
}
