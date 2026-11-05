using Axis.App.Vendedor.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Axis.App.Vendedor.Pages;

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
    private StackLayout _infoStack = null!;
    private StackLayout _manualStack = null!;
    private Button _btnScan = null!;
    private bool _jaProcessou = false;

    public ConfigPage()
    {
        try { BuildUI(); }
        catch (Exception ex) { Content = new Label { Text = "Erro: " + ex.Message, TextColor = Colors.Red, Margin = 30 }; }
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#D6E8FF");
        Title = "Configuracao";

        var cAzul = Color.FromArgb("#2A66B8");
        var cTexto = Color.FromArgb("#274176");
        var cVerde = Color.FromArgb("#2EA043");
        var cVerm = Color.FromArgb("#C0392B");

        _lblStatus = new Label { FontSize = 14, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
        _infoStack = new StackLayout { IsVisible = false, Spacing = 4 };
        _scannerContainer = new StackLayout { IsVisible = false, HeightRequest = 280 };

        _btnScan = new Button
        {
            Text = "Escanear QR Code",
            BackgroundColor = cAzul, TextColor = Colors.White,
            FontSize = 16, FontAttributes = FontAttributes.Bold,
            CornerRadius = 10, HeightRequest = 52
        };
        _btnScan.Clicked += OnAbrirScannerClicked;

        // Config manual
        _txtHost = new Entry { Placeholder = "Servidor (ex: 192.168.1.100)", FontSize = 14, Keyboard = Keyboard.Url };
        _txtPorta = new Entry { Placeholder = "Porta (ex: 5432)", FontSize = 14, Keyboard = Keyboard.Numeric, Text = "5432" };
        _txtDatabase = new Entry { Placeholder = "Nome do banco", FontSize = 14 };
        _txtUsuario = new Entry { Placeholder = "Usuario do banco", FontSize = 14 };
        _txtSenhaDb = new Entry { Placeholder = "Senha do banco", FontSize = 14, IsPassword = true };

        var btnSalvarManual = new Button
        {
            Text = "Salvar", BackgroundColor = cVerde, TextColor = Colors.White,
            FontSize = 14, CornerRadius = 8, HeightRequest = 44
        };
        btnSalvarManual.Clicked += OnSalvarManualClicked;

        _manualStack = new StackLayout
        {
            IsVisible = false, Spacing = 10,
            Children = {
                new Label { Text = "Configuracao Manual:", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                _txtHost, _txtPorta, _txtDatabase, _txtUsuario, _txtSenhaDb, btnSalvarManual
            }
        };

        var lblManual = new Label
        {
            Text = "Digitar manualmente", FontSize = 13,
            TextColor = Color.FromArgb("#919191"),
            HorizontalTextAlignment = TextAlignment.Center,
            TextDecorations = TextDecorations.Underline
        };
        var tapManual = new TapGestureRecognizer();
        tapManual.Tapped += (s, e) => _manualStack.IsVisible = !_manualStack.IsVisible;
        lblManual.GestureRecognizers.Add(tapManual);

        var btnVoltar = new Button
        {
            Text = "Voltar ao Login", BackgroundColor = Color.FromArgb("#666666"),
            TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44
        };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var btnLimpar = new Button
        {
            Text = "Limpar Configuracao", BackgroundColor = cVerm,
            TextColor = Colors.White, FontSize = 13, CornerRadius = 8, HeightRequest = 38
        };
        btnLimpar.Clicked += (s, e) =>
        {
            ConfigService.Limpar();
            _infoStack.IsVisible = false;
            _lblStatus.Text = "Configuracao limpa!";
            _lblStatus.TextColor = cAzul;
            _lblStatus.IsVisible = true;
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Spacing = 0,
                Children =
                {
                    // Header
                    new StackLayout
                    {
                        BackgroundColor = cAzul,
                        Padding = new Thickness(20, 52, 20, 20),
                        Children =
                        {
                            new Label { Text = "Configuracao", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                            new Label { Text = "Conexao com o Banco de Dados", FontSize = 13, TextColor = Color.FromArgb("#BFD7FF"), HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,4,0,0) }
                        }
                    },
                    // Body
                    new StackLayout
                    {
                        Padding = new Thickness(24, 20),
                        Spacing = 14,
                        Children =
                        {
                            new Label { Text = "Escaneie o QR Code gerado no Axis ERP (Parametros > Axis App).", FontSize = 13, TextColor = Color.FromArgb("#666666") },
                            _btnScan,
                            _scannerContainer,
                            _lblStatus,
                            _infoStack,
                            new BoxView { HeightRequest = 1, Color = Color.FromArgb("#BFD7FF"), Margin = new Thickness(0, 4) },
                            lblManual,
                            _manualStack,
                            new BoxView { HeightRequest = 1, Color = Color.FromArgb("#BFD7FF"), Margin = new Thickness(0, 4) },
                            btnVoltar,
                            btnLimpar
                        }
                    }
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _jaProcessou = false;
        if (ConfigService.Configurado) MostrarInfo();
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
                await DisplayAlert("Permissao", "Permita o acesso a camera para escanear o QR Code.", "OK");
                return;
            }

            _jaProcessou = false;
            _btnScan.IsVisible = false;

            _scanner = new CameraBarcodeReaderView
            {
                HeightRequest = 280,
                IsDetecting = true,
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.TwoDimensional,
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
            await DisplayAlert("Erro", "Nao foi possivel abrir a camera: " + ex.Message, "OK");
            _btnScan.IsVisible = true;
        }
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_jaProcessou) return;
        var result = e.Results?.FirstOrDefault();
        if (result == null) return;

        _jaProcessou = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                PararScanner();

                var (ok, msg) = ConfigService.SalvarDoQrCode(result.Value);
                _lblStatus.IsVisible = true;

                if (ok)
                {
                    _lblStatus.Text = "Configurado com sucesso!";
                    _lblStatus.TextColor = Color.FromArgb("#2EA043");
                    MostrarInfo();
                }
                else
                {
                    _lblStatus.Text = "QR invalido: " + msg;
                    _lblStatus.TextColor = Color.FromArgb("#C0392B");
                    _jaProcessou = false;
                    _btnScan.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Erro: " + ex.Message;
                _lblStatus.IsVisible = true;
                _jaProcessou = false;
                _btnScan.IsVisible = true;
            }
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
    }

    private void OnSalvarManualClicked(object? sender, EventArgs e)
    {
        var host = _txtHost.Text?.Trim() ?? "";
        var porta = _txtPorta.Text?.Trim() ?? "5432";
        var db = _txtDatabase.Text?.Trim() ?? "";
        var user = _txtUsuario.Text?.Trim() ?? "";
        var pwd = _txtSenhaDb.Text ?? "";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(db))
        {
            _lblStatus.Text = "Servidor e banco sao obrigatorios!";
            _lblStatus.TextColor = Color.FromArgb("#C0392B");
            _lblStatus.IsVisible = true;
            return;
        }

        var json = "{\"host\":\"" + host + "\",\"porta\":\"" + porta + "\",\"database\":\"" + db + "\",\"usuario\":\"" + user + "\",\"senha\":\"" + pwd + "\"}";
        var (ok, msg) = ConfigService.SalvarDoQrCode(json);
        _lblStatus.IsVisible = true;
        if (ok)
        {
            _lblStatus.Text = "Configurado!";
            _lblStatus.TextColor = Color.FromArgb("#2EA043");
            _manualStack.IsVisible = false;
            MostrarInfo();
        }
        else
        {
            _lblStatus.Text = "Erro: " + msg;
            _lblStatus.TextColor = Color.FromArgb("#C0392B");
        }
    }

    private void MostrarInfo()
    {
        var info = ConfigService.Info;
        _infoStack.Children.Clear();
        _infoStack.Children.Add(new Label
        {
            Text = "Conexao: " + info.host + ":" + info.porta + " / " + info.database,
            FontSize = 13, TextColor = Color.FromArgb("#2EA043"), FontAttributes = FontAttributes.Bold
        });
        _infoStack.IsVisible = true;
        _btnScan.IsVisible = false;
    }
}
