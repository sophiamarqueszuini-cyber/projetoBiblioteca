using System.Security.Cryptography;  // Classes de criptografia, entre elas o SHA256
using System.Text;                   // Encoding.UTF8, para converter texto em bytes

namespace projetoBiblioteca.Data
{
    // Classe utilitária estática com funções de segurança (aqui, hashing de senha).
    public static class Seguranca
    {
        // Gera o hash SHA-256 da senha (64 caracteres hexadecimais minusculos).
        // E o mesmo resultado da funcao SHA2('senha', 256) do MySQL.
        // Recebe a senha em texto puro e devolve seu hash SHA-256 (nunca guardamos a senha original).
        public static string GerarHash(string senha)
        {
            // Converte a senha para bytes (UTF-8) e calcula o hash SHA-256 desses bytes.
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
            // Converte os bytes do hash para uma string hexadecimal e deixa tudo em minúsculo.
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
