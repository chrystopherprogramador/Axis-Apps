using Axis.App.RfidStation.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace Axis.App.RfidStation.Connectivity;

/// <summary>
/// Ponte entre o leitor RFID local e o Axis Web.
/// Conecta ao hub /hubs/rfid-reader e transmite os EPCs lidos em tempo real.
/// A operacao (entrada de itens/inventario) acontece toda no Axis Web.
/// </summary>
public sealed class WebBridgeConnection : IAsyncDisposable
{
    private HubConnection? _hub;

    public event Action<string>? Status;
    public event Action<string>? Erro;

    public bool Conectado => _hub?.State == HubConnectionState.Connected;

    public async Task<bool> ConectarAsync(CancellationToken ct = default)
    {
        var cfg = AppConfig.Atual;
        if (string.IsNullOrWhiteSpace(cfg.ServerUrl))
        {
            Erro?.Invoke("URL do Axis Web nao configurada.");
            return false;
        }

        var baseUrl = cfg.ServerUrl.TrimEnd('/');
        var token = Uri.EscapeDataString(cfg.Token ?? "");
        var hubUrl = $"{baseUrl}/hubs/rfid-reader?access_token={token}";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, opt =>
            {
                opt.AccessTokenProvider = () => Task.FromResult<string?>(cfg.Token);
                opt.HttpMessageHandlerFactory = inner =>
                {
                    if (inner is HttpClientHandler h)
                        h.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return inner;
                };
                opt.WebSocketConfiguration = ws =>
                    ws.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)
            })
            .Build();

        _hub.Reconnecting += _ => { Status?.Invoke("Reconectando ao Axis Web..."); return Task.CompletedTask; };
        _hub.Reconnected += _ => RegistrarAsync();
        _hub.Closed += _ => { Status?.Invoke("Conexao com o Axis Web encerrada."); return Task.CompletedTask; };

        try
        {
            Status?.Invoke($"Conectando ao Axis Web ({baseUrl})...");
            await _hub.StartAsync(ct);
            await RegistrarAsync();
            Status?.Invoke("Conectado ao Axis Web.");
            return true;
        }
        catch (Exception ex)
        {
            Erro?.Invoke($"Falha ao conectar no Axis Web: {ex.Message}");
            return false;
        }
    }

    private async Task RegistrarAsync()
    {
        if (_hub is null) return;
        var cfg = AppConfig.Atual;
        try
        {
            await _hub.InvokeAsync("RegistrarLeitor", new
            {
                Subdominio = cfg.Subdominio,
                EmpresaId = cfg.EmpresaId,
                UsuarioId = (long?)cfg.UsuarioId,
                Nome = cfg.NomeEstacao
            });
            Status?.Invoke("Leitor registrado no Axis Web.");
        }
        catch (Exception ex)
        {
            Erro?.Invoke($"Falha ao registrar leitor: {ex.Message}");
        }
    }

    /// <summary>Envia um lote de EPCs lidos para o Axis Web.</summary>
    public async Task EnviarLeiturasAsync(IReadOnlyCollection<(string Epc, int Rssi, int Antena)> leituras)
    {
        if (_hub is null || _hub.State != HubConnectionState.Connected) return;
        if (leituras.Count == 0) return;
        try
        {
            var epcs = leituras.Select(l => new { l.Epc, l.Rssi, l.Antena }).ToArray();
            await _hub.InvokeAsync("EnviarLeituras", new { Epcs = epcs });
        }
        catch (Exception ex)
        {
            Erro?.Invoke($"Falha ao enviar leituras: {ex.Message}");
        }
    }

    public async Task DesconectarAsync()
    {
        if (_hub is not null)
        {
            await _hub.StopAsync();
            Status?.Invoke("Desconectado do Axis Web.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}
