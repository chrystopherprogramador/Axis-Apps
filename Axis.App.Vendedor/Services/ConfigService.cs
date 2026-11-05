using System.Text.Json;

namespace Axis.App.Vendedor.Services;

/// <summary>
/// Gerencia a configuração de conexão com o banco PostgreSQL.
/// Os dados são obtidos via QR Code e persistidos em Preferences.
/// </summary>
public static class ConfigService
{
    private const string KeyHost = "db_host";
    private const string KeyPorta = "db_porta";
    private const string KeyDatabase = "db_database";
    private const string KeyUsuario = "db_usuario";
    private const string KeySenha = "db_senha";
    private const string KeyEmpresaId = "db_empresa_id";

    public static bool Configurado =>
        !string.IsNullOrEmpty(Preferences.Get(KeyHost, ""));

    public static string ConnectionString
    {
        get
        {
            var host = Preferences.Get(KeyHost, "");
            var porta = Preferences.Get(KeyPorta, "5432");
            var db = Preferences.Get(KeyDatabase, "");
            var user = Preferences.Get(KeyUsuario, "");
            var pwd = Preferences.Get(KeySenha, "");
            return $"Host={host};Port={porta};Database={db};Username={user};Password={pwd};Timeout=30;Command Timeout=60;SSL Mode=Prefer;Trust Server Certificate=true";
        }
    }

    public static (bool sucesso, string mensagem) SalvarDoQrCode(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var host = root.GetProperty("host").GetString() ?? "";
            var porta = root.GetProperty("porta").GetString() ?? "5432";
            var database = root.GetProperty("database").GetString() ?? "";
            var usuario = root.GetProperty("usuario").GetString() ?? "";
            var senha = root.GetProperty("senha").GetString() ?? "";
            var empresaId = root.TryGetProperty("empresa_id", out var eid) ? eid.ToString() : "0";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database))
                return (false, "QR Code inválido: dados de conexão incompletos.");

            Preferences.Set(KeyHost, host);
            Preferences.Set(KeyPorta, porta);
            Preferences.Set(KeyDatabase, database);
            Preferences.Set(KeyUsuario, usuario);
            Preferences.Set(KeySenha, senha);
            Preferences.Set(KeyEmpresaId, empresaId);

            return (true, $"Conexão configurada: {host}:{porta}/{database}");
        }
        catch (Exception ex)
        {
            return (false, $"QR Code inválido: {ex.Message}");
        }
    }

    public static void Limpar()
    {
        Preferences.Remove(KeyHost);
        Preferences.Remove(KeyPorta);
        Preferences.Remove(KeyDatabase);
        Preferences.Remove(KeyUsuario);
        Preferences.Remove(KeySenha);
        Preferences.Remove(KeyEmpresaId);
    }

    public static long EmpresaId
    {
        get
        {
            var id = long.TryParse(Preferences.Get(KeyEmpresaId, "0"), out var v) ? v : 0;
            return id;
        }
        set => Preferences.Set(KeyEmpresaId, value.ToString());
    }

    /// <summary>
    /// Busca a primeira empresa ativa do banco se empresa_id não estiver configurada.
    /// Deve ser chamado após login.
    /// </summary>
    public static async Task CarregarEmpresaIdAsync()
    {
        if (EmpresaId > 0) return;
        try
        {
            var cs = ConnectionString + ";Timeout=10;Command Timeout=10";
            await using var conn = new Npgsql.NpgsqlConnection(cs);
            await conn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new Npgsql.NpgsqlCommand("SELECT codigo FROM empresas WHERE ativo = true ORDER BY codigo LIMIT 1", conn);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            if (result != null && result != DBNull.Value)
                EmpresaId = Convert.ToInt64(result);
        }
        catch { }
    }

    public static (string host, string porta, string database, string usuario) Info =>
        (Preferences.Get(KeyHost, ""), Preferences.Get(KeyPorta, ""), Preferences.Get(KeyDatabase, ""), Preferences.Get(KeyUsuario, ""));
}
