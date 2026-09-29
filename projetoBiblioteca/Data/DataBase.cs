using MySql.Data.MySqlClient;   // Driver oficial para conectar no MySQL a partir do .NET

namespace projetoBiblioteca.Data
{
    // Classe responsável por criar/abrir conexões com o banco de dados MySQL.
    public class DataBase
    {
        // Ajuste usuario/senha conforme o SEU MySQL
        // String de conexão: informa servidor, porta, nome do banco, usuário e senha do MySQL.
        private readonly string connectionString = "server=localhost;port=3305;database=biblioteca_db;user=root;password=12345678;";

        // Cria uma nova conexão MySQL já aberta, pronta para executar comandos SQL.
        public MySqlConnection GetConnection()
        {
            // Instancia o objeto de conexão usando a string de conexão definida acima.
            MySqlConnection conn = new MySqlConnection(connectionString);
            // Abre efetivamente a conexão com o banco de dados.
            conn.Open();
            // Retorna a conexão aberta para quem chamou este método usar.
            return conn;
        }
    }
}
