using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Axis.App.Salao.Services;

/// <summary>
/// Serviço de autenticação — valida email+senha contra o banco PostgreSQL do Axis.
/// Só permite login se o usuário tiver um profissional de salão vinculado
/// (usuarios.vendedor_id -> pessoas.codigo -> salao_profissionais.pessoa_id).
/// </summary>
public static class AuthService
{
    public static long UsuarioId { get; private set; }
    public static string UsuarioNome { get; private set; } = "";
    public static long ProfissionalId { get; private set; }
    public static string ProfissionalNome { get; private set; } = "";

    public static async Task<(bool sucesso, string mensagem)> LoginAsync(string email, string senha)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return (false, "Informe o e-mail e a senha.");

            var senhaHash = CriptografarSenha(senha);

            await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT u.codigo, u.nome, u.vendedor_id, u.ativo, u.senha,
                               pr.codigo AS profissional_id, pr.nome AS profissional_nome
                        FROM usuarios u
                        LEFT JOIN salao_profissionais pr
                               ON pr.pessoa_id = u.vendedor_id AND pr.ativo = 'S'
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

            if (reader.IsDBNull(reader.GetOrdinal("profissional_id")))
                return (false, "Este usuário não possui um profissional de salão vinculado.\nSolicite ao administrador o vínculo.");

            UsuarioId = reader.GetInt64(reader.GetOrdinal("codigo"));
            UsuarioNome = reader.GetString(reader.GetOrdinal("nome"));
            ProfissionalId = reader.GetInt64(reader.GetOrdinal("profissional_id"));
            ProfissionalNome = reader.IsDBNull(reader.GetOrdinal("profissional_nome"))
                ? ""
                : reader.GetString(reader.GetOrdinal("profissional_nome"));

            return (true, $"Bem-vindo(a), {ProfissionalNome}!");
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
        UsuarioId = 0;
        UsuarioNome = "";
        ProfissionalId = 0;
        ProfissionalNome = "";
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
