namespace projetoBiblioteca.Models
{
    // Os 3 tipos de usuario do sistema e as regras de quem cadastra quem
    // Classe utilitária estática com os nomes dos perfis (papéis) de usuário do sistema
    // e a regra de negócio de qual perfil pode gerenciar (cadastrar/editar/excluir) qual outro.
    public static class Perfis
    {
        // Perfil com acesso total ao sistema.
        public const string Administrador = "Administrador";
        // Perfil intermediário, gerencia bibliotecários.
        public const string Gerente = "Gerente";
        // Perfil operacional, atende o dia a dia da biblioteca.
        public const string Bibliotecario = "Bibliotecario";

        // Quais perfis o usuario logado pode cadastrar / editar / excluir
        // Recebe o perfil do usuário atualmente logado e devolve a lista de perfis
        // que esse usuário tem permissão de gerenciar.
        public static List<string> PodeGerenciar(string? perfilLogado)
        {
            // Administrador pode gerenciar todos os perfis, inclusive outros administradores.
            if (perfilLogado == Administrador)
                return new List<string> { Administrador, Gerente, Bibliotecario };

            // Gerente só pode gerenciar bibliotecários.
            if (perfilLogado == Gerente)
                return new List<string> { Bibliotecario };

            // Qualquer outro perfil (ex.: Bibliotecario) não gerencia ninguém.
            return new List<string>();   // Bibliotecario nao cadastra usuarios
        }
    }
}
