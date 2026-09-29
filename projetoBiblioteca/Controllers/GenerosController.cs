using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Genero

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam o acervo
    // Controller de CRUD (cadastrar/editar/excluir/listar) de Gêneros literários.
    public class GenerosController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Generos?busca=...&status=1
        // Lista os gêneros, filtrando por texto de busca e por situação (ativo/inativo).
        public IActionResult Index(string? busca, int status = 1)
        {
            // Guarda os filtros usados para repopular a tela (campo de busca, combo de status).
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            return View(Listar(db, busca, status));
        }

        // Tambem usado pelo LivrosController para montar o combo
        // Método estático reaproveitável: busca a lista de gêneros no banco via procedure.
        public static List<Genero> Listar(DataBase db, string? busca, int status)
        {
            var lista = new List<Genero>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_genero_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Se "busca" for nulo, envia string vazia (procedure trata como "sem filtro").
            cmd.Parameters.AddWithValue("p_busca", busca ?? "");
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Genero.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(Mapear(reader));

            return lista;
        }

        // GET: Generos/Cadastrar
        // Exibe o formulário para cadastrar um novo gênero (em branco).
        public IActionResult Cadastrar()
        {
            return View(new Genero());
        }

        // POST: Generos/Cadastrar
        // Recebe os dados do formulário e grava o novo gênero no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Genero genero)
        {
            // Se a validação (ex.: nome obrigatório) falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(genero);

            try
            {
                // Chama a procedure que insere o novo gênero no banco.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_genero_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_nome", genero.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco (ex.: nome duplicado): mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(genero);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Gênero cadastrado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Generos/Edit/5
        // Busca o gênero pelo Id e exibe o formulário já preenchido para edição.
        public IActionResult Edit(int id)
        {
            var genero = Obter(id);
            if (genero == null)
                return NotFound();

            return View(genero);
        }

        // POST: Generos/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Genero genero)
        {
            // Se a validação falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(genero);

            try
            {
                // Chama a procedure que atualiza os dados do gênero com o Id informado.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_genero_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.Parameters.AddWithValue("p_nome", genero.Nome.Trim());
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(genero);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Gênero alterado com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Generos/Delete/5  (exclusao logica)
        // Marca o gênero como inativo (não apaga fisicamente do banco).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Executar("sp_genero_excluir", id);
                TempData["Mensagem"] = "Gênero excluído com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: não deixa excluir gênero que ainda tem livros vinculados.
                TempData["Erro"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Generos/Reativar/5
        // Marca o gênero (antes excluído/inativo) como ativo novamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            Executar("sp_genero_reativar", id);
            TempData["Mensagem"] = "Gênero reativado com sucesso!";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // Busca um único gênero pelo Id, ou null se não existir.
        private Genero? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_genero_obter", conn);
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

        // Converte a linha atual do reader (resultado do banco) em um objeto Genero.
        private static Genero Mapear(MySqlDataReader reader)
        {
            return new Genero
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
