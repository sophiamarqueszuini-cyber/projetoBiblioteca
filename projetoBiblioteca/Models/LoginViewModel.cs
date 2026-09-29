using System.ComponentModel.DataAnnotations;  // Atributos de validação (Required, DataType, Compare, etc.)

namespace projetoBiblioteca.Models
{
    // ViewModel usado no formulário de login (tela inicial de autenticação).
    public class LoginViewModel
    {
        // Login (usuário) digitado; obrigatório.
        [Required(ErrorMessage = "Informe o login.")]
        public string Login { get; set; } = string.Empty;

        // Senha digitada; obrigatória e exibida como campo de senha (caracteres ocultos).
        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
    }

    // ViewModel usado no formulário de troca de senha do usuário logado.
    public class AlterarSenhaViewModel
    {
        // Senha atual do usuário, exigida para confirmar a identidade antes de trocar.
        [Required(ErrorMessage = "Informe a senha atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string SenhaAtual { get; set; } = string.Empty;

        // Nova senha desejada; obrigatória e com no mínimo 6 caracteres.
        [Required(ErrorMessage = "Informe a nova senha.")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        // Confirmação da nova senha; deve ser idêntica ao campo NovaSenha.
        [Required(ErrorMessage = "Confirme a nova senha.")]
        [Compare("NovaSenha", ErrorMessage = "As senhas não conferem.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
