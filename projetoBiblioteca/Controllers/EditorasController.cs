using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Editora

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam o acervo
    // Controller de CRUD (cadastrar/editar/excluir/listar) de Editoras.
    public class EditorasController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Editoras?busca=...&status=1
        // Lista as editoras, filtrando por texto de busca e por situação (ativo/inativo).
        public IActionResult Index(string? busca, int status = 1)
        {
            // Guarda os filtros usados para repopular a tela (campo de busca, combo de status).
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            return View(Listar(db, busca, status));
        }

        // Tambem usado pelo LivrosController para montar o combo
        // Método estático reaproveitável: busca a lista de editoras no banco via procedure.
        public static List<Editora> Listar(DataBase db, string? busca, int status)
        {
            var lista = new List<Editora>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_editora_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Se "busca" for nulo, envia string vazia (procedure trata como "sem filtro").
            cmd.Parameters.AddWithValue("p_busca", busca ?? "");
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Editora.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(Mapear(reader));

            return lista;
        }

        // GET: Editoras/Cadastrar
        // Exibe o formulário para cadastrar uma nova editora (em branco).
        public IActionResult Cadastrar()
        {
            return View(new Editora());
        }

        // POST: Editoras/Cadastrar
        // Recebe os dados do formulário e grava a nova editora no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Editora editora)
        {
            // Se a validação (ex.: nome obrigatório) falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(editora);

            try
            {
                // Chama a procedure que insere a nova editora no banco.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_editora_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_nome", editora.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco (ex.: nome duplicado): mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(editora);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Editora cadastrada com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Editoras/Edit/5
        // Busca a editora pelo Id e exibe o formulário já preenchido para edição.
        public IActionResult Edit(int id)
        {
            var editora = Obter(id);
            if (editora == null)
                return NotFound();

            return View(editora);
        }

        // POST: Editoras/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Editora editora)
        {
            // Se a validação falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(editora);

            try
            {
                // Chama a procedure que atualiza os dados da editora com o Id informado.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_editora_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.Parameters.AddWithValue("p_nome", editora.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(editora);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Editora alterada com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Editoras/Delete/5  (exclusao logica)
        // Marca a editora como inativa (não apaga fisicamente do banco).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Executar("sp_editora_excluir", id);
                TempData["Mensagem"] = "Editora excluída com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: não deixa excluir editora que ainda tem livros vinculados.
                TempData["Erro"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Editoras/Reativar/5
        // Marca a editora (antes excluída/inativa) como ativa novamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            Executar("sp_editora_reativar", id);
            TempData["Mensagem"] = "Editora reativada com sucesso!";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // Busca uma única editora pelo Id, ou null se não existir.
        private Editora? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_editora_obter", conn);
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

        // Converte a linha atual do reader (resultado do banco) em um objeto Editora.
        private static Editora Mapear(MySqlDataReader reader)
        {
            return new Editora
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                DataCadastro = reader.GetDateTime("data_cadastro"),
                Status = Convert.ToInt32(reader["status"]),
                QtdLivros = Convert.ToInt32(reader["qtd_livros"])
            };
        }
    }
}
