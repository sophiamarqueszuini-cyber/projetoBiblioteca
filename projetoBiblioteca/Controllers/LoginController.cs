using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand para executar procedures
using projetoBiblioteca.Autentication;          // SessionKeys, [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase, Seguranca (hash de senha)
using projetoBiblioteca.Models;                 // LoginViewModel, AlterarSenhaViewModel

namespace projetoBiblioteca.Controllers
{
    // Controller responsável por login, logout e troca de senha.
    public class LoginController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: /Login
        public IActionResult Index()
        {
            // Ja esta logado? Vai direto para o inicio
            // Se já existe um usuário salvo na sessão, não precisa logar de novo.
            if (HttpContext.Session.GetInt32(SessionKeys.UsuarioId) != null)
                return RedirectToAction("Index", "Home");

            // Ainda não logado: exibe o formulário de login.
            return View();
        }

        // POST: /Login
        // Recebe o formulário de login enviado pelo usuário.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginViewModel model)
        {
            // Se os campos obrigatórios não foram preenchidos corretamente, mostra o formulário de novo.
            if (!ModelState.IsValid)
                return View(model);

            // Abre a conexão e prepara a chamada da procedure de autenticação.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_usuario_login", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Envia o login digitado como parâmetro.
            cmd.Parameters.AddWithValue("p_login", model.Login);
            // Nunca envia a senha em texto puro: envia o hash SHA-256 dela.
            cmd.Parameters.AddWithValue("p_senha", Seguranca.GerarHash(model.Senha));

            // Executa a procedure; se encontrar um registro, login e senha conferem.
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // Grava o usuario na sessao
                // Guarda os dados do usuário autenticado na Session, para uso no resto do sistema.
                HttpContext.Session.SetInt32(SessionKeys.UsuarioId, reader.GetInt32("id"));
                HttpContext.Session.SetString(SessionKeys.UsuarioNome, reader.GetString("nome"));
                HttpContext.Session.SetString(SessionKeys.UsuarioLogin, reader.GetString("login"));
                HttpContext.Session.SetString(SessionKeys.UsuarioPerfil, reader.GetString("perfil"));

                // Login bem-sucedido: vai para a tela inicial.
                return RedirectToAction("Index", "Home");
            }

            // Nenhum registro encontrado: login ou senha estão errados.
            ViewBag.Erro = "Login ou senha inválidos.";
            return View(model);
        }

        // GET: /Login/Sair
        // Encerra a sessão do usuário (logout).
        public IActionResult Sair()
        {
            // Remove todos os dados guardados na Session deste usuário.
            HttpContext.Session.Clear();
            // Volta para a tela de login.
            return RedirectToAction("Index");
        }

        // GET: /Login/AlterarSenha
        // Exibe o formulário de troca de senha; só acessível a quem está logado.
        [SessionAuthorize]
        public IActionResult AlterarSenha()
        {
            return View();
        }

        // POST: /Login/AlterarSenha
        // Processa a troca de senha enviada pelo formulário.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize]
        public IActionResult AlterarSenha(AlterarSenhaViewModel model)
        {
            // Se os campos não passaram nas validações (tamanho mínimo, confirmação etc.), mostra o formulário de novo.
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Chama a procedure que confere a senha atual e grava o hash da nova senha.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_usuario_alterar_senha", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", HttpContext.Session.GetInt32(SessionKeys.UsuarioId));
                cmd.Parameters.AddWithValue("p_senha_atual", Seguranca.GerarHash(model.SenhaAtual));
                cmd.Parameters.AddWithValue("p_senha_nova", Seguranca.GerarHash(model.NovaSenha));
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro do banco (ex.: senha atual incorreta, validado dentro da procedure): mostra a mensagem.
                ViewBag.Erro = ex.Message;
                return View(model);
            }

            // Sucesso: avisa o usuário e volta para a tela inicial.
            TempData["Mensagem"] = "Senha alterada com sucesso!";
            return RedirectToAction("Index", "Home");
        }
    }
}
