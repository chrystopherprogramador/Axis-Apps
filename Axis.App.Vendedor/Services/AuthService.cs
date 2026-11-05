using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Axis.App.Vendedor.Services;

/// <summary>
/// Serviço de autenticação — valida email+senha contra o banco PostgreSQL.
/// Só permite login se o usuário tiver vendedor vinculado.
/// </summary>
public static class AuthService
{
    public static long VendedorId { get; private set; }
    public static string VendedorNome { get; private set; } = "";
    public static long UsuarioId { get; private set; }
    public static string UsuarioNome { get; private set; } = "";

    public static async Task<(bool sucesso, string mensagem)> LoginAsync(string email, string senha)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return (false, "Informe o e-mail e a senha.");

            var senhaHash = CriptografarSenha(senha);

            await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            // Buscar usuário por email
            var sql = @"SELECT u.codigo, u.nome, u.vendedor_id, u.ativo, u.senha,
                               p.nome_razao AS vendedor_nome
                        FROM usuarios u
                        LEFT JOIN pessoas p ON p.codigo = u.vendedor_id
                        WHERE LOWER(u.email) = @email
                        LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email.Trim().ToLower());

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (false, "E-mail não encontrado.");

            var ativo = reader.GetBoolean(reader.GetOrdinal("ativo"));
            if (!ativo)
                return (false, "Usuário inativo. Contate o administrador.");

            var senhaDb = reader.GetString(reader.GetOrdinal("senha"));
            if (senhaDb != senhaHash)
                return (false, "Senha incorreta.");

            var vendedorId = reader.IsDBNull(reader.GetOrdinal("vendedor_id"))
                ? 0L
                : reader.GetInt64(reader.GetOrdinal("vendedor_id"));

            if (vendedorId == 0)
                return (false, "Este usuário não possui vendedor vinculado.\nSolicite ao administrador o vínculo.");

            UsuarioId = reader.GetInt64(reader.GetOrdinal("codigo"));
            UsuarioNome = reader.GetString(reader.GetOrdinal("nome"));
            VendedorId = vendedorId;
            VendedorNome = reader.IsDBNull(reader.GetOrdinal("vendedor_nome"))
                ? ""
                : reader.GetString(reader.GetOrdinal("vendedor_nome"));

            return (true, $"Bem-vindo, {VendedorNome}!");
        }
        catch (NpgsqlException ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro: {ex.Message}");
        }
    }

    public static void Logout()
    {
        VendedorId = 0;
        VendedorNome = "";
        UsuarioId = 0;
        UsuarioNome = "";
    }

    private static string CriptografarSenha(string senha)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
