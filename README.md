# Sistema de Biblioteca

ASP.NET Core MVC (.NET 10) + MySQL, sem Entity Framework.

## Acesso inicial

| Campo | Valor |
|-------|-------|
| Login | `admin` |
| Senha | `admin123` |
| Perfil | Administrador |

## Como executar

1. No MySQL Workbench, execute `BancoDeDados/biblioteca_db.sql` inteiro.
2. Confira a senha do MySQL em `projetoBiblioteca/Data/DataBase.cs` (padrão: `root` / `123456789`).
3. Abra `projetoBiblioteca.slnx` no Visual Studio e pressione F5, ou rode `projetoBiblioteca/run_biblioteca.bat`.
4. Acesse http://localhost:5190

Mais detalhes (perfis, regras de empréstimo) no `LEIAME.txt`.
