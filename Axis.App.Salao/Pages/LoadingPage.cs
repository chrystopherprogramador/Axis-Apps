namespace Axis.App.Salao.Pages;

public class LoadingPage : ContentPage
{
    private readonly Label _lblStatus;
    private readonly ActivityIndicator _spinner;
    private readonly StackLayout _centerContent;

    public LoadingPage()
    {
        BackgroundColor = Color.FromArgb("#4A148C");

        _lblStatus = new Label
        {
            Text = "Inicializando...",
            FontSize = 13,
            TextColor = Color.FromArgb("#CBB4E8"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        _spinner = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#B388E0"),
            HeightRequest = 32,
            WidthRequest = 32,
            HorizontalOptions = LayoutOptions.Center
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
            Text = "Salão",
            FontSize = 16,
            TextColor = Color.FromArgb("#CBB4E8"),
            HorizontalTextAlignment = TextAlignment.Center,
            CharacterSpacing = 4,
            Margin = new Thickness(0, -4, 0, 0)
        };

        var separator = new BoxView
        {
            HeightRequest = 2,
            Color = Color.FromArgb("#7E3FCB"),
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
            Children =
            {
                lblTitle,
                lblSubtitle,
                separator,
                new BoxView { HeightRequest = 20, Color = Colors.Transparent },
                _spinner,
                _lblStatus
            }
        };

        Content = new Grid { Children = { new ScrollView { Content = _centerContent } } };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await Task.Delay(600);
            _lblStatus.Text = "Pronto!";
            _spinner.IsRunning = false;
            await Task.Delay(300);
            if (Application.Current != null)
                Application.Current.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "ERRO: " + ex.Message;
        }
    }
}
