namespace Axis.App.RfidStation.Devices;

/// <summary>
/// Leitor simulado para desenvolvimento/testes sem hardware.
/// Gera leituras periodicas a partir de uma lista de EPCs de exemplo.
/// </summary>
public sealed class MockReader : IFixedRfidReader
{
    private readonly List<string> _epcs;
    private CancellationTokenSource? _cts;
    private readonly Random _rnd = new();

    public MockReader(IEnumerable<string>? epcs = null)
    {
        _epcs = epcs?.ToList() ?? new List<string>
        {
            "E28011700000020F1234ABCD",
            "E28011700000020F5678EF01",
            "E28011700000020F9ABC2345",
            "E28011700000020FDEAD6789",
        };
    }

    public string Nome => "Leitor Simulado (Mock)";
    public bool Conectado { get; private set; }
    public bool Lendo { get; private set; }

    public event Action<TagLeitura>? TagLida;
    public event Action<string>? Status;
    public event Action<string>? Erro;

    public Task<bool> ConectarAsync(CancellationToken ct = default)
    {
        Conectado = true;
        Status?.Invoke("Leitor simulado conectado.");
        return Task.FromResult(true);
    }

    public Task DesconectarAsync()
    {
        Conectado = false;
        Lendo = false;
        _cts?.Cancel();
        Status?.Invoke("Leitor simulado desconectado.");
        return Task.CompletedTask;
    }

    public Task IniciarInventarioAsync(CancellationToken ct = default)
    {
        if (Lendo) return Task.CompletedTask;
        Lendo = true;
        Status?.Invoke("Inventario simulado iniciado.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var epc = _epcs[_rnd.Next(_epcs.Count)];
                TagLida?.Invoke(TagLeitura.Agora(epc, _rnd.Next(-70, -30), _rnd.Next(1, 5)));
                try { await Task.Delay(600, token); } catch { break; }
            }
        }, token);
        return Task.CompletedTask;
    }

    public Task PararInventarioAsync()
    {
        Lendo = false;
        _cts?.Cancel();
        Status?.Invoke("Inventario simulado parado.");
        return Task.CompletedTask;
    }

    public Task DefinirPotenciaAsync(int dbm)
    {
        Status?.Invoke($"[Mock] Potencia {dbm} dBm.");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => new(DesconectarAsync());
}
