using System.Text.Json;

namespace Axis.App.RFID.Services;

public static class ConfigService
{
    private const string KeyHost = "rfid_host";
    private const string KeyPorta = "rfid_porta";
    private const string KeyDatabase = "rfid_database";
    private const string KeyUsuario = "rfid_usuario";
    private const string KeySenha = "rfid_senha";
    private const string KeyEmpresaId = "rfid_empresa_id";

    public static bool Configurado => !string.IsNullOrEmpty(Preferences.Get(KeyHost, ""));

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

    public static long EmpresaId
    {
        get => long.TryParse(Preferences.Get(KeyEmpresaId, "0"), out var v) ? v : 0;
        set => Preferences.Set(KeyEmpresaId, value.ToString());
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

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database))
                return (false, "QR Code invalido.");

            Preferences.Set(KeyHost, host);
            Preferences.Set(KeyPorta, porta);
            Preferences.Set(KeyDatabase, database);
            Preferences.Set(KeyUsuario, usuario);
            Preferences.Set(KeySenha, senha);
            return (true, $"Conexao: {host}:{porta}/{database}");
        }
        catch (Exception ex) { return (false, $"QR invalido: {ex.Message}"); }
    }

    public static void Limpar()
    {
        Preferences.Remove(KeyHost); Preferences.Remove(KeyPorta);
        Preferences.Remove(KeyDatabase); Preferences.Remove(KeyUsuario);
        Preferences.Remove(KeySenha); Preferences.Remove(KeyEmpresaId);
    }

    public static (string host, string porta, string database, string usuario) Info =>
        (Preferences.Get(KeyHost, ""), Preferences.Get(KeyPorta, ""), Preferences.Get(KeyDatabase, ""), Preferences.Get(KeyUsuario, ""));
}
