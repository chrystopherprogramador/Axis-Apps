using Axis.App.RfidStation.Devices;
using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Forms;

public partial class ImpressaoForm : Form
{
    private readonly InventarioService _inventario = new();
    private List<TagInfo> _resultados = new();

    public ImpressaoForm()
    {
        InitializeComponent();
    }

    private async void btnBuscar_Click(object sender, EventArgs e)
    {
        var termo = txtReferencia.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo)) { AtualizarStatus("Informe a referencia."); return; }

        AtualizarStatus("Buscando...");
        _resultados = await _inventario.BuscarPorReferenciaAsync(termo);
        lstProdutos.Items.Clear();
        foreach (var t in _resultados)
        {
            var tag = string.IsNullOrWhiteSpace(t.RfidTag) ? "(sem tag)" : t.RfidTag;
            lstProdutos.Items.Add($"{t.Nome} {t.Tamanho}  |  {t.PrecoVenda:C}  |  {tag}");
        }
        AtualizarStatus(_resultados.Count == 0 ? "Nada encontrado." : $"{_resultados.Count} variacao(oes).");
    }

    private async void btnImprimir_Click(object sender, EventArgs e)
    {
        var idx = lstProdutos.SelectedIndex;
        if (idx < 0 || idx >= _resultados.Count) { AtualizarStatus("Selecione um item."); return; }

        var produto = _resultados[idx];
        var epc = string.IsNullOrWhiteSpace(txtEpc.Text)
            ? GerarEpc(produto)
            : txtEpc.Text.Trim().ToUpperInvariant();

        var printer = ZebraRfidPrinter.Atual();
        AtualizarStatus("Enviando para impressora...");
        var (ok, msg) = await printer.ImprimirComEncodeAsync(produto, epc);
        if (ok && chkVincular.Checked)
        {
            try
            {
                await _inventario.VincularTagAsync(produto.VariacaoId, epc);
                msg += " | Tag vinculada a variacao.";
            }
            catch (Exception ex) { msg += $" | Erro ao vincular: {ex.Message}"; }
        }
        AtualizarStatus(msg);
    }

    private async void btnTestar_Click(object sender, EventArgs e)
    {
        var printer = ZebraRfidPrinter.Atual();
        AtualizarStatus("Testando impressora...");
        var (_, msg) = await printer.TestarAsync();
        AtualizarStatus(msg);
    }

    private void lstProdutos_SelectedIndexChanged(object sender, EventArgs e)
    {
        var idx = lstProdutos.SelectedIndex;
        if (idx >= 0 && idx < _resultados.Count)
            txtEpc.Text = _resultados[idx].RfidTag;
    }

    /// <summary>Gera um EPC de 24 hex (96 bits) baseado no id da variacao.</summary>
    private static string GerarEpc(TagInfo p)
    {
        var baseHex = p.VariacaoId.ToString("X").PadLeft(12, '0');
        var rnd = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return (baseHex + rnd)[..24];
    }

    private void AtualizarStatus(string msg)
    {
        lblStatus.Text = msg;
    }
}
