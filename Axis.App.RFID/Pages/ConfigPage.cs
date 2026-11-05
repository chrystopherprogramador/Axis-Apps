using Axis.App.RFID.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Axis.App.RFID.Pages;

public class ConfigPage : ContentPage
{
    private CameraBarcodeReaderView? _scanner;
    private StackLayout _scannerContainer = null!;
    private Entry _txtHost = null!;
    private Entry _txtPorta = null!;
    private Entry _txtDatabase = null!;
    private Entry _txtUsuario = null!;
    private Entry _txtSenhaDb = null!;
    private Label _lblStatus = null!;
    private StackLayout _manualStack = null!;
    private Button _btnScan = null!;
    private bool _jaProcessou;

    public ConfigPage() { BuildUI(); }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");
        var cGreen = Color.FromArgb("#2E7D32");

        _lblStatus = new Label { FontSize = 13, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false, Margin = new Thickness(0, 6) };
        _scannerContainer = new StackLayout { IsVisible = false, HeightRequest = 260 };

        var btnVoltar = new Button { Text = "←", FontSize = 22, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HeightRequest = 44, Padding = 0, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = cP, Padding = new Thickness(12, 48, 20, 14),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                btnVoltar,
                new Label { Text = "Configuracao", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        // Botao scan QR
        _btnScan = new Button { Text = "📷 Escanear QR Code", BackgroundColor = cP, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50 };
        _btnScan.Clicked += OnAbrirScannerClicked;

        // Campos manuais
        _txtHost = new Entry { Placeholder = "Servidor (ex: 192.168.1.100)", FontSize = 14, Keyboard = Keyboard.Url, BackgroundColor = Colors.White };
        _txtPorta = new Entry { Placeholder = "Porta (ex: 5432)", FontSize = 14, Keyboard = Keyboard.Numeric, Text = "5432", BackgroundColor = Colors.White };
        _txtDatabase = new Entry { Placeholder = "Nome do banco", FontSize = 14, BackgroundColor = Colors.White };
        _txtUsuario = new Entry { Placeholder = "Usuario do banco", FontSize = 14, BackgroundColor = Colors.White };
        _txtSenhaDb = new Entry { Placeholder = "Senha do banco", FontSize = 14, IsPassword = true, BackgroundColor = Colors.White };

        var btnSalvarManual = new Button { Text = "Salvar Configuracao", BackgroundColor = cGreen, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 46 };
        btnSalvarManual.Clicked += OnSalvarManualClicked;

        _manualStack = new StackLayout
        {
            Spacing = 10,
            Children =
            {
                new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0E6ED"), Margin = new Thickness(0, 6) },
                new Label { Text = "Configuracao Manual", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cP },
                new Label { Text = "Servidor", FontSize = 12, TextColor = Color.FromArgb("#78909C") }, _txtHost,
                new Label { Text = "Porta", FontSize = 12, TextColor = Color.FromArgb("#78909C") }, _txtPorta,
                new Label { Text = "Banco de Dados", FontSize = 12, TextColor = Color.FromArgb("#78909C") }, _txtDatabase,
                new Label { Text = "Usuario", FontSize = 12, TextColor = Color.FromArgb("#78909C") }, _txtUsuario,
                new Label { Text = "Senha", FontSize = 12, TextColor = Color.FromArgb("#78909C") }, _txtSenhaDb,
                btnSalvarManual
            }
        };

        var btnLimpar = new Button { Text = "Limpar Configuracao", BackgroundColor = Color.FromArgb("#C62828"), TextColor = Colors.White, FontSize = 13, CornerRadius = 8, HeightRequest = 38, Margin = new Thickness(0, 10) };
        btnLimpar.Clicked += (s, e) =>
        {
            ConfigService.Limpar();
            LimparCampos();
            MostrarStatus("Configuracao removida.", Color.FromArgb("#1565C0"));
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(18, 14, 18, 24),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { _btnScan, _scannerContainer, _lblStatus, _manualStack, btnLimpar }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);

        // Preencher campos se ja configurado
        if (ConfigService.Configurado)
        {
            var info = ConfigService.Info;
            _txtHost.Text = info.host;
            _txtPorta.Text = info.porta;
            _txtDatabase.Text = info.database;
            _txtUsuario.Text = info.usuario;
            MostrarStatus($"Conectado: {info.host}/{info.database}", Color.FromArgb("#2E7D32"));
        }
    }

    private void OnAbrirScannerClicked(object? sender, EventArgs e)
    {
        if (_scanner != null)
        {
            _scannerContainer.Children.Clear();
            _scanner = null;
            _scannerContainer.IsVisible = false;
            _btnScan.Text = "📷 Escanear QR Code";
            return;
        }

        _jaProcessou = false;
        _scanner = new CameraBarcodeReaderView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Options = new BarcodeReaderOptions { Formats = BarcodeFormat.QrCode, AutoRotate = true, TryHarder = true }
        };
        _scanner.BarcodesDetected += OnQrDetected;
        _scannerContainer.Children.Add(_scanner);
        _scannerContainer.IsVisible = true;
        _btnScan.Text = "✕ Fechar Camera";
    }

    private void OnQrDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_jaProcessou) return;
        var raw = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrEmpty(raw)) return;
        _jaProcessou = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var (ok, msg) = ConfigService.SalvarDoQrCode(raw);
            MostrarStatus(msg, ok ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828"));

            if (ok)
            {
                var info = ConfigService.Info;
                _txtHost.Text = info.host;
                _txtPorta.Text = info.porta;
                _txtDatabase.Text = info.database;
                _txtUsuario.Text = info.usuario;

                _scannerContainer.Children.Clear();
                _scanner = null;
                _scannerContainer.IsVisible = false;
                _btnScan.Text = "📷 Escanear QR Code";

                await Task.Delay(800);
                await Navigation.PopAsync();
            }
        });
    }

    private void OnSalvarManualClicked(object? sender, EventArgs e)
    {
        var host = _txtHost.Text?.Trim();
        var porta = _txtPorta.Text?.Trim();
        var db = _txtDatabase.Text?.Trim();
        var user = _txtUsuario.Text?.Trim();
        var pwd = _txtSenhaDb.Text?.Trim();

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(db) || string.IsNullOrEmpty(user))
        {
            MostrarStatus("Preencha pelo menos Servidor, Banco e Usuario.", Color.FromArgb("#C62828"));
            return;
        }

        var json = $"{{\"host\":\"{host}\",\"porta\":\"{porta ?? "5432"}\",\"database\":\"{db}\",\"usuario\":\"{user}\",\"senha\":\"{pwd ?? ""}\"}}";
        var (ok, msg) = ConfigService.SalvarDoQrCode(json);
        MostrarStatus(ok ? $"Salvo: {host}/{db}" : msg, ok ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828"));
    }

    private void MostrarStatus(string texto, Color cor)
    {
        _lblStatus.Text = texto;
        _lblStatus.TextColor = cor;
        _lblStatus.IsVisible = true;
    }

    private void LimparCampos()
    {
        _txtHost.Text = ""; _txtPorta.Text = "5432"; _txtDatabase.Text = ""; _txtUsuario.Text = ""; _txtSenhaDb.Text = "";
    }
}
