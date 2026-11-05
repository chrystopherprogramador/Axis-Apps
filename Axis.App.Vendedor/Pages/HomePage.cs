using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class HomePage : ContentPage
{
    private Label _lblVendedor = null!;
    private Label _lblQtd = null!;
    private Label _lblTotal = null!;
    private Label _lblConexao = null!;

    public HomePage()
    {
        try
        {
            BuildUI();
        }
        catch (Exception ex)
        {
            Content = new Label { Text = "Erro HomePage: " + ex.Message, TextColor = Colors.Red, Margin = 30 };
        }
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#D6E8FF");
        Shell.SetNavBarIsVisible(this, false);

        var cAzul = Color.FromArgb("#2A66B8");
        var cTexto = Color.FromArgb("#274176");

        _lblVendedor = new Label { FontSize = 13, TextColor = Color.FromArgb("#BFD7FF") };
        _lblQtd = new Label { Text = "0", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = cAzul };
        _lblTotal = new Label { Text = "R$ 0,00", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2EA043") };
        _lblConexao = new Label { FontSize = 11, TextColor = Color.FromArgb("#919191"), HorizontalTextAlignment = TextAlignment.Center };

        var btnSair = new Button
        {
            Text = "Sair",
            FontSize = 12,
            BackgroundColor = Color.FromArgb("#1E4A8C"),
            TextColor = Colors.White,
            CornerRadius = 6,
            HeightRequest = 34,
            Padding = new Thickness(14, 0),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };
        btnSair.Clicked += OnSairClicked;

        Func<string, string, EventHandler<TappedEventArgs>, View> criarBotaoAcao = (emoji, texto, handler) =>
        {
            var stack = new StackLayout
            {
                BackgroundColor = Colors.White,
                Padding = new Thickness(16),
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new Label { Text = emoji, FontSize = 28, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = texto, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cTexto, HorizontalTextAlignment = TextAlignment.Center }
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += handler;
            stack.GestureRecognizers.Add(tap);
            return stack;
        };

        var header = new Grid
        {
            BackgroundColor = cAzul,
            Padding = new Thickness(20, 50, 20, 18),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new StackLayout { Spacing = 2, Children = { new Label { Text = "Axis Vendedor", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }, _lblVendedor } },
                btnSair
            }
        };
        Grid.SetColumn(btnSair, 1);

        var resumo = new StackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new Label { Text = "Resumo do Dia", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E8F0FC") },
                new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    Children =
                    {
                        new StackLayout { Spacing = 2, Children = { new Label { Text = "Pre-Vendas", FontSize = 12, TextColor = Color.FromArgb("#6E6E6E") }, _lblQtd } },
                    }
                }
            }
        };
        var gridResumo = resumo.Children[2] as Grid;
        var stackTotal = new StackLayout { Spacing = 2, Children = { new Label { Text = "Total Vendido", FontSize = 12, TextColor = Color.FromArgb("#6E6E6E") }, _lblTotal } };
        gridResumo!.Children.Add(stackTotal);
        Grid.SetColumn(stackTotal, 1);

        var acoes = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            RowSpacing = 12
        };
        var b1 = criarBotaoAcao("🛒", "Nova\nPre-Venda", async (s, e) => await Navigation.PushAsync(new PreVendaPage()));
        var b2 = criarBotaoAcao("📋", "Minhas\nPre-Vendas", async (s, e) => await Navigation.PushAsync(new MinhasPreVendasPage()));
        var b3 = criarBotaoAcao("🔍", "Buscar\nProduto", async (s, e) => await Navigation.PushAsync(new BuscarProdutoPage()));
        var b4 = criarBotaoAcao("👥", "Buscar\nCliente", async (s, e) => await Navigation.PushAsync(new BuscarClientePage()));
        var b5 = criarBotaoAcao("🎯", "Minha\nMeta", async (s, e) => await Navigation.PushAsync(new MetasPage()));
        var b6 = criarBotaoAcao("📊", "Dashboard", async (s, e) => await Navigation.PushAsync(new DashboardPage()));
        acoes.Children.Add(b1); Grid.SetRow(b1 as BindableObject, 0); Grid.SetColumn(b1 as BindableObject, 0);
        acoes.Children.Add(b2); Grid.SetRow(b2 as BindableObject, 0); Grid.SetColumn(b2 as BindableObject, 1);
        acoes.Children.Add(b3); Grid.SetRow(b3 as BindableObject, 1); Grid.SetColumn(b3 as BindableObject, 0);
        acoes.Children.Add(b4); Grid.SetRow(b4 as BindableObject, 1); Grid.SetColumn(b4 as BindableObject, 1);
        acoes.Children.Add(b5); Grid.SetRow(b5 as BindableObject, 2); Grid.SetColumn(b5 as BindableObject, 0);
        acoes.Children.Add(b6); Grid.SetRow(b6 as BindableObject, 2); Grid.SetColumn(b6 as BindableObject, 1);

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(20, 20),
                    Content = new StackLayout
                    {
                        Spacing = 16,
                        Children =
                        {
                            resumo,
                            new Label { Text = "Acoes", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cTexto, Margin = new Thickness(4,4,0,0) },
                            acoes
                        }
                    }
                },
                new StackLayout { BackgroundColor = Colors.White, Padding = new Thickness(12, 8), Children = { _lblConexao } }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
        Grid.SetRow((Content as Grid)!.Children[2] as View, 2);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _lblVendedor.Text = "Vendedor: " + AuthService.VendedorNome;
        var info = ConfigService.Info;
        _lblConexao.Text = "Conectado: " + info.host + "/" + info.database;
        CarregarResumoAsync();
    }

    private async void CarregarResumoAsync()
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            var sqlQtd = "SELECT COUNT(*) FROM pre_vendas WHERE vendedor_id = @vid AND DATE(data_cadastro) = CURRENT_DATE AND status <> 'X'";
            await using var cmdQtd = new Npgsql.NpgsqlCommand(sqlQtd, conn);
            cmdQtd.Parameters.AddWithValue("@vid", AuthService.VendedorId);
            _lblQtd.Text = (Convert.ToInt64(await cmdQtd.ExecuteScalarAsync() ?? 0)).ToString();

            var sqlTotal = "SELECT COALESCE(SUM(valor_total), 0) FROM pre_vendas WHERE vendedor_id = @vid AND DATE(data_cadastro) = CURRENT_DATE AND status <> 'X'";
            await using var cmdTotal = new Npgsql.NpgsqlCommand(sqlTotal, conn);
            cmdTotal.Parameters.AddWithValue("@vid", AuthService.VendedorId);
            var total = Convert.ToDecimal(await cmdTotal.ExecuteScalarAsync() ?? 0m);
            _lblTotal.Text = total.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
        }
        catch
        {
            _lblQtd.Text = "-";
            _lblTotal.Text = "-";
        }
    }

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var confirma = await DisplayAlert("Sair", "Deseja sair do sistema?", "Sim", "Nao");
        if (confirma)
        {
            AuthService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
