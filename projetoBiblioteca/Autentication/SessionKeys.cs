namespace projetoBiblioteca.Autentication
{
    // Nomes das chaves gravadas na Session (evita erros de digitacao)
    // Classe estática apenas com constantes: centraliza os nomes usados como
    // "chave" ao gravar/ler valores na Session, evitando strings soltas espalhadas pelo código.
    public static class SessionKeys
    {
        // Chave usada para guardar o Id do usuário logado na Session.
        public const string UsuarioId = "UsuarioId";
        // Chave usada para guardar o nome do usuário logado na Session.
        public const string UsuarioNome = "UsuarioNome";
        // Chave usada para guardar o login (usuário) do usuário logado na Session.
        public const string UsuarioLogin = "UsuarioLogin";
        // Chave usada para guardar o perfil (papel/permissão) do usuário logado na Session.
        public const string UsuarioPerfil = "UsuarioPerfil";
    }
}
