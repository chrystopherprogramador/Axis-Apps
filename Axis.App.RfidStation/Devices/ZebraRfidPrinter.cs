using System.Net.Sockets;
using System.Text;
using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Devices;

/// <summary>
/// Impressora Zebra RFID via ZPL sobre TCP (porta 9100).
/// Gera etiqueta com nome/referencia/preco + codigo de barras e faz o encode
/// do EPC na tag RFID via comando ^RFW,H (write EPC).
/// </summary>
public sealed class ZebraRfidPrinter : ILabelPrinter
{
    private readonly string _host;
    private readonly int _porta;

    public ZebraRfidPrinter(string host, int porta)
    {
        _host = host;
        _porta = porta;
    }

    public string Nome => $"Zebra RFID ({_host}:{_porta})";

    public async Task<(bool sucesso, string mensagem)> ImprimirComEncodeAsync(TagInfo produto, string epc)
    {
        if (string.IsNullOrWhiteSpace(epc))
            return (false, "EPC vazio.");

        var epcHex = epc.Trim().ToUpperInvariant();
        var zpl = MontarZpl(produto, epcHex);
        return await EnviarAsync(zpl);
    }

    public async Task<(bool sucesso, string mensagem)> TestarAsync()
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _porta);
            // ~HS solicita status da impressora
            var stream = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes("~HS\r\n");
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            return (true, $"Impressora respondeu em {_host}:{_porta}.");
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao conectar na impressora: {ex.Message}");
        }
    }

    private static string MontarZpl(TagInfo p, string epcHex)
    {
        var nome = Sanitizar(p.Nome);
        var refTam = Sanitizar($"{p.Referencia} {p.Tamanho}".Trim());
        var preco = p.PrecoVenda.ToString("C");
        var codigoBarras = string.IsNullOrWhiteSpace(p.Sku) ? epcHex : p.Sku;

        var sb = new StringBuilder();
        sb.Append("^XA");
        // Encode do EPC na tag RFID (write, hex)
        sb.Append("^RS8");
        sb.Append($"^RFW,H^FD{epcHex}^FS");
        // Layout visual
        sb.Append($"^CF0,30^FO30,30^FD{nome}^FS");
        sb.Append($"^CF0,25^FO30,70^FD{refTam}^FS");
        sb.Append($"^CF0,35^FO30,105^FD{preco}^FS");
        sb.Append($"^BY2,2,60^FO30,150^BCN,60,Y,N,N^FD{codigoBarras}^FS");
        sb.Append("^XZ");
        return sb.ToString();
    }

    private async Task<(bool, string)> EnviarAsync(string zpl)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _porta);
            var stream = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes(zpl);
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            return (true, "Etiqueta enviada para impressao.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao imprimir: {ex.Message}");
        }
    }

    private static string Sanitizar(string s) =>
        (s ?? "").Replace("^", " ").Replace("~", " ").Trim();

    public static ILabelPrinter Atual()
    {
        var c = AppConfig.Atual;
        return new ZebraRfidPrinter(c.ImpressoraHost, c.ImpressoraPorta);
    }
}
