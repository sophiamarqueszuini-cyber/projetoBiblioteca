using Microsoft.AspNetCore.Mvc;                 // Controller, IActionResult, ResponseCache
using MySql.Data.MySqlClient;                   // MySqlCommand para executar procedures
using projetoBiblioteca.Autentication;          // Atributo [SessionAuthorize]
using projetoBiblioteca.Data;                   // DataBase (abre conexão com o MySQL)
using projetoBiblioteca.Models;                 // DashboardViewModel, ErrorViewModel
using System.Diagnostics;                       // Activity, usado para pegar o Id da requisição no erro

namespace projetoBiblioteca.Controllers
{
    [SessionAuthorize]   // qualquer usuario logado
    // Controller da tela inicial (Dashboard) e da página de erro padrão.
    public class HomeController : Controller
    {
        // Instância usada para abrir conexões com o banco de dados.
        private readonly DataBase db = new DataBase();

        // GET: / ou /Home/Index -> monta e exibe o dashboard com os números resumidos.
        public IActionResult Index()
        {
            // ViewModel que vai reunir todos os números exibidos no painel.
            var painel = new DashboardViewModel();

            // Abre conexão e chama a procedure que calcula os totais do dashboard.
            using (var conn = db.GetConnection())
            using (var cmd = new MySqlCommand("sp_dashboard", conn))
            {
                // Informa que o comando é uma stored procedure (não um SQL comum).
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                // Executa a procedure e lê o resultado (um único registro com os totais).
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Copia cada coluna do resultado para a propriedade correspondente do painel.
                    painel.TotalLivros = Convert.ToInt32(reader["total_livros"]);
                    painel.ExemplaresDisponiveis = Convert.ToInt32(reader["exemplares_disponiveis"]);
                    painel.TotalLeitores = Convert.ToInt32(reader["total_leitores"]);
                    painel.EmprestimosAbertos = Convert.ToInt32(reader["emprestimos_abertos"]);
                    painel.EmprestimosAtrasados = Convert.ToInt32(reader["emprestimos_atrasados"]);
                }
            }

            // Reaproveita o método estático do EmprestimosController para listar os atrasados.
            painel.Atrasados = EmprestimosController.Listar(db, "atrasados");

            // Envia o painel preenchido para a View (Views/Home/Index.cshtml).
            return View(painel);
        }

        // Desabilita qualquer cache de navegador/proxy para a página de erro,
        // garantindo que ela sempre mostre a informação mais recente.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Monta o ViewModel de erro com o identificador da requisição atual (para suporte/log).
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
