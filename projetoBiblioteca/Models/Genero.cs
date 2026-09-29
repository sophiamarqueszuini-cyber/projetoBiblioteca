using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, StringLength, etc.)

namespace projetoBiblioteca.Models
{
    // Tabela de apoio do acervo (generos) - ligada a livros por chave estrangeira
    // Representa um Gênero literário cadastrado no sistema (tabela "generos").
    public class Genero
    {
        // Chave primária (Id) do gênero no banco de dados.
        public int Id { get; set; }

        // Nome do gênero; obrigatório e limitado a 50 caracteres.
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(50)]
        public string Nome { get; set; } = string.Empty;

        // Data em que o gênero foi cadastrado no sistema.
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;

        // Quantos livros ativos usam este cadastro (so para exibir)
        // Propriedade só para exibição nas telas (normalmente calculada via COUNT/JOIN na consulta).
        [Display(Name = "Livros")]
        public int QtdLivros { get; set; }
    }
}
