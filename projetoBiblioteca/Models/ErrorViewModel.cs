namespace projetoBiblioteca.Models
{
    // ViewModel padrão usado pela View de erro (Shared/Error.cshtml) gerada pelo template do ASP.NET.
    public class ErrorViewModel
    {
        // Identificador da requisição atual, usado para ajudar a rastrear o erro nos logs.
        public string? RequestId { get; set; }

        // Propriedade calculada: só mostra o RequestId na tela se ele não estiver vazio.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
