namespace Axis.App.RfidStation.Devices;

/// <summary>
/// Abstracao de leitor RFID UHF fixo (ex.: Chainway UR4 4 portas).
/// Expoe eventos de tag lida e ciclo de vida de conexao/inventario.
/// </summary>
public interface IFixedRfidReader : IAsyncDisposable
{
    string Nome { get; }
    bool Conectado { get; }
    bool Lendo { get; }

    /// <summary>Disparado a cada tag lida (EPC, RSSI, numero da antena/porta).</summary>
    event Action<TagLeitura>? TagLida;
    event Action<string>? Status;
    event Action<string>? Erro;

    Task<bool> ConectarAsync(CancellationToken ct = default);
    Task DesconectarAsync();
    Task IniciarInventarioAsync(CancellationToken ct = default);
    Task PararInventarioAsync();
    Task DefinirPotenciaAsync(int dbm);
}

/// <summary>Dados de uma leitura de tag.</summary>
public readonly record struct TagLeitura(string Epc, int Rssi, int Antena, DateTime Momento)
{
    public static TagLeitura Agora(string epc, int rssi = 0, int antena = 1)
        => new(epc.ToUpperInvariant(), rssi, antena, DateTime.Now);
}

/// <summary>Marca do leitor fixo suportado.</summary>
public enum MarcaLeitor { ChainwayUr4, Mock }
