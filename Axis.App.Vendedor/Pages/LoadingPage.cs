namespace Axis.App.Vendedor.Pages;

public class LoadingPage : ContentPage
{
    private readonly Label _lblStatus;
    private readonly Label _lblErro;
    private readonly ActivityIndicator _spinner;
    private readonly Image _logo;
    private readonly StackLayout _centerContent;
    private readonly Label _lblVersion;

    public LoadingPage()
    {
        BackgroundColor = Color.FromArgb("#1A4F8C");

        _lblStatus = new Label
        {
            Text = "Inicializando...",
            FontSize = 13,
            TextColor = Color.FromArgb("#8BB8E8"),
            HorizontalTextAlignment = TextAlignment.Center,
            Opacity = 0
        };

        _lblErro = new Label
        {
            Text = "",
            FontSize = 11,
            TextColor = Colors.Yellow,
            HorizontalTextAlignment = TextAlignment.Start,
            IsVisible = false,
            Margin = new Thickness(10, 0)
        };

        _spinner = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#5BA0E0"),
            HeightRequest = 32,
            WidthRequest = 32,
            HorizontalOptions = LayoutOptions.Center,
            Opacity = 0
        };

        _logo = new Image
        {
            Source = "axis_splash.png",
            HeightRequest = 100,
            WidthRequest = 100,
            HorizontalOptions = LayoutOptions.Center,
            Opacity = 0
        };

        _lblVersion = new Label
        {
            Text = "v1.0",
            FontSize = 11,
            TextColor = Color.FromArgb("#4A7FB8"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 30),
            Opacity = 0
        };

        var lblTitle = new Label
        {
            Text = "AXIS",
            FontSize = 44,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            CharacterSpacing = 6
        };

        var lblSubtitle = new Label
        {
            Text = "Vendedor",
            FontSize = 16,
            TextColor = Color.FromArgb("#8BB8E8"),
            HorizontalTextAlignment = TextAlignment.Center,
            CharacterSpacing = 4,
            Margin = new Thickness(0, -4, 0, 0)
        };

        var separator = new BoxView
        {
            HeightRequest = 2,
            Color = Color.FromArgb("#3A7CC6"),
            WidthRequest = 60,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 16, 0, 6),
            CornerRadius = 1
        };

        _centerContent = new StackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 6,
            Padding = new Thickness(30),
            Opacity = 0,
            Children =
            {
                _logo,
                lblTitle,
                lblSubtitle,
                separator,
                new BoxView { HeightRequest = 20, Color = Colors.Transparent },
                _spinner,
                _lblStatus,
                _lblErro
            }
        };

        // Fundo com gradiente simulado usando BoxViews
        var topGlow = new BoxView
        {
            Color = Color.FromArgb("#2A66B8"),
            Opacity = 0.4,
            VerticalOptions = LayoutOptions.Start,
            HeightRequest = 300
        };

        Content = new Grid
        {
            Children =
            {
                topGlow,
                new ScrollView
                {
                    Content = _centerContent
                },
                _lblVersion
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Log("LoadingPage.OnAppearing INICIO");

            // Animacao de entrada
            await Task.Delay(200);
            _centerContent.TranslationY = 30;
            _ = _logo.FadeTo(1, 500, Easing.CubicOut);
            _ = _centerContent.FadeTo(1, 600, Easing.CubicOut);
            _ = _centerContent.TranslateTo(0, 0, 600, Easing.CubicOut);
            await Task.Delay(400);
            _ = _spinner.FadeTo(1, 400, Easing.CubicOut);
            _ = _lblStatus.FadeTo(1, 400, Easing.CubicOut);
            _ = _lblVersion.FadeTo(1, 600, Easing.CubicOut);

            // Mostrar crash log anterior se existir
            var crashPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "axis_crash.txt");

            if (File.Exists(crashPath))
            {
                var crashLog = File.ReadAllText(crashPath);
                if (!string.IsNullOrWhiteSpace(crashLog))
                {
                    _lblErro.Text = "CRASH ANTERIOR:\n" + crashLog;
                    _lblErro.IsVisible = true;

                    _lblStatus.Text = "Crash detectado (mostrando log)...";
                    await Task.Delay(5000);

                    File.Delete(crashPath);
                }
            }

            _lblStatus.Text = "Carregando...";
            Log("LoadingPage delay");
            await Task.Delay(800);

            _lblStatus.Text = "Pronto!";
            _spinner.IsRunning = false;
            Log("LoadingPage navegando para Shell");
            await Task.Delay(400);

            // Fade-out antes de navegar
            await _centerContent.FadeTo(0, 300, Easing.CubicIn);

            if (Application.Current != null)
            {
                Application.Current.Windows[0].Page = new AppShell();
                Log("AppShell carregado OK");
            }
        }
        catch (Exception ex)
        {
            Log("LoadingPage ERRO: " + ex.Message);
            _centerContent.Opacity = 1;
            _spinner.IsRunning = false;
            _lblStatus.Text = "ERRO:";
            _lblStatus.TextColor = Color.FromArgb("#FF6B6B");
            _lblStatus.Opacity = 1;
            _lblErro.Text = ex.ToString();
            _lblErro.IsVisible = true;
        }
    }

    private static void Log(string msg)
    {
#if ANDROID
        MainApplication.GravarLog(msg);
#endif
        System.Diagnostics.Debug.WriteLine("[AXIS] " + msg);
    }
}
