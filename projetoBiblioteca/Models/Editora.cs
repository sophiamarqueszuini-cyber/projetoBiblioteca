using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, StringLength, etc.)

namespace projetoBiblioteca.Models
{
    // Tabela de apoio do acervo (editoras) - ligada a livros por chave estrangeira
    // Representa uma Editora cadastrada no sistema (tabela "editoras" no banco).
    public class Editora
    {
        // Chave primária (Id) da editora no banco de dados.
        public int Id { get; set; }

        // Nome da editora; obrigatório e limitado a 100 caracteres.
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        // Data em que a editora foi cadastrada no sistema.
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;

        // Quantos livros ativos usam este cadastro (so para exibir)
        // Propriedade só para exibição nas telas (normalmente calculada via COUNT/JOIN na consulta).
        [Display(Name = "Livros")]
        public int QtdLivros { get; set; }
    }
}
