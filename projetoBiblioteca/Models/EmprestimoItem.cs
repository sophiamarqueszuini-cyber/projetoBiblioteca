namespace projetoBiblioteca.Models
{
    // Cada livro dentro de um empréstimo (tabela emprestimo_itens)
    // Representa um item (um livro específico) dentro de um empréstimo com vários livros.
    public class EmprestimoItem
    {
        // Chave primária (Id) do item do empréstimo.
        public int Id { get; set; }
        // Id do empréstimo (cabeçalho) ao qual este item pertence.
        public int EmprestimoId { get; set; }
        // Id do livro emprestado neste item.
        public int LivroId { get; set; }
        // Título do livro, vindo de um JOIN, só para exibição.
        public string LivroTitulo { get; set; } = string.Empty;
        // Nome do autor do livro, vindo de um JOIN, só para exibição.
        public string AutorNome { get; set; } = string.Empty;
        // Data em que este exemplar específico foi devolvido; nulo enquanto não devolvido.
        public DateTime? DataDevolucao { get; set; }   // NULL = ainda não voltou

        // Propriedade calculada: verdadeiro se a data de devolução já foi preenchida.
        public bool Devolvido => DataDevolucao.HasValue;
    }
}
