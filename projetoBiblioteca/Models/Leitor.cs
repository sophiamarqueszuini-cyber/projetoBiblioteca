using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, StringLength, EmailAddress, etc.)

namespace projetoBiblioteca.Models
{
    // Representa um Leitor (usuário da biblioteca que pega livros emprestados).
    public class Leitor
    {
        // Chave primária (Id) do leitor no banco de dados.
        public int Id { get; set; }

        // Nome completo do leitor; obrigatório e limitado a 100 caracteres.
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        // CPF do leitor; obrigatório e limitado a 14 caracteres (com pontuação).
        [Required(ErrorMessage = "Informe o CPF.")]
        [StringLength(14)]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        // Telefone de contato do leitor (opcional).
        [StringLength(20)]
        public string? Telefone { get; set; }

        // E-mail do leitor (opcional), validado quanto ao formato de e-mail.
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(100)]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        // Endereço do leitor (opcional).
        [StringLength(200)]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        // Data em que o leitor foi cadastrado no sistema.
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;
    }
}
