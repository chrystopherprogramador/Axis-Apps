using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Axis.App.RFID.Services;

public static class AuthService
{
    public static long UsuarioId { get; private set; }
    public static string UsuarioNome { get; private set; } = "";

    public static async Task<(bool sucesso, string mensagem)> LoginAsync(string email, string senha)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return (false, "Informe e-mail e senha.");

            var senhaHash = CriptografarSenha(senha);
            await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT u.codigo, u.nome, u.ativo, u.senha
                        FROM usuarios u WHERE LOWER(u.email) = @email LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email.Trim().ToLower());

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return (false, "E-mail nao encontrado.");
            if (!r.GetBoolean(r.GetOrdinal("ativo"))) return (false, "Usuario inativo.");
            if (r.GetString(r.GetOrdinal("senha")) != senhaHash) return (false, "Senha incorreta.");

            UsuarioId = r.GetInt64(r.GetOrdinal("codigo"));
            UsuarioNome = r.GetString(r.GetOrdinal("nome"));
            return (true, $"Bem-vindo, {UsuarioNome}!");
        }
        catch (NpgsqlException ex) { return (false, $"Erro conexao: {ex.Message}"); }
        catch (Exception ex) { return (false, $"Erro: {ex.Message}"); }
    }

    /// <summary>Lista empresas vinculadas ao usuario.</summary>
    public static async Task<List<(long id, string nome)>> ListarEmpresasAsync()
    {
        var list = new List<(long, string)>();
        await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
        await conn.OpenAsync();
        var sql = @"SELECT e.codigo, COALESCE(e.nome_fantasia, e.razao_social) AS nome
                    FROM empresas e INNER JOIN usuarios_empresas ue ON e.codigo = ue.empresa_id
                    WHERE ue.usuario_id = @uid AND e.ativo = TRUE ORDER BY nome";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", UsuarioId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((r.GetInt64(0), r.GetString(1)));
        return list;
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
