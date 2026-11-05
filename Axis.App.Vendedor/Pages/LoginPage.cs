using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class LoginPage : ContentPage
{
    private Entry _txtEmail = null!;
    private Entry _txtSenha = null!;
    private Label _lblErro = null!;
    private Label _lblConexao = null!;
    private Label _lblAviso = null!;
    private Button _btnEntrar = null!;
    private ActivityIndicator _loading = null!;
    private Switch _swSalvarLogin = null!;

    public LoginPage()
    {
        try
        {
            BuildUI();
        }
        catch (Exception ex)
        {
            Content = new Label { Text = "Erro LoginPage: " + ex.Message, TextColor = Colors.Red, Margin = 30 };
        }
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#D6E8FF");
        Shell.SetNavBarIsVisible(this, false);

        var cAzul = Color.FromArgb("#2A66B8");
        var cTexto = Color.FromArgb("#274176");
        var cLight = Color.FromArgb("#BFD7FF");

        _txtEmail = new Entry
        {
            Placeholder = "seu@email.com",
            Keyboard = Keyboard.Email,
            FontSize = 15,
            TextColor = Color.FromArgb("#212121"),
            BackgroundColor = Color.FromArgb("#F5F8FC")
        };

        _txtSenha = new Entry
        {
            Placeholder = "********",
            IsPassword = true,
            FontSize = 15,
            TextColor = Color.FromArgb("#212121"),
            BackgroundColor = Color.FromArgb("#F5F8FC")
        };

        _lblErro = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb("#C0392B"),
            HorizontalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };

        _lblConexao = new Label
        {
            FontSize = 11,
            TextColor = Color.FromArgb("#919191"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        _lblAviso = new Label
        {
            Text = "Conexao nao configurada! Toque na engrenagem acima.",
            FontSize = 13,
            TextColor = Color.FromArgb("#E65100"),
            BackgroundColor = Color.FromArgb("#FFF3E0"),
            Padding = new Thickness(16, 12),
            HorizontalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };

        _btnEntrar = new Button
        {
            Text = "Entrar",
            BackgroundColor = cAzul,
            TextColor = Colors.White,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 50
        };
        _btnEntrar.Clicked += OnEntrarClicked;

        _swSalvarLogin = new Switch { IsToggled = false, OnColor = cAzul, HorizontalOptions = LayoutOptions.Start };

        _loading = new ActivityIndicator
        {
            Color = cAzul,
            IsRunning = false,
            HorizontalOptions = LayoutOptions.Center
        };

        var salvarRow = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                _swSalvarLogin,
                new Label { Text = "Salvar login", FontSize = 13, TextColor = cTexto, VerticalTextAlignment = TextAlignment.Center }
            }
        };

        var btnConfig = new Button
        {
            Text = "⚙",
            FontSize = 22,
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };
        btnConfig.Clicked += OnConfigTapped;

        // Header
        var header = new Grid
        {
            BackgroundColor = cAzul,
            Padding = new Thickness(20, 52, 20, 25),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new StackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "Axis Vendedor", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Pre-Venda Mobile", FontSize = 14, TextColor = cLight, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,4,0,0) }
                    }
                }
            }
        };
        Grid.SetColumnSpan(header.Children[0] as View, 2);
        header.Children.Add(btnConfig);
        Grid.SetColumn(btnConfig, 1);

        // Card
        var card = new StackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new Label { Text = "Acesso do Vendedor", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = cTexto, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "E-mail", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                _txtEmail,
                new Label { Text = "Senha", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                _txtSenha,
                salvarRow,
                _lblErro,
                _btnEntrar,
                _loading
            }
        };

        // Layout
        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(24, 20),
                    Content = new StackLayout
                    {
                        Spacing = 16,
                        Children = { _lblAviso, card, _lblConexao }
                    }
                }
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lblErro.IsVisible = false;

        if (ConfigService.Configurado)
        {
            _lblAviso.IsVisible = false;
            _btnEntrar.IsEnabled = true;
            var info = ConfigService.Info;
            _lblConexao.Text = info.host + "/" + info.database;
        }
        else
        {
            _lblAviso.IsVisible = true;
            _btnEntrar.IsEnabled = false;
            _lblConexao.Text = "Sem conexao configurada";
        }

        // Carregar login salvo
        try
        {
            var savedEmail = await SecureStorage.GetAsync("axis_email");
            var savedSenha = await SecureStorage.GetAsync("axis_senha");
            if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedSenha))
            {
                _txtEmail.Text = savedEmail;
                _txtSenha.Text = savedSenha;
                _swSalvarLogin.IsToggled = true;
            }
        }
        catch { /* SecureStorage pode falhar em alguns devices */ }
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        if (!ConfigService.Configurado)
        {
            await DisplayAlert("Aviso", "Configure a conexao primeiro.", "OK");
            return;
        }

        _lblErro.IsVisible = false;
        _btnEntrar.IsEnabled = false;
        _loading.IsRunning = true;

        try
        {
            _lblErro.Text = "Conectando...";
            _lblErro.TextColor = Color.FromArgb("#2A66B8");
            _lblErro.IsVisible = true;

            var (sucesso, mensagem) = await AuthService.LoginAsync(
                _txtEmail.Text ?? "", _txtSenha.Text ?? "");

            _lblErro.TextColor = Color.FromArgb("#C0392B");

            if (sucesso)
            {
                // Salvar ou limpar credenciais
                if (_swSalvarLogin.IsToggled)
                {
                    await SecureStorage.SetAsync("axis_email", _txtEmail.Text ?? "");
                    await SecureStorage.SetAsync("axis_senha", _txtSenha.Text ?? "");
                }
                else
                {
                    SecureStorage.Remove("axis_email");
                    SecureStorage.Remove("axis_senha");
                }

                _lblErro.Text = "Carregando empresa...";
                await ConfigService.CarregarEmpresaIdAsync();
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                _lblErro.Text = mensagem;
                _lblErro.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            _lblErro.Text = "Erro: " + ex.Message;
            _lblErro.IsVisible = true;
        }
        finally
        {
            _btnEntrar.IsEnabled = true;
            _loading.IsRunning = false;
        }
    }

    private async void OnConfigTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ConfigPage");
    }
}
