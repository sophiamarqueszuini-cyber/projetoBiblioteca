using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, StringLength, Compare, etc.)

namespace projetoBiblioteca.Models
{
    // Representa um Usuário do sistema (funcionário que faz login: administrador,
    // gerente ou bibliotecário).
    public class Usuario
    {
        // Chave primária (Id) do usuário no banco de dados.
        public int Id { get; set; }

        // Nome completo do usuário; obrigatório e limitado a 100 caracteres.
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        // Login usado para autenticação; obrigatório e limitado a 50 caracteres.
        [Required(ErrorMessage = "Informe o login.")]
        [StringLength(50)]
        public string Login { get; set; } = string.Empty;

        // So usado nos formularios (no banco fica apenas o hash)
        // Senha digitada no formulário (texto puro); nunca é gravada assim no banco,
        // apenas seu hash (veja Data/Seguranca.cs).
        [DataType(DataType.Password)]
        public string? Senha { get; set; }

        // Campo de confirmação da senha, usado só na validação do formulário.
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare("Senha", ErrorMessage = "As senhas não conferem.")]
        public string? ConfirmarSenha { get; set; }

        // Perfil (papel) do usuário: Administrador, Gerente ou Bibliotecario.
        [Required(ErrorMessage = "Selecione o perfil.")]
        public string Perfil { get; set; } = string.Empty;

        // Data em que o usuário foi cadastrado no sistema.
        [Display(Name = "Cadastrado em")]
        public DateTime DataCadastro { get; set; }

        // Situação do registro: 1 = ativo, 0 = inativo (exclusão lógica).
        public int Status { get; set; } = 1;
    }
}
