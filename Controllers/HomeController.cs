using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using VozDoPovo.Data;
using VozDoPovo.Models;

namespace VozDoPovo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BancoContext _bancoContext;

        public HomeController(BancoContext bancoContext)
        {
            _bancoContext = bancoContext;
        }

        private async Task PreencherMetadadosDenunciasAsync(List<DenunciaModel> denuncias)
        {
            if (denuncias.Count == 0)
                return;

            var idsPosts = denuncias.Select(d => d.id).ToList();
            var idsPerfis = denuncias.Select(d => d.perfil).Distinct().ToList();

            var contagemComentarios = await _bancoContext.Comentarios
                .Where(c => idsPosts.Contains(c.idPost))
                .GroupBy(c => c.idPost)
                .Select(g => new { IdPost = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.IdPost, x => x.Total);

            var nomesPerfis = await _bancoContext.Perfis
                .Where(p => idsPerfis.Contains(p.id))
                .ToDictionaryAsync(p => p.id, p => p.nome);

            foreach (var denuncia in denuncias)
            {
                denuncia.comments = contagemComentarios.TryGetValue(denuncia.id, out int total) ? total : 0;
                denuncia.nome = nomesPerfis.TryGetValue(denuncia.perfil, out string? nomeAutor) && !string.IsNullOrWhiteSpace(nomeAutor)
                    ? nomeAutor
                    : "Cidadão";
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(PerfilModel formulario)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Home");

            string nomeBusca = formulario.nome.Trim().ToLower();
            PerfilModel? perfil = await _bancoContext.Perfis
                .FirstOrDefaultAsync(x => x.nome.ToLower() == nomeBusca);

            if (perfil is null)
            {
                TempData[MeusIDs.Sucesso] = "Nome de perfil não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            if (formulario.senha != perfil.senha)
            {
                TempData[MeusIDs.Sucesso] = "Senha incorreta.";
                return RedirectToAction("Index", "Home");
            }

            perfil.ultimoLogin = DateTime.Now;
            await _bancoContext.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, perfil.nome),
                new Claim(ClaimTypes.Role, perfil.cargo.ToString()),
                new Claim(ClaimTypes.UserData, perfil.id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Reddit", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult CriarPerfil()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CriarPerfil(PerfilModel formulario)
        {
            if (!ModelState.IsValid)
                return View(formulario);

            string nomeBusca = formulario.nome.Trim().ToLower();
            bool nomeJaExiste = await _bancoContext.Perfis
                .AnyAsync(x => x.nome.ToLower() == nomeBusca);

            if (nomeJaExiste)
            {
                TempData[MeusIDs.Sucesso] = "Este nome de usuário já está sendo utilizado.";
                return RedirectToAction("CriarPerfil", "Home");
            }

            formulario.nome = formulario.nome.Trim();
            formulario.cargo = IdCargos.Membro;
            formulario.ultimoLogin = DateTime.Now;
            formulario.criada = DateTime.Now;

            _bancoContext.Perfis.Add(formulario);
            await _bancoContext.SaveChangesAsync();

            TempData[MeusIDs.Sucesso] = "Perfil criado com sucesso! Faça login para continuar.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Reddit()
        {
            List<DenunciaModel> denuncias = await _bancoContext.Denuncias
                .Where(x => !x.arquivada)
                .OrderByDescending(x => x.id)
                .ToListAsync();

            await PreencherMetadadosDenunciasAsync(denuncias);
            return View(denuncias);
        }

        public async Task<IActionResult> Lixeira()
        {
            List<DenunciaModel> denuncias = await _bancoContext.Denuncias
                .Where(x => x.arquivada)
                .OrderByDescending(x => x.id)
                .ToListAsync();

            await PreencherMetadadosDenunciasAsync(denuncias);
            return View(denuncias);
        }

        public async Task<IActionResult> MapaGeral()
        {
            List<DenunciaModel> denuncias = await _bancoContext.Denuncias
                .Where(x => !x.arquivada)
                .OrderByDescending(x => x.id)
                .ToListAsync();

            await PreencherMetadadosDenunciasAsync(denuncias);
            return View(denuncias);
        }

        public async Task<IActionResult> MinhasDenuncias()
        {
            if (!User.EstaLogado())
                return RedirectToAction("Index", "Home");

            int idUsuario = User.ObterIdUsuario();
            List<DenunciaModel> denuncias = await _bancoContext.Denuncias
                .Where(x => x.perfil == idUsuario)
                .OrderByDescending(x => x.id)
                .ToListAsync();

            await PreencherMetadadosDenunciasAsync(denuncias);
            return View(denuncias);
        }

        public IActionResult LeitorPlaca()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
