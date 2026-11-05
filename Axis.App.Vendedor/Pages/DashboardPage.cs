using Axis.App.Vendedor.Services;
using Npgsql;

namespace Axis.App.Vendedor.Pages;

/// <summary>
/// Dashboard do vendedor com indicadores:
/// PA (Pecas por Atendimento), PM (Preco Medio), TKM (Ticket Medio),
/// Total vendido no mes, melhores dias e melhores produtos.
/// </summary>
public class DashboardPage : ContentPage
{
    private Label _lblPA = null!;
    private Label _lblPM = null!;
    private Label _lblTKM = null!;
    private Label _lblTotalMes = null!;
    private Label _lblQtdVendas = null!;
    private Label _lblQtdItens = null!;
    private Label _lblMesRef = null!;
    private StackLayout _stackDias = null!;
    private StackLayout _stackProdutos = null!;
    private ActivityIndicator _loading = null!;

    private int _mesAtual = DateTime.Now.Month;
    private int _anoAtual = DateTime.Now.Year;

    private static readonly string[] NomesMes = ["", "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
    private static readonly string[] NomesDia = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];

    public DashboardPage()
    {
        try { BuildUI(); }
        catch (Exception ex) { Content = new Label { Text = "Erro: " + ex.Message, TextColor = Colors.Red, Margin = 30 }; }
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);

        var cPrimary = Color.FromArgb("#1565C0");
        var cTexto = Color.FromArgb("#263238");
        var cSub = Color.FromArgb("#546E7A");

