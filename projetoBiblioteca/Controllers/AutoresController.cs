using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Autor

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam o acervo
    // Controller de CRUD (cadastrar/editar/excluir/listar) de Autores.
    public class AutoresController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Autores?busca=...&status=1
        // Lista os autores, filtrando por texto de busca e por situação (ativo/inativo).
        public IActionResult Index(string? busca, int status = 1)
        {
            // Guarda os filtros usados para repopular a tela (campo de busca, combo de status).
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            return View(Listar(db, busca, status));
        }

        // Tambem usado pelo LivrosController para montar o combo
        // Método estático reaproveitável: busca a lista de autores no banco via procedure.
        public static List<Autor> Listar(DataBase db, string? busca, int status)
        {
            var lista = new List<Autor>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_autor_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Se "busca" for nulo, envia string vazia (procedure trata como "sem filtro").
            cmd.Parameters.AddWithValue("p_busca", busca ?? "");
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Autor.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(Mapear(reader));

            return lista;
        }

        // GET: Autores/Cadastrar
        // Exibe o formulário para cadastrar um novo autor (em branco).
        public IActionResult Cadastrar()
        {
            return View(new Autor());
        }

        // POST: Autores/Cadastrar
        // Recebe os dados do formulário e grava o novo autor no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Autor autor)
        {
            // Se a validação (ex.: nome obrigatório) falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(autor);

            try
            {
                // Chama a procedure que insere o novo autor no banco.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_autor_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_nome", autor.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco (ex.: nome duplicado): mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(autor);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Autor cadastrado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Autores/Edit/5
        // Busca o autor pelo Id e exibe o formulário já preenchido para edição.
        public IActionResult Edit(int id)
        {
            var autor = Obter(id);
            if (autor == null)
                return NotFound();

            return View(autor);
        }

        // POST: Autores/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Autor autor)
        {
            // Se a validação falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(autor);

            try
            {
                // Chama a procedure que atualiza os dados do autor com o Id informado.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_autor_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.Parameters.AddWithValue("p_nome", autor.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(autor);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Autor alterado com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Autores/Delete/5  (exclusao logica)
        // Marca o autor como inativo (não apaga fisicamente do banco).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Executar("sp_autor_excluir", id);
                TempData["Mensagem"] = "Autor excluído com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: não deixa excluir autor que ainda tem livros vinculados.
                TempData["Erro"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Autores/Reativar/5
        // Marca o autor (antes excluído/inativo) como ativo novamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            Executar("sp_autor_reativar", id);
            TempData["Mensagem"] = "Autor reativado com sucesso!";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // Busca um único autor pelo Id, ou null se não existir.
        private Autor? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_autor_obter", conn);
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

        // Converte a linha atual do reader (resultado do banco) em um objeto Autor.
        private static Autor Mapear(MySqlDataReader reader)
        {
            return new Autor
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
