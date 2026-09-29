using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Leitor

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam leitores
    // Controller de CRUD (cadastrar/editar/excluir/listar) de Leitores (usuários que pegam livros emprestados).
    public class LeitoresController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Leitores?busca=maria&status=1
        // Lista os leitores, filtrando por texto de busca (nome/CPF) e por situação (ativo/inativo).
        public IActionResult Index(string? busca, int status = 1)
        {
            // Guarda os filtros usados para repopular a tela (campo de busca, combo de status).
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            return View(Listar(db, busca, status));
        }

        // Tambem usado pelo EmprestimosController para montar o combo
        // Método estático reaproveitável: busca a lista de leitores no banco via procedure.
        public static List<Leitor> Listar(DataBase db, string? busca, int status)
        {
            var leitores = new List<Leitor>();

            // Abre a conexão e chama a procedure de listagem.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_leitor_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // Se "busca" for nulo, envia string vazia (procedure trata como "sem filtro").
            cmd.Parameters.AddWithValue("p_busca", busca ?? "");
            cmd.Parameters.AddWithValue("p_status", status);

            // Percorre cada linha retornada e converte para um objeto Leitor.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                leitores.Add(Mapear(reader));

            return leitores;
        }

        // GET: Leitores/Cadastrar
        // Exibe o formulário para cadastrar um novo leitor (em branco).
        public IActionResult Cadastrar()
        {
            return View(new Leitor());
        }

        // POST: Leitores/Cadastrar
        // Recebe os dados do formulário e grava o novo leitor no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Leitor leitor)
        {
            // Se a validação (nome/CPF obrigatórios, e-mail em formato válido, etc.) falhar, devolve o formulário.
            if (!ModelState.IsValid)
                return View(leitor);

            try
            {
                // Chama a procedure que insere o novo leitor no banco.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_leitor_criar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_nome", leitor.Nome);
                cmd.Parameters.AddWithValue("p_cpf", leitor.Cpf);
                // Campos opcionais: se estiverem nulos, envia DBNull.Value (equivalente a NULL no banco).
                cmd.Parameters.AddWithValue("p_telefone", (object?)leitor.Telefone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_email", (object?)leitor.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_endereco", (object?)leitor.Endereco ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco (ex.: CPF duplicado): mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(leitor);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Leitor cadastrado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET: Leitores/Edit/5
        // Busca o leitor pelo Id e exibe o formulário já preenchido para edição.
        public IActionResult Edit(int id)
        {
            var leitor = Obter(id);
            if (leitor == null)
                return NotFound();

            return View(leitor);
        }

        // POST: Leitores/Edit/5
        // Recebe os dados alterados no formulário e grava a atualização no banco.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Leitor leitor)
        {
            // Se a validação falhar, devolve o formulário com os erros.
            if (!ModelState.IsValid)
                return View(leitor);

            try
            {
                // Chama a procedure que atualiza os dados do leitor com o Id informado.
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_leitor_editar", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.Parameters.AddWithValue("p_nome", leitor.Nome);
                cmd.Parameters.AddWithValue("p_cpf", leitor.Cpf);
                cmd.Parameters.AddWithValue("p_telefone", (object?)leitor.Telefone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_email", (object?)leitor.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_endereco", (object?)leitor.Endereco ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Erro vindo do banco: mostra a mensagem no formulário.
                ModelState.AddModelError("", ex.Message);
                return View(leitor);
            }

            // Sucesso: mensagem para a próxima tela e volta para a listagem.
            TempData["Mensagem"] = "Leitor alterado com sucesso!";
            return RedirectToAction("Index");
        }

        // POST: Leitores/Delete/5  (exclusao logica)
        // Marca o leitor como inativo (não apaga fisicamente do banco).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Executar("sp_leitor_excluir", id);
                TempData["Mensagem"] = "Leitor excluído com sucesso!";
            }
            catch (MySqlException ex)
            {
                // Ex.: não deixa excluir leitor que ainda tem empréstimos em aberto.
                TempData["Erro"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Leitores/Reativar/5
        // Marca o leitor (antes excluído/inativo) como ativo novamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            Executar("sp_leitor_reativar", id);
            TempData["Mensagem"] = "Leitor reativado com sucesso!";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // Busca um único leitor pelo Id, ou null se não existir.
        private Leitor? Obter(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_leitor_obter", conn);
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

        // Converte a linha atual do reader (resultado do banco) em um objeto Leitor.
        private static Leitor Mapear(MySqlDataReader reader)
        {
            return new Leitor
            {
                Id = reader.GetInt32("id"),
                Nome = reader.GetString("nome"),
                Cpf = reader.GetString("cpf"),
                // Colunas que aceitam NULL precisam do IsDBNull
                // Antes de ler como string, confere se a coluna está nula no banco.
                Telefone = reader.IsDBNull(reader.GetOrdinal("telefone")) ? null : reader.GetString("telefone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Endereco = reader.IsDBNull(reader.GetOrdinal("endereco")) ? null : reader.GetString("endereco"),
                DataCadastro = reader.GetDateTime("data_cadastro"),
                Status = Convert.ToInt32(reader["status"])
            };
        }
    }
}
