using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, atributos HTTP
using Microsoft.AspNetCore.Mvc.Rendering;       // SelectList, usado para montar os combos (dropdowns)
using MySql.Data.MySqlClient;                   // MySqlCommand, MySqlException, MySqlDataReader
using projetoBiblioteca.Autentication;          // [SessionAuthorize], SessionKeys
using projetoBiblioteca.Data;                   // DataBase
using projetoBiblioteca.Models;                 // Emprestimo, EmprestimoItem

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // todos os perfis gerenciam empréstimos
    // Controller responsável pelo fluxo de empréstimos: criar, listar, ver detalhes,
    // prorrogar, devolver (tudo ou item a item) e excluir.
    public class EmprestimosController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: Emprestimos?filtro=abertos|atrasados|devolvidos|todos
        // Lista os empréstimos de acordo com o filtro escolhido (aba da tela).
        public IActionResult Index(string filtro = "abertos")
        {
            // Guarda o filtro atual para marcar a aba selecionada na View.
            ViewBag.Filtro = filtro;
            return View(Listar(db, filtro));
        }

        // Também usado pelo HomeController (lista de atrasados no painel)
        // Método estático reaproveitável: busca os empréstimos conforme o filtro informado.
        public static List<Emprestimo> Listar(DataBase db, string filtro)
        {
            var lista = new List<Emprestimo>();

            // Abre a conexão e chama a procedure de listagem, passando o filtro desejado.
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_emprestimo_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_filtro", filtro);

            // Percorre cada linha retornada e converte para um objeto Emprestimo.
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(Mapear(reader));

            return lista;
        }

        // GET: Emprestimos/Detalhes/5  (cabeçalho + livros, com devolução livro a livro)
        // Mostra os detalhes de um empréstimo específico, incluindo cada livro emprestado.
        public IActionResult Detalhes(int id)
        {
            Emprestimo? emprestimo = null;

            // Primeiro bloco: busca o cabeçalho do empréstimo (dados gerais).
            using (var conn = db.GetConnection())
            using (var cmd = new MySqlCommand("sp_emprestimo_obter", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    emprestimo = Mapear(reader);
            }

            // Se não encontrou o empréstimo com esse Id, retorna página 404.
            if (emprestimo == null)
                return NotFound();

            // Segundo bloco: busca a lista de livros (itens) que fazem parte deste empréstimo.
            using (var conn = db.GetConnection())
            using (var cmd = new MySqlCommand("sp_emprestimo_itens_listar", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_emprestimo_id", id);
                using var reader = cmd.ExecuteReader();
                // Para cada item retornado, monta um EmprestimoItem e adiciona na lista do empréstimo.
                while (reader.Read())
                {
                    emprestimo.Itens.Add(new EmprestimoItem
                    {
                        Id = reader.GetInt32("id"),
                        EmprestimoId = reader.GetInt32("emprestimo_id"),
                        LivroId = reader.GetInt32("livro_id"),
                        LivroTitulo = reader.GetString("livro_titulo"),
                        AutorNome = reader.GetString("autor_nome"),
                        // Coluna que aceita NULL: confere IsDBNull antes de ler a data.
                        DataDevolucao = reader.IsDBNull(reader.GetOrdinal("data_devolucao")) ? null : reader.GetDateTime("data_devolucao")
                    });
                }
            }

            // Envia o empréstimo (com seus itens) para a View de detalhes.
            return View(emprestimo);
        }

        // GET: Emprestimos/Cadastrar
        // Exibe o formulário para registrar um novo empréstimo, com os combos carregados.
        public IActionResult Cadastrar()
        {
            CarregarCombos();
            return View(new Emprestimo());
        }

        // POST: Emprestimos/Cadastrar
        // Grava o cabeçalho e depois cada livro, tudo dentro de UMA TRANSAÇÃO:
        // se qualquer livro der erro, nada é gravado (rollback).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Emprestimo emprestimo)
        {
            // Remove ids inválidos (<= 0) e duplicados da lista de livros marcados no formulário.
            var livros = emprestimo.LivrosIds.Where(x => x > 0).Distinct().ToList();

            // Exige pelo menos um livro selecionado.
            if (livros.Count == 0)
                ModelState.AddModelError("LivrosIds", "Selecione pelo menos um livro.");

            // Se algo na validação falhou (leitor não selecionado, nenhum livro, etc.), recarrega os combos.
            if (!ModelState.IsValid)
            {
                CarregarCombos();
                return View(emprestimo);
            }

            int novoId;
            // Abre a conexão e inicia uma transação: ou tudo é gravado, ou nada é.
            using var conn = db.GetConnection();
            using var transacao = conn.BeginTransaction();
            try
            {
                // 1) Cabeçalho (a procedure devolve o id gerado)
                // Cria o registro principal do empréstimo (leitor + usuário responsável).
                using (var cmd = new MySqlCommand("sp_emprestimo_criar", conn, transacao))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_leitor_id", emprestimo.LeitorId);
                    cmd.Parameters.AddWithValue("p_usuario_id", HttpContext.Session.GetInt32(SessionKeys.UsuarioId));
                    // ExecuteScalar: a procedure retorna um único valor (o Id do empréstimo recém-criado).
                    novoId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) Um item para cada livro selecionado
                // Para cada livro marcado no formulário, cria um item de empréstimo ligado ao cabeçalho acima.
                foreach (var livroId in livros)
                {
                    using var cmdItem = new MySqlCommand("sp_emprestimo_item_adicionar", conn, transacao);
                    cmdItem.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdItem.Parameters.AddWithValue("p_emprestimo_id", novoId);
                    cmdItem.Parameters.AddWithValue("p_livro_id", livroId);
                    cmdItem.ExecuteNonQuery();
                }

                // Tudo certo: confirma (grava definitivamente) todas as operações da transação.
                transacao.Commit();
            }
            catch (MySqlException ex)
            {
                // Algo deu errado (ex.: livro sem exemplar disponível): desfaz TUDO o que foi feito na transação.
                transacao.Rollback();
                ModelState.AddModelError("", ex.Message);
                CarregarCombos();
                return View(emprestimo);
            }

            // Monta a mensagem de sucesso informando o número do empréstimo, quantidade de livros e prazo.
            TempData["Mensagem"] = $"Empréstimo nº {novoId} registrado com {livros.Count} livro(s)! Devolução prevista para "
                                   + DateTime.Today.AddDays(Emprestimo.DiasEmprestimo).ToString("dd/MM/yyyy") + ".";
            // Redireciona para a tela de detalhes do empréstimo recém-criado.
            return RedirectToAction("Detalhes", new { id = novoId });
        }

        // POST: Emprestimos/Prorrogar/5   (+1 dias, apenas uma vez, para todos os livros)
        // Estende o prazo de devolução do empréstimo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Prorrogar(int id, string? filtro, string? voltar)
        {
            try
            {
                Executar("sp_emprestimo_prorrogar", "p_id", id);
                TempData["Mensagem"] = "Empréstimo prorrogado por mais " + Emprestimo.DiasProrrogacao + (Emprestimo.DiasProrrogacao == 1 ? " dia." : " dias.");
            }
            catch (MySqlException ex)
            {
                // Ex.: procedure impede prorrogar de novo ou empréstimo já atrasado.
                TempData["Erro"] = ex.Message;
            }
            // Decide para qual tela voltar (lista ou detalhes) com base no parâmetro "voltar".
            return Voltar(id, filtro, voltar);
        }

        // POST: Emprestimos/Devolver/5   (devolve TODOS os livros pendentes)
        // Registra a devolução de todos os livros ainda pendentes deste empréstimo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Devolver(int id, string? filtro, string? voltar)
        {
            try
            {
                Executar("sp_emprestimo_devolver", "p_id", id);
                TempData["Mensagem"] = "Devolução de todos os livros registrada com sucesso!";
            }
            catch (MySqlException ex)
            {
                TempData["Erro"] = ex.Message;
            }
            return Voltar(id, filtro, voltar);
        }

        // POST: Emprestimos/DevolverItem/12?emprestimoId=5   (devolve UM livro)
        // Registra a devolução de apenas um livro específico do empréstimo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DevolverItem(int id, int emprestimoId)
        {
            try
            {
                Executar("sp_emprestimo_item_devolver", "p_item_id", id);
                TempData["Mensagem"] = "Devolução do livro registrada com sucesso!";
            }
            catch (MySqlException ex)
            {
                TempData["Erro"] = ex.Message;
            }
            // Sempre volta para a tela de detalhes do empréstimo (onde o botão foi clicado).
            return RedirectToAction("Detalhes", new { id = emprestimoId });
        }

        // POST: Emprestimos/Delete/5  (exclusão lógica; livros não devolvidos voltam ao estoque)
        // Exclui (logicamente) o empréstimo; a procedure cuida de devolver os livros ao estoque.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, string? filtro)
        {
            Executar("sp_emprestimo_excluir", "p_id", id);
            TempData["Mensagem"] = "Empréstimo excluído com sucesso!";
            // Volta para a listagem, mantendo o filtro que estava sendo usado (padrão "abertos").
            return RedirectToAction("Index", new { filtro = filtro ?? "abertos" });
        }

        // ------------------------------------------------------------------
        // Volta para a tela de onde o botão foi clicado (lista ou detalhes)
        // Método auxiliar para decidir o redirecionamento após ações como Prorrogar/Devolver.
        private IActionResult Voltar(int id, string? filtro, string? voltar)
        {
            // Se veio da tela de detalhes, volta para ela.
            if (voltar == "detalhes")
                return RedirectToAction("Detalhes", new { id });

            // Caso contrário, volta para a listagem com o filtro atual.
            return RedirectToAction("Index", new { filtro = filtro ?? "abertos" });
        }

        // Monta as listas (SelectList) usadas no formulário de Cadastrar empréstimo:
        // o combo de leitores e a lista de livros disponíveis (checkboxes).
        private void CarregarCombos()
        {
            var leitores = LeitoresController.Listar(db, null, 1);
            // Monta o texto exibido no combo como "Nome - CPF 000.000.000-00".
            ViewBag.Leitores = new SelectList(
                leitores.Select(l => new { l.Id, Texto = l.Nome + " - CPF " + l.Cpf }), "Id", "Texto");

            // Lista de livros com exemplar disponível (vira uma tabela com checkboxes)
            ViewBag.Livros = LivrosController.ListarDisponiveis(db);
        }

        // Executa qualquer procedure que receba apenas um Id, cujo nome do parâmetro varia
        // (por isso recebe também o nome do parâmetro, diferente do padrão "p_id" dos outros controllers).
        private void Executar(string procedure, string parametro, int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand(procedure, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue(parametro, id);
            cmd.ExecuteNonQuery();
        }

        // Converte a linha atual do reader (resultado do banco) em um objeto Emprestimo (cabeçalho).
        private static Emprestimo Mapear(MySqlDataReader reader)
        {
            return new Emprestimo
            {
                Id = reader.GetInt32("id"),
                LeitorId = reader.GetInt32("leitor_id"),
                LeitorNome = reader.GetString("leitor_nome"),
                UsuarioId = reader.GetInt32("usuario_id"),
                UsuarioNome = reader.GetString("usuario_nome"),
                DataEmprestimo = reader.GetDateTime("data_emprestimo"),
                DataPrevistaDevolucao = reader.GetDateTime("data_prevista_devolucao"),
                // Coluna que aceita NULL: confere IsDBNull antes de ler a data.
                DataDevolucao = reader.IsDBNull(reader.GetOrdinal("data_devolucao")) ? null : reader.GetDateTime("data_devolucao"),
                Prorrogado = Convert.ToBoolean(reader["prorrogado"]),
                Status = Convert.ToInt32(reader["status"]),
                QtdLivros = Convert.ToInt32(reader["qtd_livros"]),
                QtdPendentes = Convert.ToInt32(reader["qtd_pendentes"]),
                // Texto com os títulos concatenados; se vier nulo, usa string vazia.
                Livros = reader.IsDBNull(reader.GetOrdinal("livros")) ? "" : reader.GetString("livros")
            };
        }
    }
}
