using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, StringLength, etc.)

namespace projetoBiblioteca.Models
{
    // Tabela de apoio do acervo (autores) - ligada a livros por chave estrangeira
    // Representa um Autor cadastrado no sistema (tabela "autores" no banco).
    public class Autor
    {
        // Chave primária (Id) do autor no banco de dados.
        public int Id { get; set; }

        // Nome do autor; obrigatório e limitado a 100 caracteres.
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        // Data em que o autor foi cadastrado no sistema.
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica / "soft delete").
        public int Status { get; set; } = 1;

        // Quantos livros ativos usam este cadastro (so para exibir)
        // Propriedade só para exibição nas telas (normalmente calculada via COUNT/JOIN na consulta).
        [Display(Name = "Livros")]
        public int QtdLivros { get; set; }
    }
}
