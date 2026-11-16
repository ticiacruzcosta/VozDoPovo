using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VozDoPovo.Data;
using VozDoPovo.Models;

namespace VozDoPovo.Controllers
{
    public class DenunciaController : Controller
    {
        private readonly BancoContext _bancoContext;

        public DenunciaController(BancoContext bancoContext)
        {
            _bancoContext = bancoContext;
        }

        public IActionResult CriarDenuncia(string? placa)
        {
            if (!User.EstaLogado())
                return RedirectToAction("Index", "Home");

            var model = new DenunciaModel
            {
                contexto = !string.IsNullOrWhiteSpace(placa)
                    ? $"Ocorrência envolvendo veículo de placa {placa.ToUpper()}: "
                    : string.Empty
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CriarDenuncia(DenunciaModel model)
        {
            if (!User.EstaLogado())
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(model);

            if (model.arquivoImg != null)
            {
                string pastaImagens = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(pastaImagens);

                string nomeArquivo = Path.GetRandomFileName() + Path.GetExtension(model.arquivoImg.FileName);
                string caminhoCompleto = Path.Combine(pastaImagens, nomeArquivo);

                using var stream = new FileStream(caminhoCompleto, FileMode.Create);
                await model.arquivoImg.CopyToAsync(stream);

                model.imagem = nomeArquivo;
            }

            model.perfil = User.ObterIdUsuario();
            model.data = DateTime.Now;
            model.arquivada = false;
            model.arquivoImg = null;

            _bancoContext.Denuncias.Add(model);
            await _bancoContext.SaveChangesAsync();

            TempData[MeusIDs.Sucesso] = JsonSerializer.Serialize(
                new JsonDenuncia { id = model.id }
            );

            return RedirectToAction("Reddit", "Home");
        }

        public async Task<IActionResult> EditarDenuncia(int id)
        {
            DenunciaModel? denuncia = await _bancoContext.Denuncias.FirstOrDefaultAsync(x => x.id == id);

            if (denuncia == null)
                return NotFound();

            return View(denuncia);
        }

        private void DeletarImagemPorNome(string nomeArquivo)
        {
            string arquivo = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                nomeArquivo
            );

            if (System.IO.File.Exists(arquivo))
            {
                System.IO.File.Delete(arquivo);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditarDenuncia(DenunciaModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var denunciaExistente = await _bancoContext.Denuncias.AsNoTracking().FirstOrDefaultAsync(x => x.id == model.id);
            if (denunciaExistente != null && model.perfil == 0)
            {
                model.perfil = denunciaExistente.perfil;
                model.likes = denunciaExistente.likes;
                model.dislikes = denunciaExistente.dislikes;
            }

            if (model.arquivoImg != null)
            {
                if (!string.IsNullOrEmpty(model.imagem))
                    DeletarImagemPorNome(model.imagem);

                string pastaImagens = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(pastaImagens);

                string nomeArquivo = Path.GetRandomFileName() + Path.GetExtension(model.arquivoImg.FileName);
                using var stream = new FileStream(Path.Combine(pastaImagens, nomeArquivo), FileMode.Create);
                await model.arquivoImg.CopyToAsync(stream);

                model.imagem = nomeArquivo;
            }

            model.data = DateTime.Now;
            model.arquivada = false;

            _bancoContext.Denuncias.Update(model);
            await _bancoContext.SaveChangesAsync();

            TempData[MeusIDs.Sucesso] = JsonSerializer.Serialize(
                new JsonDenuncia { id = model.id }
            );

            return RedirectToAction("Reddit", "Home");
        }

        public async Task<IActionResult> DeletarDenuncia(int id)
        {
            var denuncia = await _bancoContext.Denuncias.FirstOrDefaultAsync(x => x.id == id);

            if (denuncia != null)
            {
                denuncia.arquivada = true;
                _bancoContext.Denuncias.Update(denuncia);
                await _bancoContext.SaveChangesAsync();
            }

            return RedirectToAction("Reddit", "Home");
        }

        private async Task<IActionResult> LikeDislikeAsync(int id, string? url, bool like)
        {
            DenunciaModel? denuncia = await _bancoContext.Denuncias.FirstOrDefaultAsync(x => x.id == id);

            if (denuncia != null)
            {
                if (like && denuncia.likes < byte.MaxValue)
                    denuncia.likes += 1;
                else if (!like && denuncia.dislikes < byte.MaxValue)
                    denuncia.dislikes += 1;

                _bancoContext.Denuncias.Update(denuncia);
                await _bancoContext.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
                return LocalRedirect(url);

            return RedirectToAction("Reddit", "Home");
        }

        public async Task<IActionResult> Like(int id, string? a)
        {
            return await LikeDislikeAsync(id, a, true);
        }

        public async Task<IActionResult> Dislike(int id, string? a)
        {
            return await LikeDislikeAsync(id, a, false);
        }

        private async Task<DenunciaModel?> AbrirPostAsync(int id)
        {
            DenunciaModel? denuncia = await _bancoContext.Denuncias.FirstOrDefaultAsync(x => x.id == id);

            if (denuncia != null)
            {
                denuncia.comentarios = await _bancoContext.Comentarios
                    .Where(x => x.idPost == id)
                    .OrderByDescending(x => x.id)
                    .ToListAsync();
                denuncia.comments = denuncia.comentarios.Count;

                PerfilModel? autor = await _bancoContext.Perfis.FirstOrDefaultAsync(p => p.id == denuncia.perfil);
                denuncia.nome = autor?.nome ?? "Cidadão";

                ViewBag.map = true;
                return denuncia;
            }
            return null;
        }

        public async Task<IActionResult> VerDenuncia(int id)
        {
            var denuncia = await AbrirPostAsync(id);
            if (denuncia == null)
                return RedirectToAction("Reddit", "Home");

            return View(denuncia);
        }

        [HttpPost]
        public async Task<IActionResult> Comentar(ComentarioModel comentario)
        {
            int idPost = comentario.idPost > 0 ? comentario.idPost : comentario.id;

            if (!string.IsNullOrWhiteSpace(comentario.mensagem))
            {
                var novoComentario = new ComentarioModel
                {
                    idPost = idPost,
                    mensagem = comentario.mensagem.Trim(),
                    data = DateTime.Now
                };

                _bancoContext.Comentarios.Add(novoComentario);
                await _bancoContext.SaveChangesAsync();
            }

            return RedirectToAction("VerDenuncia", "Denuncia", new { id = idPost });
        }
    }
}
