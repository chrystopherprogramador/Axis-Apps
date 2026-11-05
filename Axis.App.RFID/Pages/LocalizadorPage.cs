using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

/// <summary>
/// Localizador: busca por referencia e conta quantas EPCs daquela referencia foram encontradas.
/// </summary>
public class LocalizadorPage : ContentPage
{
    private IRfidReader? _reader;
    private readonly InventarioService _svc = new();
    private List<TagInfo> _tagsEsperadas = [];
    private readonly HashSet<string> _tagsEncontradas = [];

    private Entry _txtRef = null!;
    private Label _lblProduto = null!;
    private Label _lblContador = null!;
    private Label _lblTotal = null!;
    private Label _lblStatus = null!;
    private Label _lblPotencia = null!;
    private Slider _sliderPotencia = null!;
    private Button _btnBuscar = null!;
    private Button _btnIniciar = null!;
    private Button _btnParar = null!;
    private StackLayout _stackItens = null!;

    public LocalizadorPage() { BuildUI(); }

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
                new Label { Text = "🔍 Localizador", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        _txtRef = new Entry { Placeholder = "Referencia do produto", FontSize = 15, BackgroundColor = Colors.White };
        _btnBuscar = new Button { Text = "Buscar", BackgroundColor = Color.FromArgb("#1565C0"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 42 };
        _btnBuscar.Clicked += async (s, e) => await BuscarReferencia();

        _lblProduto = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = cP, IsVisible = false };
        _lblContador = new Label { Text = "0 / 0", FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0"), HorizontalTextAlignment = TextAlignment.Center };
        _lblTotal = new Label { FontSize = 13, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };
        _lblStatus = new Label { FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };

        _btnIniciar = new Button { Text = "▶ Iniciar Leitura", BackgroundColor = Color.FromArgb("#2E7D32"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50, IsVisible = false };
        _btnIniciar.Clicked += async (s, e) => await Iniciar();
        _btnParar = new Button { Text = "⏹ Parar", BackgroundColor = Color.FromArgb("#C62828"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50, IsVisible = false };
        _btnParar.Clicked += async (s, e) => await Parar();

        _stackItens = new StackLayout { Spacing = 4 };

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

        var searchRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8, Children = { _txtRef, _btnBuscar }
        };
        Grid.SetColumn(_btnBuscar, 1);

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
                        Children = { searchRow, potenciaCard, _lblProduto, _lblContador, _lblTotal, _lblStatus, _btnIniciar, _btnParar, _stackItens }
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
        _reader.Erro += s => MainThread.BeginInvokeOnMainThread(() => _lblStatus.Text = s);
        await _reader.ConectarAsync();
    }

    protected override async void OnDisappearing() { base.OnDisappearing(); await Parar(); }

    private async Task BuscarReferencia()
    {
        var refTxt = _txtRef.Text?.Trim();
        if (string.IsNullOrEmpty(refTxt)) return;

        try
        {
            _tagsEsperadas = await _svc.BuscarPorReferenciaAsync(refTxt);
            _tagsEncontradas.Clear();

            if (_tagsEsperadas.Count == 0)
            {
                _lblProduto.Text = "Nenhum produto com tag RFID para esta referencia.";
                _lblProduto.IsVisible = true;
                _btnIniciar.IsVisible = false;
                return;
            }

            var primeiro = _tagsEsperadas[0];
            _lblProduto.Text = $"{primeiro.Referencia} - {primeiro.Nome}";
            _lblProduto.IsVisible = true;
            _lblTotal.Text = $"{_tagsEsperadas.Count} tags esperadas";
            _btnIniciar.IsVisible = true;
            AtualizarUI();
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
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
        if (esperada == null) return;
        if (!_tagsEncontradas.Add(epc)) return;

        BeepService.BeepDuplo();
        MainThread.BeginInvokeOnMainThread(() => AtualizarUI());
    }

    private void AtualizarUI()
    {
        _lblContador.Text = $"{_tagsEncontradas.Count} / {_tagsEsperadas.Count}";
        var pct = _tagsEsperadas.Count > 0 ? _tagsEncontradas.Count * 100 / _tagsEsperadas.Count : 0;
        _lblContador.TextColor = pct >= 100 ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0");

        _stackItens.Children.Clear();
        foreach (var tag in _tagsEsperadas)
        {
            var found = _tagsEncontradas.Contains(tag.RfidTag.ToUpperInvariant());
            var icon = found ? "✅" : "⬜";
            var color = found ? Color.FromArgb("#2E7D32") : Color.FromArgb("#B0BEC5");
            _stackItens.Children.Add(new Label { Text = $"{icon} {tag.Tamanho} - {tag.RfidTag}", FontSize = 12, TextColor = color });
        }
    }
}
