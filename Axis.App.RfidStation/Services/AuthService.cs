using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Axis.App.RfidStation.Services;

/// <summary>
/// Autentica o usuario na base do tenant ja resolvido (usuario/senha).
/// Segue o mesmo esquema do Axis Web: SHA256 na coluna 'senha' da tabela 'usuarios'.
/// </summary>
public static class AuthService
{
    public static long UsuarioId { get; private set; }
    public static string UsuarioNome { get; private set; } = "";

    public static async Task<(bool sucesso, string mensagem)> LoginAsync(string login, string senha)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
                return (false, "Informe usuario e senha.");
            if (!AppConfig.Atual.TenantConfigurado)
                return (false, "Tenant nao resolvido. Verifique o subdominio.");

            var senhaHash = CriptografarSenha(senha);
            await using var conn = new NpgsqlConnection(AppConfig.Atual.TenantConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT u.codigo, u.nome, u.ativo, u.senha
                        FROM usuarios u
                        WHERE LOWER(u.login) = @login OR LOWER(u.email) = @login
                        LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@login", login.Trim().ToLower());

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return (false, "Usuario nao encontrado.");
            if (!r.GetBoolean(r.GetOrdinal("ativo"))) return (false, "Usuario inativo.");
            if (r.GetString(r.GetOrdinal("senha")) != senhaHash) return (false, "Senha incorreta.");

            UsuarioId = r.GetInt64(r.GetOrdinal("codigo"));
            UsuarioNome = r.GetString(r.GetOrdinal("nome"));
            return (true, $"Bem-vindo, {UsuarioNome}!");
        }
        catch (NpgsqlException ex) { return (false, $"Erro conexao: {ex.Message}"); }
        catch (Exception ex) { return (false, $"Erro: {ex.Message}"); }
    }

    public static void Logout() { UsuarioId = 0; UsuarioNome = ""; }

    private static string CriptografarSenha(string senha)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
