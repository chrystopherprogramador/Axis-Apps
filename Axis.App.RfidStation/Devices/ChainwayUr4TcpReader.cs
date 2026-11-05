using System.Net.Sockets;
using System.Text;

namespace Axis.App.RfidStation.Devices;

/// <summary>
/// Leitor Chainway UR4 (4 portas UHF) via TCP/IP.
/// IMPORTANTE: o protocolo exato do UR4 depende do SDK/firmware. Esta implementacao
/// abre um socket TCP, envia um comando de inicio de inventario e faz o parse de
/// frames de resposta em formato "EPC,RSSI,ANTENA" (linha por leitura) como esqueleto
/// generico. Ajustar o encode/decode ao protocolo real (LLRP ou proprietario Chainway)
/// quando o SDK/manual estiver disponivel.
/// </summary>
public sealed class ChainwayUr4TcpReader : IFixedRfidReader
{
    private readonly string _host;
    private readonly int _porta;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _loopCts;
    private int _potencia = 30;

    public ChainwayUr4TcpReader(string host, int porta)
    {
        _host = host;
        _porta = porta;
    }

    public string Nome => $"Chainway UR4 ({_host}:{_porta})";
    public bool Conectado => _client?.Connected == true;
    public bool Lendo { get; private set; }

    public event Action<TagLeitura>? TagLida;
    public event Action<string>? Status;
    public event Action<string>? Erro;

    public async Task<bool> ConectarAsync(CancellationToken ct = default)
    {
        try
        {
            Status?.Invoke($"Conectando ao leitor {_host}:{_porta}...");
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _porta, ct);
            _stream = _client.GetStream();
            Status?.Invoke("Leitor conectado.");
            return true;
        }
        catch (Exception ex)
        {
            Erro?.Invoke($"Falha ao conectar no leitor: {ex.Message}");
            return false;
        }
    }

    public async Task DesconectarAsync()
    {
        await PararInventarioAsync();
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        Status?.Invoke("Leitor desconectado.");
    }

    public async Task IniciarInventarioAsync(CancellationToken ct = default)
    {
        if (!Conectado || _stream is null)
        {
            Erro?.Invoke("Leitor nao conectado.");
            return;
        }
        if (Lendo) return;

        // Comando generico de inicio (ajustar ao protocolo real)
        await EnviarComandoAsync("START_INVENTORY");
        Lendo = true;
        Status?.Invoke("Inventario iniciado.");

        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => LoopLeituraAsync(_loopCts.Token));
    }

    public async Task PararInventarioAsync()
    {
        if (!Lendo) return;
        try
        {
            _loopCts?.Cancel();
            if (_stream is not null)
                await EnviarComandoAsync("STOP_INVENTORY");
        }
        catch { }
        finally
        {
            Lendo = false;
            Status?.Invoke("Inventario parado.");
        }
    }

    public async Task DefinirPotenciaAsync(int dbm)
    {
        _potencia = Math.Clamp(dbm, 5, 33);
        if (_stream is not null)
            await EnviarComandoAsync($"SET_POWER {_potencia}");
        Status?.Invoke($"Potencia ajustada para {_potencia} dBm.");
    }

    private async Task LoopLeituraAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();
        try
        {
            while (!ct.IsCancellationRequested && _stream is not null)
            {
                int lidos = await _stream.ReadAsync(buffer, ct);
                if (lidos <= 0) break;
                sb.Append(Encoding.ASCII.GetString(buffer, 0, lidos));

                int nl;
                while ((nl = sb.ToString().IndexOf('\n')) >= 0)
                {
                    var linha = sb.ToString(0, nl).Trim();
                    sb.Remove(0, nl + 1);
                    ProcessarLinha(linha);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Erro?.Invoke($"Erro na leitura: {ex.Message}"); }
    }

    private void ProcessarLinha(string linha)
    {
        if (string.IsNullOrWhiteSpace(linha)) return;
        // Formato esperado (esqueleto): EPC,RSSI,ANTENA
        var partes = linha.Split(',', StringSplitOptions.TrimEntries);
        var epc = partes[0];
        if (string.IsNullOrWhiteSpace(epc)) return;
        int rssi = partes.Length > 1 && int.TryParse(partes[1], out var r) ? r : 0;
        int antena = partes.Length > 2 && int.TryParse(partes[2], out var a) ? a : 1;
        TagLida?.Invoke(TagLeitura.Agora(epc, rssi, antena));
    }

    private async Task EnviarComandoAsync(string comando)
    {
        if (_stream is null) return;
        var bytes = Encoding.ASCII.GetBytes(comando + "\r\n");
        await _stream.WriteAsync(bytes);
        await _stream.FlushAsync();
    }

    public async ValueTask DisposeAsync() => await DesconectarAsync();
}
