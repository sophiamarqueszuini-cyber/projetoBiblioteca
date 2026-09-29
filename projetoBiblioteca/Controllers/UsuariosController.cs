using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize], SessionKeys
using projetoBiblioteca.Data;                   // DataBase, Seguranca (hash de senha)
using projetoBiblioteca.Models;                 // Usuario, Perfis

namespace projetoBiblioteca.Controllers
{
    // Bibliotecario NAO entra aqui. Admin gerencia todos; Gerente so Bibliotecarios.
    // Só Administrador e Gerente podem acessar esta área (gestão de usuários do sistema).
    [SessionAuthorize(Perfis.Administrador, Perfis.Gerente)]
    public class UsuariosController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // Atalhos para os dados de quem esta logado
        // Id do usuário atualmente logado (lido da Session); 0 se não houver (não deveria acontecer aqui).
        private int UsuarioLogadoId => HttpContext.Session.GetInt32(SessionKeys.UsuarioId) ?? 0;
        // Perfil (papel) do usuário logado, lido da Session.
        private string? PerfilLogado => HttpContext.Session.GetString(SessionKeys.UsuarioPerfil);
        // Lista de perfis que o usuário logado tem permissão de gerenciar (regra em Models/Perfis.cs).
        private List<string> PerfisPermitidos => Perfis.PodeGerenciar(PerfilLogado);

        // GET: Usuarios  (status=1 ativos | status=0 excluidos)
        // Lista os usuários do sistema, filtrando por situação (ativos ou excluídos).
        public IActionResult Index(int status = 1)
        {
            var usuarios = new List<Usuario>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_usuario_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Usuario.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                usuarios.Add(Mapear(reader));

            // Envia para a View o status filtrado, os perfis que o logado pode gerenciar
            // e o Id do próprio usuário logado (para a View não deixar excluir a si mesmo, por exemplo).
            ViewBag.Status = status;
            ViewBag.PerfisPermitidos = PerfisPermitidos;
            ViewBag.UsuarioLogadoId = UsuarioLogadoId;
            return View(usuarios);
        }

        // GET: Usuarios/Cadastrar
        // Exibe o formulário para cadastrar um novo usuário, com os perfis permitidos no combo.
        public IActionResult Cadastrar()
        {
            ViewBag.Perfis = PerfisPermitidos;
            return View(new Usuario());
        }

