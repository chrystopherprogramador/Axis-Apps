using System.Text.Json;

namespace Axis.App.RfidStation.Services;

/// <summary>
/// Configuracao local persistida em disco (%AppData%\AxisRfidStation\config.json).
/// Guarda subdominio, connection string do banco master, connection string do tenant
/// resolvido e enderecos dos dispositivos RFID (leitor UR4 e impressora Zebra).
/// </summary>
public sealed class AppConfig
{
    public string Subdominio { get; set; } = "";
    public string MasterConnectionString { get; set; } = "";
    public string TenantConnectionString { get; set; } = "";
    public long EmpresaId { get; set; }

    // Conexao com o Axis Web (ponte leitora via SignalR)
    public string ServerUrl { get; set; } = "";
    public string Token { get; set; } = "";
    public long UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = "";
    public string NomeEstacao { get; set; } = Environment.MachineName;

    // Leitor fixo Chainway UR4 (4 portas UHF)
    public string LeitorHost { get; set; } = "192.168.1.100";
    public int LeitorPorta { get; set; } = 8160;
    public int PotenciaDbm { get; set; } = 30;

    // Impressora Zebra RFID
    public string ImpressoraHost { get; set; } = "192.168.1.200";
    public int ImpressoraPorta { get; set; } = 9100;

    private static string PastaConfig =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AxisRfidStation");

    private static string CaminhoArquivo => Path.Combine(PastaConfig, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppConfig Atual { get; private set; } = new();

    /// <summary>Carrega a configuracao do disco (ou cria padrao) e aplica o master do appsettings se vazio.</summary>
    public static AppConfig Carregar()
    {
        try
        {
            if (File.Exists(CaminhoArquivo))
            {
                var json = File.ReadAllText(CaminhoArquivo);
                Atual = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { Atual = new AppConfig(); }

        if (string.IsNullOrWhiteSpace(Atual.MasterConnectionString))
            Atual.MasterConnectionString = LerMasterDoAppSettings();

        return Atual;
    }

    public void Salvar()
    {
        try
        {
            Directory.CreateDirectory(PastaConfig);
            File.WriteAllText(CaminhoArquivo, JsonSerializer.Serialize(this, JsonOpts));
            Atual = this;
        }
        catch { /* falha de disco nao deve derrubar o app */ }
    }

    public bool TenantConfigurado => !string.IsNullOrWhiteSpace(TenantConnectionString);

    private static string LerMasterDoAppSettings()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(caminho)) return "";
            using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
            if (doc.RootElement.TryGetProperty("MasterConnectionString", out var v))
                return v.GetString() ?? "";
        }
        catch { }
        return "";
    }
}
