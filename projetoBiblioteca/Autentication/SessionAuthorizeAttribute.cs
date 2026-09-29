using Microsoft.AspNetCore.Mvc;           // Tipos base de MVC (Controller, RedirectToActionResult, etc.)
using Microsoft.AspNetCore.Mvc.Filters;   // ActionFilterAttribute, ActionExecutingContext

namespace projetoBiblioteca.Autentication
{
    // Uso:
    //   [SessionAuthorize]                                   -> qualquer usuario logado
    //   [SessionAuthorize(Perfis.Administrador, Perfis.Gerente)] -> so esses perfis
    // Filtro de ação customizado: roda ANTES de qualquer Action de um Controller
    // decorado com [SessionAuthorize], funcionando como um "guarda" de autenticação/autorização.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        // Lista de perfis (papéis) autorizados a acessar a action; vazia = qualquer logado.
        private readonly string[] _perfis;

        // Construtor: recebe 0 ou mais nomes de perfis passados no atributo, ex.: [SessionAuthorize("Admin")]
        public SessionAuthorizeAttribute(params string[] perfis)
        {
            // Guarda os perfis recebidos no campo privado para uso posterior.
            _perfis = perfis;
        }

        // Método chamado automaticamente pelo ASP.NET Core antes de executar a action.
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Obtém o objeto de Session da requisição atual.
            var session = context.HttpContext.Session;
            // Tenta ler o Id do usuário logado, salvo na Session no momento do login.
            var usuarioId = session.GetInt32(SessionKeys.UsuarioId);

            // 1) Nao esta logado -> manda para o login
            // Se não existe Id de usuário na sessão, o usuário não está autenticado.
            if (usuarioId == null)
            {
                // Substitui o resultado da action por um redirecionamento para a tela de Login.
                context.Result = new RedirectToActionResult("Index", "Login", null);
                // Interrompe a execução aqui: a action original não vai rodar.
                return;
            }

            // 2) Esta logado, mas o perfil nao tem permissao
            // Só valida perfil se o atributo foi usado com uma lista de perfis permitidos.
            if (_perfis.Length > 0)
            {
                // Lê o perfil (papel) do usuário logado, salvo na Session.
                var perfil = session.GetString(SessionKeys.UsuarioPerfil);
                // Se não há perfil salvo, ou o perfil do usuário não está na lista permitida...
                if (perfil == null || !_perfis.Contains(perfil))
                {
                    // Se o controller atual for um Controller MVC comum, guarda uma mensagem
                    // de erro no TempData para ser exibida na próxima página.
                    if (context.Controller is Controller c)
                        c.TempData["Erro"] = "Você não tem permissão para acessar esta página.";

                    // Redireciona para a Home, já que o usuário está logado mas sem permissão.
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    // Interrompe a execução: a action original não vai rodar.
                    return;
                }
            }

            // Usuário logado e autorizado: deixa a execução seguir normalmente para a action.
            base.OnActionExecuting(context);
        }
    }
}
