using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

public class FinanceiroPage : ContentPage
{
    private DatePicker _dtIni = null!;
    private DatePicker _dtFim = null!;
    private readonly StackLayout _resultado;
    private readonly ActivityIndicator _loading;

    private static readonly Color CRoxo = Color.FromArgb("#6f42c1");
    private static readonly Color CTexto = Color.FromArgb("#4A148C");

    public FinanceiroPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");

        _loading = new ActivityIndicator { Color = CRoxo, IsRunning = false, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20) };
        _resultado = new StackLayout { Spacing = 12 };

        _dtIni = new DatePicker { Date = DateTime.Today.AddDays(-DateTime.Today.Day + 1), FontSize = 14, BackgroundColor = Colors.White };
        _dtFim = new DatePicker { Date = DateTime.Today, FontSize = 14, BackgroundColor = Colors.White };

        var btnVoltar = new Button { Text = "‹", FontSize = 26, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Shell.Current.GoToAsync("//HomePage");

        var header = new Grid
        {
            BackgroundColor = CRoxo,
            Padding = new Thickness(12, 48, 12, 16),
            Children =
            {
                btnVoltar,
                new Label { Text = "Financeiro", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            }
        };

        var btnConsultar = new Button { Text = "Consultar", BackgroundColor = CRoxo, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 48 };
        btnConsultar.Clicked += async (s, e) => await ConsultarAsync();

        var periodo = new StackLayout
        {
            Padding = new Thickness(20, 20),
            Spacing = 10,
            Children =
            {
                new Label { Text = "Período", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = CTexto },
                new Grid
                {
                    ColumnSpacing = 10,
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    Children =
                    {
                        new StackLayout { Children = { new Label { Text = "De", FontSize = 12, TextColor = CTexto }, _dtIni } },
                        new StackLayout { Children = { new Label { Text = "Até", FontSize = 12, TextColor = CTexto }, _dtFim } }.Column(1)
                    }
                },
                btnConsultar
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView { Content = new StackLayout { Children = { periodo, _loading, _resultado } } }
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ConsultarAsync();
    }

    private async Task ConsultarAsync()
    {
        _loading.IsRunning = true;
        _resultado.Children.Clear();
        try
        {
            var r = await SalaoDataService.ObterResumoFinanceiroAsync(_dtIni.Date, _dtFim.Date, AuthService.ProfissionalId);

            _resultado.Children.Add(CardValor("Total Líquido", r.TotalLiquido.ToString("C"), Color.FromArgb("#2EA043")));
            _resultado.Children.Add(CardValor("Comissão (" + r.ComissaoPercentual.ToString("0.##") + "%)", r.ComissaoValor.ToString("C"), CRoxo));

            _resultado.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Padding = new Thickness(16),
                HasShadow = false,
                Margin = new Thickness(20, 0),
                Content = new StackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        LinhaDet("Atendimentos concluídos", r.QtdAtendidos.ToString()),
                        LinhaDet("Ainda agendados", r.QtdAgendados.ToString()),
                        LinhaDet("Faturamento bruto", r.TotalBruto.ToString("C")),
                        LinhaDet("Descontos", r.TotalDescontos.ToString("C"))
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _resultado.Children.Add(new Label { Text = "Erro: " + ex.Message, TextColor = Colors.Red, Margin = new Thickness(20) });
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }

    private static Frame CardValor(string titulo, string valor, Color cor) => new()
    {
        BackgroundColor = cor,
        CornerRadius = 12,
        Padding = new Thickness(20),
        HasShadow = true,
        Margin = new Thickness(20, 0),
        Content = new StackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = titulo, FontSize = 13, TextColor = Color.FromArgb("#F0EAFB") },
                new Label { Text = valor, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
            }
        }
    };

    private static View LinhaDet(string titulo, string valor) => new Grid
    {
        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        Children =
        {
            new Label { Text = titulo, FontSize = 13, TextColor = Color.FromArgb("#777777") },
            new Label { Text = valor, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4A148C") }.Column(1)
        }
    };
}
