using Axis.App.Salao.Services;

namespace Axis.App.Salao.Services;
/// <summary>
/// Serviço de notificações locais (push no aparelho) para novos agendamentos.
/// Enquanto o app está em execução, um verificador periódico consulta o banco
/// e dispara uma notificação local sempre que surge um novo agendamento.
/// </summary>
public static class NotificacaoService
{
    private static long _ultimoCodigo;
    private static bool _monitorando;
    private static CancellationTokenSource? _cts;
    private const int IntervaloSegundos = 60;

    /// <summary>Inicia o monitoramento de novos agendamentos para o profissional logado.</summary>
    public static async Task IniciarMonitoramentoAsync()
    {
        if (_monitorando) return;
        _monitorando = true;
        _cts = new CancellationTokenSource();

        try
        {
            _ultimoCodigo = await SalaoDataService.ObterUltimoCodigoAgendamentoAsync(AuthService.ProfissionalId);
        }
        catch
        {
            _ultimoCodigo = 0;
        }

        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public static void PararMonitoramento()
    {
        _monitorando = false;
        _cts?.Cancel();
        _cts = null;
    }

    private static async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _monitorando)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(IntervaloSegundos), token);
                if (token.IsCancellationRequested) break;
                if (AuthService.ProfissionalId <= 0) continue;

                var novos = await SalaoDataService.ListarNovosDesdeAsync(_ultimoCodigo, AuthService.ProfissionalId);
                foreach (var ag in novos)
                {
                    _ultimoCodigo = Math.Max(_ultimoCodigo, ag.Codigo);
                    var cliente = string.IsNullOrWhiteSpace(ag.ClienteNome) ? "Cliente" : ag.ClienteNome;
                    var titulo = ag.EhEncaixe ? "Novo encaixe!" : "Novo agendamento!";
                    var msg = $"{cliente} • {ag.DataHora:dd/MM HH:mm}";
                    Notificar(titulo, msg);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // silencia falhas transitórias de rede; tenta novamente no próximo ciclo
            }
        }
    }

    /// <summary>Dispara uma notificação local no aparelho.</summary>
    public static void Notificar(string titulo, string mensagem)
    {
#if ANDROID
        Axis.App.Salao.Platforms.Android.NotificacaoAndroid.Mostrar(titulo, mensagem);
#endif
    }
}
