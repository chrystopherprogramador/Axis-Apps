using Axis.App.Vendedor.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Axis.App.Vendedor.Pages;

public class BuscarProdutoPage : ContentPage
{
    private readonly Entry _txtBusca;
    private readonly StackLayout _listaStack;
    private readonly Label _lblStatus;
    private readonly Action<ProdutoDto>? _onSelecionado;
    private CameraBarcodeReaderView? _scanner;
    private StackLayout _scannerContainer = null!;
    private Button _btnScan = null!;
    private bool _jaProcessou;

    public BuscarProdutoPage(Action<ProdutoDto>? onSelecionado = null)
    {
        _onSelecionado = onSelecionado;
        BackgroundColor = Color.FromArgb("#D6E8FF");

        var cAzul = Color.FromArgb("#2A66B8");

        _txtBusca = new Entry { Placeholder = "Nome, referencia, SKU ou EAN...", FontSize = 15 };
        _txtBusca.Completed += async (s, e) => await BuscarAsync();

        var btnBuscar = new Button { Text = "Buscar", BackgroundColor = cAzul, TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnBuscar.Clicked += async (s, e) => await BuscarAsync();

        _btnScan = new Button { Text = "Escanear Codigo de Barras", BackgroundColor = Color.FromArgb("#1E4A8C"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        _btnScan.Clicked += OnAbrirScannerClicked;

        _scannerContainer = new StackLayout { IsVisible = false, HeightRequest = 250 };

        _lblStatus = new Label { FontSize = 12, TextColor = Color.FromArgb("#666"), HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
        _listaStack = new StackLayout { Spacing = 8 };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                new StackLayout
                {
                    BackgroundColor = cAzul, Padding = new Thickness(20, 50, 20, 16),
                    Children = { new Label { Text = "Buscar Produto", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center } }
                },
                new ScrollView
                {
                    Padding = new Thickness(16, 12),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { _txtBusca, btnBuscar, _btnScan, _scannerContainer, _lblStatus, _listaStack }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        PararScanner();
    }

    private async void OnAbrirScannerClicked(object? sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permissao", "Permita o acesso a camera.", "OK");
                return;
            }

            _jaProcessou = false;
            _btnScan.IsVisible = false;

            _scanner = new CameraBarcodeReaderView
            {
                HeightRequest = 250,
                IsDetecting = true,
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.All,
                    AutoRotate = true,
                    Multiple = false
                }
            };
            _scanner.BarcodesDetected += OnBarcodesDetected;

            _scannerContainer.Children.Clear();
            _scannerContainer.Children.Add(_scanner);
            _scannerContainer.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", "Camera: " + ex.Message, "OK");
            _btnScan.IsVisible = true;
        }
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_jaProcessou) return;
        var result = e.Results?.FirstOrDefault();
        if (result == null) return;
        _jaProcessou = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            PararScanner();
            _txtBusca.Text = result.Value;
            await BuscarAsync();
        });
    }

    private void PararScanner()
    {
        if (_scanner != null)
        {
            _scanner.IsDetecting = false;
            _scanner.BarcodesDetected -= OnBarcodesDetected;
            _scanner = null;
        }
        _scannerContainer.Children.Clear();
        _scannerContainer.IsVisible = false;
        _btnScan.IsVisible = true;
    }

    private async Task BuscarAsync()
    {
        var termo = _txtBusca.Text?.Trim() ?? "";
        if (termo.Length < 2) { _lblStatus.Text = "Digite ao menos 2 caracteres"; _lblStatus.IsVisible = true; return; }

        _lblStatus.Text = "Buscando..."; _lblStatus.IsVisible = true;
        _listaStack.Children.Clear();

        try
        {
            var lista = await DataService.BuscarProdutosAsync(termo);
            _lblStatus.Text = lista.Count + " produto(s) encontrado(s)";

            foreach (var p in lista)
                _listaStack.Children.Add(CriarCardProduto(p));

            // Se escaneou EAN e achou 1 só, seleciona direto
            if (lista.Count == 1 && _onSelecionado != null)
            {
                _onSelecionado(lista[0]);
            }
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
    }

    private View CriarCardProduto(ProdutoDto p)
    {
        var cVerde = Color.FromArgb("#2EA043");
        var cVerm = Color.FromArgb("#C0392B");
        var ptBr = new System.Globalization.CultureInfo("pt-BR");

        var img = new Image { WidthRequest = 60, HeightRequest = 60, Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#F0F0F0") };
        if (p.FotoId > 0)
            _ = CarregarFotoAsync(img, p.FotoId);

        var estoqueColor = p.Estoque > 0 ? cVerde : cVerm;
        var card = new Grid
        {
            BackgroundColor = Colors.White, Padding = new Thickness(12),
            ColumnDefinitions = { new ColumnDefinition(new GridLength(65)), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
            Children =
            {
                img,
                new StackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = p.Descricao, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#274176"), LineBreakMode = LineBreakMode.TailTruncation },
                        new Label { Text = (string.IsNullOrEmpty(p.Sku) ? "" : "SKU: " + p.Sku + "  ") + (string.IsNullOrEmpty(p.Ean) ? "" : "EAN: " + p.Ean), FontSize = 11, TextColor = Color.FromArgb("#888") },
                        new Grid
                        {
                            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                            Children =
                            {
                                new Label { Text = p.PrecoVenda.ToString("C2", ptBr), FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = cVerde },
                            }
                        }
                    }
                }
            }
        };
        var lblEstoque = new Label { Text = "Estoque: " + p.Estoque.ToString("N0"), FontSize = 12, TextColor = estoqueColor, HorizontalTextAlignment = TextAlignment.End };
        var innerGrid = (card.Children[1] as StackLayout)!.Children[2] as Grid;
        innerGrid!.Children.Add(lblEstoque); Grid.SetColumn(lblEstoque, 1);
        Grid.SetColumn(card.Children[1] as View, 1);

        if (_onSelecionado != null)
        {
            var produto = p;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => _onSelecionado(produto);
            card.GestureRecognizers.Add(tap);
        }
        return card;
    }

    private static async Task CarregarFotoAsync(Image img, long fotoId)
    {
        try
        {
            var bytes = await DataService.ObterFotoProdutoAsync(fotoId);
            if (bytes != null)
                img.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch { }
    }
}