        // Header
        var btnVoltar = new Button
        {
            Text = "←", FontSize = 22, BackgroundColor = Colors.Transparent,
            TextColor = Colors.White, WidthRequest = 44, HeightRequest = 44,
            Padding = 0, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center
        };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = cPrimary,
            Padding = new Thickness(12, 48, 20, 14),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                btnVoltar,
                new StackLayout
                {
                    Spacing = 1, VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "Dashboard", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = AuthService.VendedorNome, FontSize = 12, TextColor = Color.FromArgb("#B3D4FC") }
                    }
                }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        // Navegacao mes
        var btnAnt = new Button { Text = "◀", FontSize = 18, BackgroundColor = Colors.Transparent, TextColor = cPrimary, WidthRequest = 44, HeightRequest = 36, Padding = 0 };
        btnAnt.Clicked += (s, e) => NavegarMes(-1);
        var btnProx = new Button { Text = "▶", FontSize = 18, BackgroundColor = Colors.Transparent, TextColor = cPrimary, WidthRequest = 44, HeightRequest = 36, Padding = 0 };
        btnProx.Clicked += (s, e) => NavegarMes(1);
        _lblMesRef = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cPrimary, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center };

        var navMes = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(8, 4),
            Children = { btnAnt, _lblMesRef, btnProx }
        };
        Grid.SetColumn(_lblMesRef, 1);
        Grid.SetColumn(btnProx, 2);

        // KPIs
        _lblPA = CriarLabelKPI("0,0");
        _lblPM = CriarLabelKPI("R$ 0");
        _lblTKM = CriarLabelKPI("R$ 0");
        _lblTotalMes = CriarLabelKPI("R$ 0");
        _lblQtdVendas = CriarLabelKPI("0");
        _lblQtdItens = CriarLabelKPI("0");

        var gridKPI = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 8, RowSpacing = 8,
            Children =
            {
                CriarCardKPI("PA", "Peças/Atend.", _lblPA, "#1565C0"),
                CriarCardKPI("PM", "Preço Médio", _lblPM, "#00897B"),
                CriarCardKPI("TKM", "Ticket Médio", _lblTKM, "#E65100"),
                CriarCardKPI("💰", "Total Mês", _lblTotalMes, "#2E7D32"),
                CriarCardKPI("🧾", "Vendas", _lblQtdVendas, "#5C6BC0"),
                CriarCardKPI("📦", "Itens", _lblQtdItens, "#7B1FA2")
            }
        };
        // Position KPIs in grid
        Grid.SetColumn(gridKPI.Children[0] as View, 0); Grid.SetRow(gridKPI.Children[0] as View, 0);
        Grid.SetColumn(gridKPI.Children[1] as View, 1); Grid.SetRow(gridKPI.Children[1] as View, 0);
        Grid.SetColumn(gridKPI.Children[2] as View, 2); Grid.SetRow(gridKPI.Children[2] as View, 0);
        Grid.SetColumn(gridKPI.Children[3] as View, 0); Grid.SetRow(gridKPI.Children[3] as View, 1);
        Grid.SetColumn(gridKPI.Children[4] as View, 1); Grid.SetRow(gridKPI.Children[4] as View, 1);
        Grid.SetColumn(gridKPI.Children[5] as View, 2); Grid.SetRow(gridKPI.Children[5] as View, 1);

        // Melhores Dias
        _stackDias = new StackLayout { Spacing = 4 };
        var cardDias = CriarSecao("📅 Melhores Dias", _stackDias);

        // Melhores Produtos
        _stackProdutos = new StackLayout { Spacing = 4 };
        var cardProdutos = CriarSecao("🏆 Top Produtos", _stackProdutos);

        _loading = new ActivityIndicator { Color = cPrimary, IsRunning = false, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(12, 8, 12, 20),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { navMes, _loading, gridKPI, cardDias, cardProdutos }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    private static Label CriarLabelKPI(string txt)
    {
        return new Label { Text = txt, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#212121"), HorizontalTextAlignment = TextAlignment.Center };
    }

    private static Border CriarCardKPI(string icone, string titulo, Label lblValor, string cor)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#E0E6ED"), StrokeThickness = 1,
            Padding = new Thickness(6, 10),
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#18000000")), Offset = new Point(0, 1), Radius = 4 },
            Content = new StackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = icone, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(cor), HorizontalTextAlignment = TextAlignment.Center },
                    lblValor,
                    new Label { Text = titulo, FontSize = 9, TextColor = Color.FromArgb("#90A4AE"), HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };
    }

    private static Border CriarSecao(string titulo, StackLayout content)
    {
        var stack = new StackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = titulo, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#263238") },
                new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0E6ED") },
                content
            }
        };
        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#E0E6ED"), StrokeThickness = 1,
            Padding = new Thickness(14, 12),
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#18000000")), Offset = new Point(0, 1), Radius = 4 },
            Content = stack
        };
    }

    private void NavegarMes(int dir)
    {
        _mesAtual += dir;
        if (_mesAtual > 12) { _mesAtual = 1; _anoAtual++; }
        if (_mesAtual < 1) { _mesAtual = 12; _anoAtual--; }
        CarregarDadosAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarDadosAsync();
    }

    private async void CarregarDadosAsync()
    {
        _loading.IsRunning = true;
        _lblMesRef.Text = $"{NomesMes[_mesAtual]} {_anoAtual}";
        var ci = new System.Globalization.CultureInfo("pt-BR");

        try
        {
            await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            var vid = AuthService.VendedorId;
            var ini = new DateTime(_anoAtual, _mesAtual, 1);
            var fim = ini.AddMonths(1).AddDays(-1);

            // --- KPIs: vendas fechadas + pedidos faturados/expedidos ---
            var sqlKPI = @"SELECT
                COUNT(*) AS qtd_vendas,
                COALESCE(SUM(x.valor_total), 0) AS total_vendido,
                COALESCE(SUM(x.qtd_itens), 0) AS total_itens
            FROM (
                SELECT v.valor_total,
                    (SELECT COALESCE(SUM(vi.quantidade),0) FROM vendas_itens vi WHERE vi.venda_id = v.codigo) AS qtd_itens,
                    v.data_cadastro, v.vendedor_id
                FROM vendas v WHERE v.status = 'F'
                UNION ALL
                SELECT p.valor_total,
                    (SELECT COALESCE(SUM(pi.quantidade),0) FROM pedidos_itens pi WHERE pi.pedido_id = p.codigo) AS qtd_itens,
                    p.data_cadastro, p.vendedor_id
                FROM pedidos p WHERE p.status IN ('F','E')
            ) x
            WHERE x.vendedor_id = @vid
                AND DATE(x.data_cadastro) >= @ini AND DATE(x.data_cadastro) <= @fim";

            await using var cmdKPI = new NpgsqlCommand(sqlKPI, conn);
            cmdKPI.Parameters.AddWithValue("@vid", vid);
            cmdKPI.Parameters.AddWithValue("@ini", ini);
            cmdKPI.Parameters.AddWithValue("@fim", fim);

            long qtdVendas = 0; decimal totalVendido = 0; decimal totalItens = 0;
            await using (var r = await cmdKPI.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    qtdVendas = r.GetInt64(0);
                    totalVendido = r.GetDecimal(1);
                    totalItens = r.GetDecimal(2);
                }
            }

            var tkm = qtdVendas > 0 ? totalVendido / qtdVendas : 0;
            var pa = qtdVendas > 0 ? totalItens / qtdVendas : 0;
            var pm = totalItens > 0 ? totalVendido / totalItens : 0;

            _lblPA.Text = pa.ToString("N1", ci);
            _lblPM.Text = pm.ToString("C0", ci);
            _lblTKM.Text = tkm.ToString("C0", ci);
            _lblTotalMes.Text = totalVendido.ToString("C0", ci);
            _lblQtdVendas.Text = qtdVendas.ToString();
            _lblQtdItens.Text = totalItens.ToString("N0", ci);

            // --- Melhores Dias (top 5) ---
            var sqlDias = @"SELECT DATE(x.data_cadastro) AS dia, SUM(x.valor_total) AS total, COUNT(*) AS qtd
            FROM (
                SELECT valor_total, data_cadastro, vendedor_id FROM vendas WHERE status = 'F'
                UNION ALL SELECT valor_total, data_cadastro, vendedor_id FROM pedidos WHERE status IN ('F','E')
            ) x
            WHERE x.vendedor_id = @vid AND DATE(x.data_cadastro) >= @ini AND DATE(x.data_cadastro) <= @fim
            GROUP BY DATE(x.data_cadastro) ORDER BY total DESC LIMIT 5";

            await using var cmdDias = new NpgsqlCommand(sqlDias, conn);
            cmdDias.Parameters.AddWithValue("@vid", vid);
            cmdDias.Parameters.AddWithValue("@ini", ini);
            cmdDias.Parameters.AddWithValue("@fim", fim);

            _stackDias.Children.Clear();
            var melhorDia = 0m;
            await using (var r = await cmdDias.ExecuteReaderAsync())
            {
                int pos = 0;
                while (await r.ReadAsync())
                {
                    pos++;
                    var dia = r.GetDateTime(0);
                    var total = r.GetDecimal(1);
                    var qtd = r.GetInt64(2);
                    if (pos == 1) melhorDia = total;
                    var pct = melhorDia > 0 ? (float)(total / melhorDia) : 0f;
                    var diaSem = NomesDia[(int)dia.DayOfWeek];
                    var medal = pos switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $" {pos}." };

                    var barColor = pos switch { 1 => "#1565C0", 2 => "#1976D2", 3 => "#42A5F5", _ => "#90CAF9" };

                    var row = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(new GridLength(30)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Padding = new Thickness(0, 2),
                        Children =
                        {
                            new Label { Text = medal, FontSize = 14, VerticalTextAlignment = TextAlignment.Center },
                            new StackLayout
                            {
                                Spacing = 2,
                                Children =
                                {
                                    new Label { Text = $"{dia:dd/MM} ({diaSem})", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#263238") },
                                    new Border
                                    {
                                        BackgroundColor = Color.FromArgb(barColor),
                                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 3 },
                                        StrokeThickness = 0,
                                        HeightRequest = 6,
                                        WidthRequest = 200 * pct,
                                        HorizontalOptions = LayoutOptions.Start
                                    }
                                }
                            },
                            new StackLayout
                            {
                                Spacing = 0,
                                Children =
                                {
                                    new Label { Text = total.ToString("C0", ci), FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0"), HorizontalTextAlignment = TextAlignment.End },
                                    new Label { Text = $"{qtd} venda(s)", FontSize = 10, TextColor = Color.FromArgb("#90A4AE"), HorizontalTextAlignment = TextAlignment.End }
                                }
                            }
                        }
                    };
                    Grid.SetColumn(row.Children[0] as View, 0);
                    Grid.SetColumn(row.Children[1] as View, 1);
                    Grid.SetColumn(row.Children[2] as View, 2);
                    _stackDias.Children.Add(row);
                }
            }
            if (_stackDias.Children.Count == 0)
                _stackDias.Children.Add(new Label { Text = "Nenhuma venda neste mês.", FontSize = 12, TextColor = Color.FromArgb("#90A4AE") });

            // --- Top Produtos (top 10) ---
            var sqlProd = @"SELECT pr.nome, SUM(it.quantidade) AS qtd, SUM(it.valor_total) AS total
            FROM (
                SELECT vi.variacao_id, vi.quantidade, vi.valor_total, v.vendedor_id, v.data_cadastro
                FROM vendas_itens vi JOIN vendas v ON v.codigo = vi.venda_id WHERE v.status = 'F'
                UNION ALL
                SELECT pi.variacao_id, pi.quantidade, pi.valor_total, p.vendedor_id, p.data_cadastro
                FROM pedidos_itens pi JOIN pedidos p ON p.codigo = pi.pedido_id WHERE p.status IN ('F','E')
            ) it
            JOIN produtos_variacoes pv ON pv.codigo = it.variacao_id
            JOIN produtos pr ON pr.codigo = pv.produto_id
            WHERE it.vendedor_id = @vid AND DATE(it.data_cadastro) >= @ini AND DATE(it.data_cadastro) <= @fim
            GROUP BY pr.nome ORDER BY total DESC LIMIT 10";

            await using var cmdProd = new NpgsqlCommand(sqlProd, conn);
            cmdProd.Parameters.AddWithValue("@vid", vid);
            cmdProd.Parameters.AddWithValue("@ini", ini);
            cmdProd.Parameters.AddWithValue("@fim", fim);

            _stackProdutos.Children.Clear();
            await using (var r = await cmdProd.ExecuteReaderAsync())
            {
                int pos = 0;
                while (await r.ReadAsync())
                {
                    pos++;
                    var desc = r.GetString(0);
                    var qtd = r.GetDecimal(1);
                    var total = r.GetDecimal(2);
                    var corPos = pos <= 3 ? "#1565C0" : "#546E7A";

                    var row = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(new GridLength(24)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Padding = new Thickness(0, 3),
                        Children =
                        {
                            new Label { Text = $"{pos}.", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(corPos), VerticalTextAlignment = TextAlignment.Center },
                            new StackLayout
                            {
                                Spacing = 0,
                                Children =
                                {
                                    new Label { Text = desc, FontSize = 12, TextColor = Color.FromArgb("#263238"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 },
                                    new Label { Text = $"{qtd:N0} un", FontSize = 10, TextColor = Color.FromArgb("#90A4AE") }
                                }
                            },
                            new Label { Text = total.ToString("C0", ci), FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(corPos), VerticalTextAlignment = TextAlignment.Center }
                        }
                    };
                    Grid.SetColumn(row.Children[0] as View, 0);
                    Grid.SetColumn(row.Children[1] as View, 1);
                    Grid.SetColumn(row.Children[2] as View, 2);
                    _stackProdutos.Children.Add(row);
                }
            }
            if (_stackProdutos.Children.Count == 0)
                _stackProdutos.Children.Add(new Label { Text = "Nenhum produto vendido neste mês.", FontSize = 12, TextColor = Color.FromArgb("#90A4AE") });
        }
        catch (Exception ex)
        {
            _stackDias.Children.Clear();
            _stackDias.Children.Add(new Label { Text = "Erro: " + ex.Message, FontSize = 11, TextColor = Colors.Red });
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }
}
