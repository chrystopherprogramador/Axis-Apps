using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

public class HomePage : ContentPage
{
    private Label _lblLeitor = null!;

    public HomePage() { BuildUI(); }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");
        var cCard = Colors.White;

        _lblLeitor = new Label { FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };

        var btnSair = new Button { Text = "Sair", FontSize = 12, BackgroundColor = Color.FromArgb("#37474F"), TextColor = Colors.White, CornerRadius = 6, HeightRequest = 34, Padding = new Thickness(14, 0), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start };
        btnSair.Clicked += async (s, e) =>
        {
            if (await DisplayAlert("Sair", "Deseja sair?", "Sim", "Nao"))
            {
                AuthService.Logout();
                await Shell.Current.GoToAsync("//LoginPage");
            }
        };

        var header = new Grid
        {
            BackgroundColor = cP, Padding = new Thickness(20, 48, 20, 18),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new StackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = "📡 Axis RFID", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = AuthService.UsuarioNome, FontSize = 13, TextColor = Color.FromArgb("#90A4AE") }
                    }
                },
                btnSair
            }
        };
        Grid.SetColumn(btnSair, 1);

        Func<string, string, string, EventHandler<TappedEventArgs>, View> card = (emoji, titulo, desc, handler) =>
        {
            var border = new Border
            {
                BackgroundColor = cCard,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Stroke = Color.FromArgb("#E0E6ED"), StrokeThickness = 1,
                Padding = new Thickness(18, 16),
                Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#18000000")), Offset = new Point(0, 2), Radius = 6 },
                Content = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                    ColumnSpacing = 14,
                    Children =
                    {
                        new Label { Text = emoji, FontSize = 32, VerticalTextAlignment = TextAlignment.Center },
                        new StackLayout
                        {
                            Spacing = 2, VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label { Text = titulo, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = cP },
                                new Label { Text = desc, FontSize = 12, TextColor = Color.FromArgb("#78909C") }
                            }
                        }
                    }
                }
            };
            Grid.SetColumn((border.Content as Grid)!.Children[1] as View, 1);
            var tap = new TapGestureRecognizer();
            tap.Tapped += handler;
            border.GestureRecognizers.Add(tap);
            return border;
        };

        var c1 = card("📦", "Inventario Completo", "Leitura total de tags RFID",
            async (s, e) => await Navigation.PushAsync(new InventarioPage()));
        var c2 = card("🔍", "Localizador", "Encontrar produto por referencia",
            async (s, e) => await Navigation.PushAsync(new LocalizadorPage()));
        var c3 = card("📋", "Inventario Parcial", "Por classificacao de produto",
            async (s, e) => await Navigation.PushAsync(new InventarioParcialPage()));
        var c4 = card("🔧", "Diagnostico", "Testar APIs RFID do dispositivo",
            async (s, e) => await Navigation.PushAsync(new DiagnosticoPage()));

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(16, 16),
                    Content = new StackLayout
                    {
                        Spacing = 12,
                        Children = { c1, c2, c3, c4, _lblLeitor }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var (marca, tipo) = RfidReaderFactory.DetectarLeitor();
        _lblLeitor.Text = $"Dispositivo: {Android.OS.Build.Manufacturer} {Android.OS.Build.Model} | Leitor: {marca} ({tipo})";
    }
}
