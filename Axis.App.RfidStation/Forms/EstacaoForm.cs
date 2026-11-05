using System.Collections.Concurrent;
using Axis.App.RfidStation.Connectivity;
using Axis.App.RfidStation.Devices;
using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Forms;

public partial class EstacaoForm : Form
{
    private readonly WebBridgeConnection _web = new();
    private IFixedRfidReader? _leitor;
    private readonly ConcurrentDictionary<string, byte> _distintas = new(StringComparer.OrdinalIgnoreCase);
    private long _totalTransmitidas;

    public EstacaoForm()
    {
        InitializeComponent();
    }

    private async void EstacaoForm_Load(object sender, EventArgs e)
    {
        lblTenant.Text = $"Tenant: {AppConfig.Atual.Subdominio}   |   Usuario: {AuthService.UsuarioNome}";
        cboLeitor.Items.AddRange(new object[] { "Chainway UR4 (TCP)", "Simulado (Mock)" });
        cboLeitor.SelectedIndex = 0;

        _web.Status += s => AtualizarWeb(s);
        _web.Erro += s => AtualizarWeb("ERRO: " + s, true);

        AtualizarStatus("Conecte o leitor para comecar.");
        await _web.ConectarAsync();
    }

    private async void btnConectar_Click(object sender, EventArgs e)
    {
        if (_leitor?.Conectado == true)
        {
            await _leitor.DesconectarAsync();
            _leitor = null;
            btnConectar.Text = "Conectar leitor";
            btnIniciar.Enabled = false;
            return;
        }

        var marca = cboLeitor.SelectedIndex == 1 ? MarcaLeitor.Mock : MarcaLeitor.ChainwayUr4;
        _leitor = RfidReaderFactory.Criar(marca);
        _leitor.Status += s => AtualizarStatus(s);
        _leitor.Erro += s => AtualizarStatus("ERRO: " + s, true);
        _leitor.TagLida += OnTagLida;

        var ok = await _leitor.ConectarAsync();
        if (ok)
        {
            await _leitor.DefinirPotenciaAsync(AppConfig.Atual.PotenciaDbm);
            btnConectar.Text = "Desconectar";
            btnIniciar.Enabled = true;
        }
    }

    private async void btnIniciar_Click(object sender, EventArgs e)
    {
        if (_leitor is null) return;

        if (_leitor.Lendo)
        {
            await _leitor.PararInventarioAsync();
            btnIniciar.Text = "Iniciar leitura";
            AtualizarStatus("Leitura parada.");
            return;
        }

        if (!_web.Conectado)
        {
            AtualizarStatus("Sem conexao com o Axis Web. Reconectando...", true);
            await _web.ConectarAsync();
            if (!_web.Conectado) return;
        }

        _distintas.Clear();
        _totalTransmitidas = 0;
        lstTags.Items.Clear();
        AtualizarContadores();
        await _leitor.IniciarInventarioAsync();
        btnIniciar.Text = "Parar leitura";
        AtualizarStatus("Leitura iniciada. Transmitindo ao Axis Web...");
    }

    private async void OnTagLida(TagLeitura leitura)
    {
        // A ponte NAO grava no banco: apenas transmite o EPC ao Axis Web.
        var nova = _distintas.TryAdd(leitura.Epc, 0);
        _totalTransmitidas++;

        await _web.EnviarLeiturasAsync(new[] { (leitura.Epc, leitura.Rssi, leitura.Antena) });

        if (nova)
        {
            BeginInvoke(() =>
            {
                lstTags.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {leitura.Epc}  (ant {leitura.Antena}, rssi {leitura.Rssi})");
                if (lstTags.Items.Count > 500) lstTags.Items.RemoveAt(lstTags.Items.Count - 1);
                AtualizarContadores();
            });
        }
        else
        {
            BeginInvoke(AtualizarContadores);
        }
    }

    private void btnImprimir_Click(object sender, EventArgs e)
    {
        using var f = new ImpressaoForm();
        f.ShowDialog(this);
    }

    private void AtualizarContadores()
        => lblTotais.Text = $"EPCs distintos: {_distintas.Count}   |   Transmitidas: {_totalTransmitidas}";

    private void AtualizarStatus(string msg, bool erro = false)
    {
        if (InvokeRequired) { BeginInvoke(() => AtualizarStatus(msg, erro)); return; }
        lblStatus.ForeColor = erro ? Color.Firebrick : Color.DimGray;
        lblStatus.Text = msg;
    }

    private void AtualizarWeb(string msg, bool erro = false)
    {
        if (InvokeRequired) { BeginInvoke(() => AtualizarWeb(msg, erro)); return; }
        lblWeb.ForeColor = erro ? Color.Firebrick : (_web.Conectado ? Color.Green : Color.DimGray);
        lblWeb.Text = "Axis Web: " + msg;
    }

    private async void EstacaoForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_leitor is not null) await _leitor.DisposeAsync();
        await _web.DisposeAsync();
    }
}
