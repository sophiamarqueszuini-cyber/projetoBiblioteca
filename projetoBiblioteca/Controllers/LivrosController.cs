using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using Microsoft.AspNetCore.Mvc.Rendering;       // SelectList, usado para montar os combos (dropdowns)
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Livro

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam livros
    // Controller de CRUD (cadastrar/editar/excluir/listar) de Livros do acervo.
    public class LivrosController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Livros?busca=assis&status=1
        // Lista os livros, filtrando por texto de busca (título/autor) e por situação (ativo/inativo).
        public IActionResult Index(string? busca, int status = 1)
        {
            var livros = new List<Livro>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_livro_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Se "busca" for nulo, envia string vazia (procedure trata como "sem filtro").
            cmd.Parameters.AddWithValue("p_busca", busca ?? "");
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Livro.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                livros.Add(Mapear(reader));

            // Guarda os filtros usados para repopular a tela (campo de busca, combo de status).
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            return View(livros);
        }

        // Livros ativos com exemplar disponivel (usado no novo emprestimo)
        // Método estático reaproveitável pelo EmprestimosController, para montar a lista de
        // checkboxes de livros que podem ser emprestados agora.
        public static List<Livro> ListarDisponiveis(DataBase db)
        {
            var livros = new List<Livro>();

            // Abre a conexão e chama a procedure que já filtra só livros com exemplar livre.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_livro_listar_disponiveis", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            // Percorre cada linha retornada e converte para um objeto Livro.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                livros.Add(Mapear(reader));

            return livros;
        }

        // GET: Livros/Cadastrar
        // Exibe o formulário para cadastrar um novo livro (em branco), com os combos já carregados.
        public IActionResult Cadastrar()
        {
            CarregarCombos();
            return View(new Livro());
        }

        // POST: Livros/Cadastrar
        // Recebe os dados do formulário e grava o novo livro no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Livro livro)
        {
            // Se a validação falhar, recarrega os combos (senão eles ficariam vazios) e devolve o formulário.
            if (!ModelState.IsValid)
            {
                CarregarCombos();
                return View(livro);
            }

            try
            {
                // Chama a procedure que insere o novo livro no banco.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_livro_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                // Monta os parâmetros comuns a Cadastrar/Edit em um único método auxiliar.
                PreencherParametros(cmd, livro);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem e recarrega os combos.
                ModelState.AddModelError("", ex.Message);
                CarregarCombos();
                return View(livro);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Livro cadastrado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Livros/Edit/5
        // Busca o livro pelo Id e exibe o formulário já preenchido, com os combos carregados.
        public IActionResult Edit(int id)
        {
            var livro = Obter(id);
            if (livro == null)
                return NotFound();

            CarregarCombos();
            return View(livro);
        }

        // POST: Livros/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Livro livro)
        {
            // Se a validação falhar, recarrega os combos e devolve o formulário.
            if (!ModelState.IsValid)
            {
                CarregarCombos();
                return View(livro);
            }

            try
            {
                // Chama a procedure que atualiza os dados do livro com o Id informado.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_livro_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                PreencherParametros(cmd, livro);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem e recarrega os combos.
                ModelState.AddModelError("", ex.Message);
                CarregarCombos();
                return View(livro);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Livro alterado com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Livros/Delete/5  (exclusao logica)
        // Marca o livro como inativo (não apaga fisicamente do banco).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Executar("sp_livro_excluir", id);
                TempData["Mensagem"] = "Livro excluído com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: não deixa excluir livro que está emprestado.
                TempData["Erro"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Livros/Reativar/5
        // Marca o livro (antes excluído/inativo) como ativo novamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            try
            {
                Executar("sp_livro_reativar", id);
                TempData["Mensagem"] = "Livro reativado com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: autor/gênero/editora do livro ainda estão inativos.
                TempData["Erro"] = ex.Message + " Reative o cadastro correspondente antes.";
                return RedirectToAction("Index", new { status = 0 });
            }
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // Combos de autor, gênero e editora (somente cadastros ativos)
        // Monta as listas (SelectList) usadas nos dropdowns do formulário de Livro,
        // reaproveitando os métodos estáticos "Listar" dos outros controllers.
        private void CarregarCombos()
        {
            ViewBag.Autores = new SelectList(AutoresController.Listar(db, null, 1), "Id", "Nome");
            ViewBag.Generos = new SelectList(GenerosController.Listar(db, null, 1), "Id", "Nome");
            ViewBag.Editoras = new SelectList(EditorasController.Listar(db, null, 1), "Id", "Nome");
        }

        // Preenche os parâmetros comuns às procedures de criar e editar livro.
        private static void PreencherParametros(MySqlCommand cmd, Livro livro)
        {
            cmd.Parameters.AddWithValue("p_titulo", livro.Titulo);
            cmd.Parameters.AddWithValue("p_autor_id", livro.AutorId);
            cmd.Parameters.AddWithValue("p_genero_id", livro.GeneroId);
            cmd.Parameters.AddWithValue("p_editora_id", livro.EditoraId);
            // Campos opcionais: se estiverem nulos, envia DBNull.Value (equivalente a NULL no banco).
            cmd.Parameters.AddWithValue("p_isbn", (object?)livro.Isbn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_ano", (object?)livro.AnoPublicacao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_quantidade", livro.QuantidadeTotal);
        }

        // Busca um único livro pelo Id, ou null se não existir.
        private Livro? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_livro_obter", conn);
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

        // Converte a linha atual do reader (resultado do banco, já com os JOINs) em um objeto Livro.
        private static Livro Mapear(MySqlDataReader reader)
        {
            return new Livro
            {
                Id = reader.GetInt32("id"),
                Titulo = reader.GetString("titulo"),
                AutorId = reader.GetInt32("autor_id"),
                AutorNome = reader.GetString("autor_nome"),
                GeneroId = reader.GetInt32("genero_id"),
                GeneroNome = reader.GetString("genero_nome"),
                EditoraId = reader.GetInt32("editora_id"),
                EditoraNome = reader.GetString("editora_nome"),
                // Colunas que aceitam NULL precisam checar IsDBNull antes de ler o valor.
                Isbn = reader.IsDBNull(reader.GetOrdinal("isbn")) ? null : reader.GetString("isbn"),
                AnoPublicacao = reader.IsDBNull(reader.GetOrdinal("ano_publicacao")) ? null : reader.GetInt32("ano_publicacao"),
                QuantidadeTotal = reader.GetInt32("quantidade_total"),
                QuantidadeDisponivel = reader.GetInt32("quantidade_disponivel"),
                DataCadastro = reader.GetDateTime("data_cadastro"),
                Status = Convert.ToInt32(reader["status"])
            };
        }
    }
}
