using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Devices;

/// <summary>Cria a instancia de leitor conforme a marca/configuracao.</summary>
public static class RfidReaderFactory
{
    public static IFixedRfidReader Criar(MarcaLeitor marca)
    {
        var cfg = AppConfig.Atual;
        return marca switch
        {
            MarcaLeitor.ChainwayUr4 => new ChainwayUr4TcpReader(cfg.LeitorHost, cfg.LeitorPorta),
            MarcaLeitor.Mock => new MockReader(),
            _ => new MockReader(),
        };
    }
}
