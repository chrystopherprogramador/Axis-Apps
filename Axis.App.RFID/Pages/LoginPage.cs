using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

public class LoginPage : ContentPage
{
    private Entry _txtEmail = null!;
    private Entry _txtSenha = null!;
    private Label _lblErro = null!;
    private Button _btnEntrar = null!;
    private Switch _swSalvar = null!;
    private ActivityIndicator _loading = null!;

    public LoginPage() { BuildUI(); }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");

        _txtEmail = new Entry { Placeholder = "seu@email.com", Keyboard = Keyboard.Email, FontSize = 15, BackgroundColor = Color.FromArgb("#F5F8FC") };
        _txtSenha = new Entry { Placeholder = "********", IsPassword = true, FontSize = 15, BackgroundColor = Color.FromArgb("#F5F8FC") };
        _lblErro = new Label { FontSize = 13, TextColor = Color.FromArgb("#C62828"), HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
        _btnEntrar = new Button { Text = "Entrar", BackgroundColor = cP, TextColor = Colors.White, FontSize = 17, FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 50 };
        _btnEntrar.Clicked += OnEntrarClicked;
        _swSalvar = new Switch { IsToggled = false, OnColor = cP };
        _loading = new ActivityIndicator { Color = cP, IsRunning = false, HorizontalOptions = LayoutOptions.Center };

        var btnConfig = new Button { Text = "⚙", FontSize = 22, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HeightRequest = 44, Padding = 0, HorizontalOptions = LayoutOptions.End };
        btnConfig.Clicked += async (s, e) => await Shell.Current.GoToAsync("ConfigPage");

        var header = new StackLayout
        {
            BackgroundColor = cP, Padding = new Thickness(20, 52, 20, 25),
            Children =
            {
                btnConfig,
                new Label { Text = "📡", FontSize = 40, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Axis RFID", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Inventario por RFID", FontSize = 14, TextColor = Color.FromArgb("#90A4AE"), HorizontalTextAlignment = TextAlignment.Center }
            }
        };

        var salvarRow = new StackLayout { Orientation = StackOrientation.Horizontal, Spacing = 8, Children = { _swSalvar, new Label { Text = "Salvar login", FontSize = 13, TextColor = cP, VerticalTextAlignment = TextAlignment.Center } } };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Children =
                {
                    header,
                    new StackLayout
                    {
                        Padding = new Thickness(24, 20), Spacing = 14,
                        Children =
                        {
                            new Label { Text = "E-mail", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cP },
                            _txtEmail,
                            new Label { Text = "Senha", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cP },
                            _txtSenha,
                            salvarRow, _lblErro, _btnEntrar, _loading
                        }
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lblErro.IsVisible = false;
        if (!ConfigService.Configurado) { _btnEntrar.IsEnabled = false; _lblErro.Text = "Configure a conexao (engrenagem acima)."; _lblErro.IsVisible = true; return; }
        _btnEntrar.IsEnabled = true;
        try
        {
            var e = await SecureStorage.GetAsync("rfid_email");
            var s = await SecureStorage.GetAsync("rfid_senha");
            if (!string.IsNullOrEmpty(e)) { _txtEmail.Text = e; _txtSenha.Text = s; _swSalvar.IsToggled = true; }
        }
        catch { }
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        _lblErro.IsVisible = false; _btnEntrar.IsEnabled = false; _loading.IsRunning = true;
        try
        {
            var (ok, msg) = await AuthService.LoginAsync(_txtEmail.Text ?? "", _txtSenha.Text ?? "");
            if (ok)
            {
                if (_swSalvar.IsToggled) { await SecureStorage.SetAsync("rfid_email", _txtEmail.Text ?? ""); await SecureStorage.SetAsync("rfid_senha", _txtSenha.Text ?? ""); }
                else { SecureStorage.Remove("rfid_email"); SecureStorage.Remove("rfid_senha"); }

                var empresas = await AuthService.ListarEmpresasAsync();
                if (empresas.Count == 0) { _lblErro.Text = "Nenhuma empresa vinculada."; _lblErro.IsVisible = true; return; }
                if (empresas.Count == 1) { ConfigService.EmpresaId = empresas[0].id; await Shell.Current.GoToAsync("//HomePage"); }
                else { await Navigation.PushAsync(new EmpresaSelectPage(empresas)); }
            }
            else { _lblErro.Text = msg; _lblErro.IsVisible = true; }
        }
        catch (Exception ex) { _lblErro.Text = "Erro: " + ex.Message; _lblErro.IsVisible = true; }
        finally { _btnEntrar.IsEnabled = true; _loading.IsRunning = false; }
    }
}
