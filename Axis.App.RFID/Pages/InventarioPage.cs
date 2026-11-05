using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

/// <summary>
/// Inventario completo por RFID.
/// Le todas as tags, identifica produtos, conta encontrados vs esperados.
/// </summary>
public class InventarioPage : ContentPage
{
    private IRfidReader? _reader;
    private readonly InventarioService _svc = new();
    private readonly Dictionary<string, TagInfo?> _tagsLidas = new();
    private readonly string _sessaoId = $"INV-{DateTime.Now:yyyyMMdd-HHmmss}";

    private Label _lblContador = null!;
    private Label _lblIdent = null!;
    private Label _lblDesconhecidas = null!;
    private Label _lblStatus = null!;
    private Label _lblValorTotal = null!;
    private Label _lblPotencia = null!;
    private Slider _sliderPotencia = null!;
    private Button _btnIniciar = null!;
    private Button _btnParar = null!;
    private StackLayout _stackItens = null!;
    private ActivityIndicator _loading = null!;

    private readonly System.Globalization.CultureInfo _ci = new("pt-BR");

    public InventarioPage() { BuildUI(); }

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
                new StackLayout
                {
                    Spacing = 1, VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "📦 Inventario Completo", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = _sessaoId, FontSize = 11, TextColor = Color.FromArgb("#90A4AE") }
                    }
                }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        _lblContador = new Label { Text = "0", FontSize = 42, FontAttributes = FontAttributes.Bold, TextColor = cP, HorizontalTextAlignment = TextAlignment.Center };
        _lblIdent = new Label { Text = "0 identificadas", FontSize = 13, TextColor = Color.FromArgb("#2E7D32"), HorizontalTextAlignment = TextAlignment.Center };
        _lblDesconhecidas = new Label { Text = "0 desconhecidas", FontSize = 13, TextColor = Color.FromArgb("#E65100"), HorizontalTextAlignment = TextAlignment.Center };
        _lblValorTotal = new Label { Text = "R$ 0", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0"), HorizontalTextAlignment = TextAlignment.Center };
        _lblStatus = new Label { Text = "Pronto", FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };

        _btnIniciar = new Button { Text = "▶ Iniciar Leitura", BackgroundColor = Color.FromArgb("#2E7D32"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50 };
        _btnIniciar.Clicked += async (s, e) => await Iniciar();
        _btnParar = new Button { Text = "⏹ Parar", BackgroundColor = Color.FromArgb("#C62828"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50, IsVisible = false };
        _btnParar.Clicked += async (s, e) => await Parar();

        var btnSalvar = new Button { Text = "💾 Salvar Inventario", BackgroundColor = Color.FromArgb("#1565C0"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnSalvar.Clicked += async (s, e) => await SalvarInventario();

        var btnLimpar = new Button { Text = "🗑 Limpar", BackgroundColor = Color.FromArgb("#78909C"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnLimpar.Clicked += (s, e) => { _tagsLidas.Clear(); AtualizarUI(); };

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

        var kpiCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Stroke = Color.FromArgb("#E0E6ED"), StrokeThickness = 1, Padding = new Thickness(16, 14),
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#18000000")), Offset = new Point(0, 2), Radius = 6 },
            Content = new StackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Tags Lidas", FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center },
                    _lblContador, _lblIdent, _lblDesconhecidas, _lblValorTotal
                }
            }
        };

        var gridBtns = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10, Children = { btnSalvar, btnLimpar }
        };
        Grid.SetColumn(btnLimpar, 1);

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(14, 10, 14, 20),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { _lblStatus, _loading, potenciaCard, kpiCard, _btnIniciar, _btnParar, gridBtns, _stackItens }
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
        var (marca, tipo) = RfidReaderFactory.DetectarLeitor();
        _reader = RfidReaderFactory.Criar(marca, tipo);
        _reader.TagLida += OnTagLida;
        _reader.StatusChanged += s => MainThread.BeginInvokeOnMainThread(() => _lblStatus.Text = s);
        _reader.Erro += s => MainThread.BeginInvokeOnMainThread(() => { _lblStatus.Text = s; _lblStatus.TextColor = Color.FromArgb("#C62828"); });
        _lblStatus.Text = $"Leitor: {_reader.NomeDispositivo}";
        await _reader.ConectarAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await Parar();
    }

    private async Task Iniciar()
    {
        if (_reader == null) return;
        _btnIniciar.IsVisible = false;
        _btnParar.IsVisible = true;
        await _reader.IniciarLeituraAsync();
    }

    private async Task Parar()
    {
        if (_reader == null) return;
        _btnIniciar.IsVisible = true;
        _btnParar.IsVisible = false;
        await _reader.PararLeituraAsync();
    }

    private async void OnTagLida(string epc)
    {
        if (_tagsLidas.ContainsKey(epc)) return; // ja lida

        TagInfo? info = null;
        try { info = await _svc.BuscarPorTagAsync(epc); } catch { }
        _tagsLidas[epc] = info;

        if (info != null) BeepService.Beep(); else BeepService.BeepErro();

        MainThread.BeginInvokeOnMainThread(() => AtualizarUI());
    }

    private void AtualizarUI()
    {
        var total = _tagsLidas.Count;
        var ident = _tagsLidas.Values.Count(v => v != null);
        var desc = total - ident;
        var valor = _tagsLidas.Values.Where(v => v != null).Sum(v => v!.PrecoVenda);

        _lblContador.Text = total.ToString();
        _lblIdent.Text = $"{ident} identificadas";
        _lblDesconhecidas.Text = $"{desc} desconhecidas";
        _lblValorTotal.Text = valor.ToString("C0", _ci);

        _stackItens.Children.Clear();
        foreach (var kvp in _tagsLidas.Reverse())
        {
            var info = kvp.Value;
            var lbl = info != null
                ? new Label { Text = $"✅ {info.Referencia} - {info.Nome} ({info.Tamanho}) {info.PrecoVenda.ToString("C0", _ci)}", FontSize = 11, TextColor = Color.FromArgb("#263238") }
                : new Label { Text = $"❓ {kvp.Key}", FontSize = 11, TextColor = Color.FromArgb("#E65100") };
            _stackItens.Children.Add(lbl);
            if (_stackItens.Children.Count >= 50) break; // limita exibicao
        }
    }

    private async Task SalvarInventario()
    {
        if (_tagsLidas.Count == 0) { await DisplayAlert("Aviso", "Nenhuma tag lida.", "OK"); return; }
        _loading.IsRunning = true;
        try
        {
            await _svc.SalvarLeiturasAsync(_sessaoId, [.. _tagsLidas.Keys]);
            await DisplayAlert("Sucesso", $"{_tagsLidas.Count} tags salvas na sessao {_sessaoId}.", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
        finally { _loading.IsRunning = false; }
    }
}
