using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

/// <summary>
/// Inventario parcial: seleciona classificacao e faz inventario apenas dos produtos daquela classificacao.
/// </summary>
public class InventarioParcialPage : ContentPage
{
    private IRfidReader? _reader;
    private readonly InventarioService _svc = new();
    private List<TagInfo> _tagsEsperadas = [];
    private readonly HashSet<string> _tagsEncontradas = [];
    private readonly HashSet<string> _tagsDesconhecidas = [];
    private string _sessaoId = "";

    private Picker _pkTipo = null!;
    private Picker _pkValor = null!;
    private Label _lblContador = null!;
    private Label _lblResumo = null!;
    private Label _lblStatus = null!;
    private Label _lblPotencia = null!;
    private Slider _sliderPotencia = null!;
    private Button _btnIniciar = null!;
    private Button _btnParar = null!;
    private StackLayout _stackItens = null!;
    private ActivityIndicator _loading = null!;

    private List<(long tipoId, string tipoNome, List<(long valId, string valNome)> valores)> _classificacoes = [];
    private readonly System.Globalization.CultureInfo _ci = new("pt-BR");

    public InventarioParcialPage() { BuildUI(); }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");

        var btnVoltar = new Button { Text = "←", FontSize = 22, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HeightRequest = 44, Padding = 0, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => { await Parar(); await Navigation.PopAsync(); };

        var header = new Grid
        {
            BackgroundColor = cP, Padding = new Thickness(12, 48, 20, 14),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                btnVoltar,
                new Label { Text = "📋 Inventario Parcial", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        _pkTipo = new Picker { Title = "Tipo de Classificacao", FontSize = 14 };
        _pkTipo.SelectedIndexChanged += OnTipoChanged;
        _pkValor = new Picker { Title = "Valor", FontSize = 14, IsEnabled = false };

        var btnCarregar = new Button { Text = "Carregar Produtos", BackgroundColor = Color.FromArgb("#1565C0"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 42 };
        btnCarregar.Clicked += async (s, e) => await CarregarProdutos();

        _lblContador = new Label { Text = "0 / 0", FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0"), HorizontalTextAlignment = TextAlignment.Center };
        _lblResumo = new Label { FontSize = 13, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };
        _lblStatus = new Label { FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };

        _btnIniciar = new Button { Text = "▶ Iniciar Leitura", BackgroundColor = Color.FromArgb("#2E7D32"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50, IsVisible = false };
        _btnIniciar.Clicked += async (s, e) => await Iniciar();
        _btnParar = new Button { Text = "⏹ Parar", BackgroundColor = Color.FromArgb("#C62828"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50, IsVisible = false };
        _btnParar.Clicked += async (s, e) => await Parar();

        var btnSalvar = new Button { Text = "💾 Salvar", BackgroundColor = Color.FromArgb("#1565C0"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnSalvar.Clicked += async (s, e) => await SalvarInventario();

        _stackItens = new StackLayout { Spacing = 4 };
        _loading = new ActivityIndicator { Color = cP, IsRunning = false, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center };

        // Controle de potencia
        _lblPotencia = new Label { Text = "Potencia: 20 dBm", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#78909C") };
        _sliderPotencia = new Slider { Minimum = 5, Maximum = 30, Value = 20, MinimumTrackColor = cP, MaximumTrackColor = Color.FromArgb("#CFD8DC"), ThumbColor = cP };
        _sliderPotencia.ValueChanged += async (s, e) =>
        {
            var val = (int)e.NewValue;
            _lblPotencia.Text = $"Potencia: {val} dBm";
            if (_reader != null) await _reader.SetPotenciaAsync(val);
        };
        var potenciaCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Stroke = Color.FromArgb("#E0E6ED"), StrokeThickness = 1, Padding = new Thickness(12, 8),
            Content = new StackLayout { Spacing = 4, Children = { _lblPotencia, _sliderPotencia } }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(14, 12, 14, 20),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { _pkTipo, _pkValor, btnCarregar, potenciaCard, _loading, _lblContador, _lblResumo, _lblStatus, _btnIniciar, _btnParar, btnSalvar, _stackItens }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Conectar leitor
        var (marca, tipo) = RfidReaderFactory.DetectarLeitor();
        _reader = RfidReaderFactory.Criar(marca, tipo);
        _reader.TagLida += OnTagLida;
        _reader.StatusChanged += s => MainThread.BeginInvokeOnMainThread(() => _lblStatus.Text = s);
        _reader.Erro += s => MainThread.BeginInvokeOnMainThread(() => _lblStatus.Text = s);
        await _reader.ConectarAsync();

        // Carregar classificacoes
        try
        {
            _loading.IsRunning = true;
            _classificacoes = await _svc.ListarClassificacoesAsync();
            _pkTipo.Items.Clear();
            foreach (var c in _classificacoes) _pkTipo.Items.Add(c.tipoNome);
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
        finally { _loading.IsRunning = false; }
    }

    protected override async void OnDisappearing() { base.OnDisappearing(); await Parar(); }

    private void OnTipoChanged(object? sender, EventArgs e)
    {
        _pkValor.Items.Clear();
        _pkValor.IsEnabled = false;
        if (_pkTipo.SelectedIndex < 0) return;
        var vals = _classificacoes[_pkTipo.SelectedIndex].valores;
        foreach (var v in vals) _pkValor.Items.Add(v.valNome);
        _pkValor.IsEnabled = vals.Count > 0;
    }

    private async Task CarregarProdutos()
    {
        if (_pkTipo.SelectedIndex < 0 || _pkValor.SelectedIndex < 0) { await DisplayAlert("Aviso", "Selecione tipo e valor.", "OK"); return; }
        var valId = _classificacoes[_pkTipo.SelectedIndex].valores[_pkValor.SelectedIndex].valId;
        _sessaoId = $"PARC-{DateTime.Now:yyyyMMdd-HHmmss}";

        _loading.IsRunning = true;
        try
        {
            _tagsEsperadas = await _svc.BuscarPorClassificacaoAsync(valId);
            _tagsEncontradas.Clear();
            _tagsDesconhecidas.Clear();

            if (_tagsEsperadas.Count == 0) { _lblResumo.Text = "Nenhum produto com tag RFID nesta classificacao."; return; }

            _lblResumo.Text = $"{_tagsEsperadas.Count} tags esperadas";
            _btnIniciar.IsVisible = true;
            AtualizarUI();
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
        finally { _loading.IsRunning = false; }
    }

    private async Task Iniciar()
    {
        if (_reader == null) return;
        _btnIniciar.IsVisible = false; _btnParar.IsVisible = true;
        await _reader.IniciarLeituraAsync();
    }

    private async Task Parar()
    {
        if (_reader == null) return;
        _btnIniciar.IsVisible = true; _btnParar.IsVisible = false;
        await _reader.PararLeituraAsync();
    }

    private void OnTagLida(string epc)
    {
        var esperada = _tagsEsperadas.FirstOrDefault(t => t.RfidTag.Equals(epc, StringComparison.OrdinalIgnoreCase));
        if (esperada != null) { if (!_tagsEncontradas.Add(epc)) return; BeepService.Beep(); }
        else { if (!_tagsDesconhecidas.Add(epc)) return; BeepService.BeepErro(); }

        MainThread.BeginInvokeOnMainThread(() => AtualizarUI());
    }

    private void AtualizarUI()
    {
        var found = _tagsEncontradas.Count;
        var expected = _tagsEsperadas.Count;
        _lblContador.Text = $"{found} / {expected}";
        var pct = expected > 0 ? found * 100 / expected : 0;
        _lblContador.TextColor = pct >= 100 ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0");

        var valor = _tagsEsperadas.Where(t => _tagsEncontradas.Contains(t.RfidTag.ToUpperInvariant())).Sum(t => t.PrecoVenda);
        _lblResumo.Text = $"{found}/{expected} encontradas ({pct}%) | {valor.ToString("C0", _ci)} | {_tagsDesconhecidas.Count} fora";

        _stackItens.Children.Clear();
        // Agrupado por referencia
        var grupos = _tagsEsperadas.GroupBy(t => t.Referencia);
        foreach (var grp in grupos)
        {
            var totalGrp = grp.Count();
            var foundGrp = grp.Count(t => _tagsEncontradas.Contains(t.RfidTag.ToUpperInvariant()));
            var cor = foundGrp == totalGrp ? Color.FromArgb("#2E7D32") : foundGrp > 0 ? Color.FromArgb("#E65100") : Color.FromArgb("#B0BEC5");
            var icon = foundGrp == totalGrp ? "✅" : foundGrp > 0 ? "🟡" : "⬜";
            _stackItens.Children.Add(new Label { Text = $"{icon} {grp.Key} - {grp.First().Nome}  ({foundGrp}/{totalGrp})", FontSize = 12, TextColor = cor });
        }
    }

    private async Task SalvarInventario()
    {
        var todas = _tagsEncontradas.Union(_tagsDesconhecidas).ToList();
        if (todas.Count == 0) { await DisplayAlert("Aviso", "Nenhuma tag lida.", "OK"); return; }
        _loading.IsRunning = true;
        try
        {
            await _svc.SalvarLeiturasAsync(_sessaoId, todas);
            await DisplayAlert("Sucesso", $"{todas.Count} tags salvas ({_sessaoId}).", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
        finally { _loading.IsRunning = false; }
    }
}
