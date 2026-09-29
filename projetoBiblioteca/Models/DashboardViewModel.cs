namespace projetoBiblioteca.Models
{
    // ViewModel usado na tela inicial (Dashboard): reúne os números resumidos do sistema
    // para serem exibidos de uma só vez na View.
    public class DashboardViewModel
    {
        // Quantidade total de livros cadastrados (ativos).
        public int TotalLivros { get; set; }
        // Quantidade total de exemplares ainda disponíveis para empréstimo.
        public int ExemplaresDisponiveis { get; set; }
        // Quantidade total de leitores cadastrados.
        public int TotalLeitores { get; set; }
        // Quantidade de empréstimos que ainda estão em aberto (não devolvidos).
        public int EmprestimosAbertos { get; set; }
        // Quantidade de empréstimos em aberto cuja data prevista de devolução já passou.
        public int EmprestimosAtrasados { get; set; }
        // Lista dos empréstimos atrasados, para exibir o detalhe deles no dashboard.
        public List<Emprestimo> Atrasados { get; set; } = new List<Emprestimo>();
    }
}
