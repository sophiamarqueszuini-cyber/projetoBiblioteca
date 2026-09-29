using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, Range, StringLength, etc.)

namespace projetoBiblioteca.Models
{
    // Representa um Livro do acervo da biblioteca (tabela "livros").
    public class Livro
    {
        // Chave primária (Id) do livro no banco de dados.
        public int Id { get; set; }

        // Título do livro; obrigatório e limitado a 150 caracteres.
        [Required(ErrorMessage = "Informe o título.")]
        [StringLength(150)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        // ---- Chaves estrangeiras (vão para o banco) ----
        // Id do autor do livro; deve ser selecionado (valor > 0) no formulário.
        [Range(1, int.MaxValue, ErrorMessage = "Selecione o autor.")]
        [Display(Name = "Autor")]
        public int AutorId { get; set; }

        // Id do gênero do livro; deve ser selecionado (valor > 0) no formulário.
        [Range(1, int.MaxValue, ErrorMessage = "Selecione o gênero.")]
        [Display(Name = "Gênero")]
        public int GeneroId { get; set; }

        // Id da editora do livro; deve ser selecionado (valor > 0) no formulário.
        [Range(1, int.MaxValue, ErrorMessage = "Selecione a editora.")]
        [Display(Name = "Editora")]
        public int EditoraId { get; set; }

        // ---- Nomes vindos do JOIN (só para exibir) ----
        // Nome do autor, preenchido via JOIN, apenas para exibição nas telas.
        public string? AutorNome { get; set; }
        // Nome do gênero, preenchido via JOIN, apenas para exibição nas telas.
        public string? GeneroNome { get; set; }
        // Nome da editora, preenchido via JOIN, apenas para exibição nas telas.
        public string? EditoraNome { get; set; }

        // Código ISBN do livro (opcional).
        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string? Isbn { get; set; }

        // Ano de publicação do livro (opcional), validado dentro de uma faixa razoável.
        [Range(1000, 2100, ErrorMessage = "Ano inválido.")]
        [Display(Name = "Ano de publicação")]
        public int? AnoPublicacao { get; set; }

        // Quantidade total de exemplares deste livro no acervo.
        [Range(1, 1000, ErrorMessage = "Informe entre 1 e 1000 exemplares.")]
        [Display(Name = "Quantidade de exemplares")]
        public int QuantidadeTotal { get; set; } = 1;

        // Quantidade de exemplares atualmente disponíveis (não emprestados).
        [Display(Name = "Disponíveis")]
        public int QuantidadeDisponivel { get; set; }

        // Data em que o livro foi cadastrado no sistema.
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;
    }
}
