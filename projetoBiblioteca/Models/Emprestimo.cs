using System.ComponentModel.DataAnnotations;  // Atributos de validação (Range, Display, etc.)

namespace projetoBiblioteca.Models
{
    // CABEÇALHO do empréstimo: um leitor + vários livros (Itens)
    // Representa o registro principal de um empréstimo (tabela "emprestimos").
    public class Emprestimo
    {
        // Regras de negócio (as mesmas das stored procedures)
        // Prazo padrão, em dias, para devolução de um empréstimo novo.
        public const int DiasEmprestimo = 7;
        // Quantidade de dias adicionais concedidos quando o empréstimo é prorrogado.
        public const int DiasProrrogacao = 1;

        // Chave primária (Id) do empréstimo.
        public int Id { get; set; }

        // Id do leitor que está pegando o(s) livro(s) emprestado(s); deve ser selecionado (> 0).
        [Range(1, int.MaxValue, ErrorMessage = "Selecione o leitor.")]
        [Display(Name = "Leitor")]
        public int LeitorId { get; set; }
        // Nome do leitor, vindo de um JOIN, só para exibição (não é gravado nesta tabela).
        public string? LeitorNome { get; set; }

        // Id do usuário (funcionário/atendente) que registrou o empréstimo.
        public int UsuarioId { get; set; }
        // Nome do usuário responsável, vindo de um JOIN, só para exibição.
        public string? UsuarioNome { get; set; }

        // Data em que o empréstimo foi realizado.
        public DateTime DataEmprestimo { get; set; }
        // Data limite prevista para a devolução dos livros.
        public DateTime DataPrevistaDevolucao { get; set; }
        // Data em que o empréstimo foi efetivamente encerrado (todos os livros voltaram).
        public DateTime? DataDevolucao { get; set; }   // preenchida quando o último livro volta
        // Indica se este empréstimo já teve o prazo prorrogado uma vez.
        public bool Prorrogado { get; set; }
        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;

        // ---- Livros do empréstimo ----
        // Usado no formulário: ids dos livros marcados nos checkboxes
        // Lista dos Ids de livros escolhidos no formulário de cadastro do empréstimo.
        public List<int> LivrosIds { get; set; } = new List<int>();

        // Usado nas listas (vem do GROUP_CONCAT / COUNT da procedure)
        // Quantidade total de livros que fazem parte deste empréstimo.
        public int QtdLivros { get; set; }
        // Quantidade de livros deste empréstimo que ainda não foram devolvidos.
        public int QtdPendentes { get; set; }
        // Texto com os títulos dos livros concatenados, pronto para exibir em listagens.
        public string? Livros { get; set; }

        // Usado na tela de detalhes
        // Lista detalhada de cada livro/item que compõe este empréstimo.
        public List<EmprestimoItem> Itens { get; set; } = new List<EmprestimoItem>();

        // ---- Propriedades calculadas (não existem no banco) ----
        // Verdadeiro se a data de devolução já foi preenchida (empréstimo encerrado).
        public bool Devolvido => DataDevolucao.HasValue;

        // Verdadeiro se ainda não foi devolvido e a data prevista já passou (está atrasado).
        public bool Atrasado => !Devolvido && DataPrevistaDevolucao.Date < DateTime.Today;

        // Quantidade de dias de atraso (0 se não estiver atrasado).
        public int DiasAtraso => Atrasado ? (DateTime.Today - DataPrevistaDevolucao.Date).Days : 0;

        // Verdadeiro se o empréstimo pode ser prorrogado: não devolvido, ainda não
        // prorrogado antes e não está atrasado.
        public bool PodeProrrogar => !Devolvido && !Prorrogado && !Atrasado;

        // Texto amigável com a situação atual do empréstimo, usado nas listagens/telas.
        public string Situacao =>
            Devolvido ? "Devolvido" :
            Atrasado ? "Atrasado" :
            Prorrogado ? "Prorrogado" : "Em andamento";
    }
}