        // POST: Usuarios/Cadastrar
        // Recebe os dados do formulário e grava o novo usuário no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Usuario usuario)
        {
            // Valida manualmente o tamanho mínimo da senha (regra que não dá para colocar só na Model,
            // pois a senha é opcional na edição, mas obrigatória no cadastro).
            if (string.IsNullOrWhiteSpace(usuario.Senha) || usuario.Senha.Length < 6)
                ModelState.AddModelError("Senha", "A senha deve ter pelo menos 6 caracteres.");

            // Seguranca: confere no servidor se o perfil escolhido e permitido
            // Mesmo que o combo do formulário só mostre perfis permitidos, confere de novo no servidor
            // (nunca confiar só na validação feita no navegador).
            if (!PerfisPermitidos.Contains(usuario.Perfil))
                ModelState.AddModelError("Perfil", "Você não pode cadastrar usuários com este perfil.");

            // Se alguma validação falhou, recarrega o combo de perfis e devolve o formulário.
            if (!ModelState.IsValid)
            {
                ViewBag.Perfis = PerfisPermitidos;
                return View(usuario);
            }

            try
            {
                // Chama a procedure que insere o novo usuário, já gravando o HASH da senha (nunca a senha em texto puro).
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_usuario_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_nome", usuario.Nome);
                cmd.Parameters.AddWithValue("p_login", usuario.Login);
                cmd.Parameters.AddWithValue("p_senha", Seguranca.GerarHash(usuario.Senha!));
                cmd.Parameters.AddWithValue("p_perfil", usuario.Perfil);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco (ex.: login duplicado): mostra a mensagem e recarrega o combo.
                ModelState.AddModelError("", ex.Message);
                ViewBag.Perfis = PerfisPermitidos;
                return View(usuario);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Usuário cadastrado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Usuarios/Edit/5
        // Busca o usuário pelo Id e exibe o formulário já preenchido para edição, se permitido.
        public IActionResult Edit(int id)
        {
            var usuario = Obter(id);
            if (usuario == null)
                return NotFound();

            // Confere se o usuário logado tem permissão para editar ESTE usuário (pelo perfil dele).
            if (!PodeGerenciar(usuario))
            {
                TempData["Erro"] = "Você não tem permissão para editar este usuário.";
                return RedirectToAction("Index");
            }

            ViewBag.Perfis = PerfisPermitidos;
            // Informa à View se o usuário está editando o próprio cadastro (para, por exemplo, esconder o combo de perfil).
            ViewBag.EditandoASiMesmo = usuario.Id == UsuarioLogadoId;
            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Usuario usuario)
        {
            // Busca o usuario REAL no banco (nao confia no que veio do formulario)
            // Recarrega do banco o usuário original, para checar a permissão com o perfil ATUAL dele
            // (o perfil enviado no formulário poderia ter sido manipulado).
            var atual = Obter(id);
            if (atual == null)
                return NotFound();

            if (!PodeGerenciar(atual))
            {
                TempData["Erro"] = "Você não tem permissão para editar este usuário.";
                return RedirectToAction("Index");
            }

            // Ninguem muda o proprio perfil (evita o admin se rebaixar e o sistema ficar sem admin)
            // Se a pessoa está editando a si mesma, ignora qualquer perfil enviado e mantém o perfil atual.
            if (id == UsuarioLogadoId)
                usuario.Perfil = atual.Perfil;

            // Confere de novo no servidor se o perfil informado é um dos permitidos.
            if (!PerfisPermitidos.Contains(usuario.Perfil))
                ModelState.AddModelError("Perfil", "Você não pode atribuir este perfil.");

            // Senha em branco = mantem a atual
            // Só valida o tamanho mínimo se uma nova senha foi realmente digitada.
            if (!string.IsNullOrEmpty(usuario.Senha) && usuario.Senha.Length < 6)
                ModelState.AddModelError("Senha", "A senha deve ter pelo menos 6 caracteres.");

            // Se alguma validação falhou, recarrega o combo/flags e devolve o formulário.
            if (!ModelState.IsValid)
            {
                ViewBag.Perfis = PerfisPermitidos;
                ViewBag.EditandoASiMesmo = id == UsuarioLogadoId;
                return View(usuario);
            }

            try
            {
                // Chama a procedure que atualiza os dados do usuário.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_usuario_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.Parameters.AddWithValue("p_nome", usuario.Nome);
                cmd.Parameters.AddWithValue("p_login", usuario.Login);
                // Se a senha veio em branco, envia NULL (a procedure entende que é para manter a senha atual).
                cmd.Parameters.AddWithValue("p_senha",
                    string.IsNullOrEmpty(usuario.Senha) ? DBNull.Value : Seguranca.GerarHash(usuario.Senha));
                cmd.Parameters.AddWithValue("p_perfil", usuario.Perfil);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem e recarrega o combo/flags.
                ModelState.AddModelError("", ex.Message);
                ViewBag.Perfis = PerfisPermitidos;
                ViewBag.EditandoASiMesmo = id == UsuarioLogadoId;
                return View(usuario);
            }

            // Se editou a si mesmo, atualiza o nome mostrado no menu
            // Mantém a Session sincronizada com os dados que acabaram de ser alterados.
            if (id == UsuarioLogadoId)
            {
                HttpContext.Session.SetString(SessionKeys.UsuarioNome, usuario.Nome);
                HttpContext.Session.SetString(SessionKeys.UsuarioLogin, usuario.Login);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Usuário alterado com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Usuarios/Delete/5  (exclusao logica: status = 0)
        // Exclui (logicamente) um usuário, com várias checagens de segurança antes.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            // Ninguém pode excluir o próprio usuário (evita ficar sem acesso ao sistema).
            if (id == UsuarioLogadoId)
            {
                TempData["Erro"] = "Você não pode excluir o seu próprio usuário.";
                return RedirectToAction("Index");
            }

            var usuario = Obter(id);
            // Só permite excluir se o usuário existir e o perfil dele puder ser gerenciado pelo logado.
            if (usuario == null || !PodeGerenciar(usuario))
            {
                TempData["Erro"] = "Você não tem permissão para excluir este usuário.";
                return RedirectToAction("Index");
            }

            Executar("sp_usuario_excluir", id);
            TempData["Mensagem"] = "Usuário excluído com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Usuarios/Reativar/5  (status = 1)
        // Reativa um usuário antes excluído, com a mesma checagem de permissão.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            var usuario = Obter(id);
            if (usuario == null || !PodeGerenciar(usuario))
            {
                TempData["Erro"] = "Você não tem permissão para reativar este usuário.";
                return RedirectToAction("Index", new { status = 0 });
            }

            Executar("sp_usuario_reativar", id);
            TempData["Mensagem"] = "Usuário reativado com sucesso!";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        //  Metodos auxiliares
        // ------------------------------------------------------------------
        // Verifica se o perfil do usuário-alvo está entre os perfis que o usuário logado pode gerenciar.
        private bool PodeGerenciar(Usuario alvo) => PerfisPermitidos.Contains(alvo.Perfil);

        // Busca um único usuário pelo Id, ou null se não existir.
        private Usuario? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_usuario_obter", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Mapear(reader) : null;
        }

        // Executa qualquer procedure que receba apenas o Id como parâmetro (excluir/reativar).
        private void Executar(string procedure, int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand(procedure, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_id", id);
            cmd.ExecuteNonQuery();
        }

        // Converte a linha atual do reader (resultado do banco) em um objeto Usuario.
        // Repare que a senha (hash) NUNCA é lida de volta para o objeto, por segurança.
        private static Usuario Mapear(MySqlDataReader reader)
        {
            return new Usuario
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                Login = reader.GetString("login"),
                Perfil = reader.GetString("perfil"),
                DataCadastro = reader.GetDateTime("data_cadastro"),
                Status = Convert.ToInt32(reader["status"])
            };
        }
    }
}
