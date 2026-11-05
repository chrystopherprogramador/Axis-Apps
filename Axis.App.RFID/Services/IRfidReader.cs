namespace Axis.App.RFID.Services;

/// <summary>
/// Interface para abstração do leitor RFID.
/// Suporta UROVO e Chainway, Serial e Bluetooth.
/// </summary>
public interface IRfidReader
{
    string NomeDispositivo { get; }
    bool Conectado { get; }

    event Action<string> TagLida;
    event Action<string> StatusChanged;
    event Action<string> Erro;

    Task<bool> ConectarAsync();
    Task DesconectarAsync();
    Task IniciarLeituraAsync();
    Task PararLeituraAsync();
    Task<int> SetPotenciaAsync(int dbm);
}

/// <summary>Tipo de conexao do leitor.</summary>
public enum TipoConexao { Serial, Bluetooth }

/// <summary>Marca do leitor.</summary>
public enum MarcaLeitor { UROVO, Chainway, Generico }
