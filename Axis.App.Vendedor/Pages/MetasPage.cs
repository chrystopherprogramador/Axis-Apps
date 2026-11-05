using Axis.App.Vendedor.Services;
using Npgsql;

namespace Axis.App.Vendedor.Pages;

public class MetasPage : ContentPage
{
    private GaugeDrawable _gaugeDiario = null!;
    private GaugeDrawable _gaugeSemanal = null!;
    private GaugeDrawable _gaugeMensal = null!;
    private GraphicsView _gvDiario = null!;
    private GraphicsView _gvSemanal = null!;
    private GraphicsView _gvMensal = null!;
    private Label _lblPeriodo = null!;
    private Label _lblStatus = null!;
    private Label _lblFalta = null!;
    private ActivityIndicator _loading = null!;

    private static readonly string[] NomesDia = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];

    public MetasPage()
    {
        try { BuildUI(); }
        catch (Exception ex) { Content = new Label { Text = "Erro: " + ex.Message, TextColor = Colors.Red, Margin = 30 }; }
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);

        var cPrimary = Color.FromArgb("#1565C0");

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
                        new Label { Text = "Minha Meta", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = AuthService.VendedorNome, FontSize = 12, TextColor = Color.FromArgb("#B3D4FC") }
                    }
                }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        // Gauges
        _gaugeDiario = new GaugeDrawable { Titulo = "Hoje" };
        _gaugeSemanal = new GaugeDrawable { Titulo = "Semana" };
        _gaugeMensal = new GaugeDrawable { Titulo = "Mes" };

        _gvDiario = new GraphicsView { Drawable = _gaugeDiario, HeightRequest = 200, HorizontalOptions = LayoutOptions.Fill };
        _gvSemanal = new GraphicsView { Drawable = _gaugeSemanal, HeightRequest = 165, HorizontalOptions = LayoutOptions.Fill };
        _gvMensal = new GraphicsView { Drawable = _gaugeMensal, HeightRequest = 165, HorizontalOptions = LayoutOptions.Fill };

        var cardDiario = CriarCard(_gvDiario);
        var cardSemanal = CriarCard(_gvSemanal);
        var cardMensal = CriarCard(_gvMensal);

        _lblPeriodo = new Label { FontSize = 12, TextColor = Color.FromArgb("#78909C"), HorizontalTextAlignment = TextAlignment.Center };
        _lblStatus = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cPrimary, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2) };
        _lblFalta = new Label { FontSize = 12, TextColor = Color.FromArgb("#546E7A"), HorizontalTextAlignment = TextAlignment.Center };
        _loading = new ActivityIndicator { Color = cPrimary, IsRunning = false, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center };

        var gridDuplo = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { cardSemanal, cardMensal }
        };
        Grid.SetColumn(cardSemanal, 0);
        Grid.SetColumn(cardMensal, 1);

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView
                {
                    Padding = new Thickness(16, 12, 16, 20),
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = { _loading, _lblStatus, cardDiario, gridDuplo, _lblFalta, _lblPeriodo }
                    }
                }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    private static Border CriarCard(View content)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Stroke = Color.FromArgb("#E0E6ED"),
            StrokeThickness = 1,
            Padding = new Thickness(6, 10),
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#20000000")), Offset = new Point(0, 2), Radius = 6 },
            Content = content
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarMetasAsync();
    }

    private async void CarregarMetasAsync()
    {
        _loading.IsRunning = true;
        _lblStatus.Text = "Carregando...";
        _lblFalta.Text = "";

        try
        {
            await using var conn = new NpgsqlConnection(ConfigService.ConnectionString);
            await conn.OpenAsync();

            var vendedorId = AuthService.VendedorId;
            var hoje = DateTime.Today;
            var ci = new System.Globalization.CultureInfo("pt-BR");

            // 1. Buscar meta do vendedor (tipo V)
            var sqlMeta = @"SELECT valor_meta, data_inicio, data_fim, COALESCE(distribuicao,'') as distribuicao
                            FROM metas
                            WHERE tipo = 'V' AND vendedor_id = @vid AND ativo = true
                            AND @dt >= data_inicio AND @dt <= data_fim
                            ORDER BY data_inicio DESC LIMIT 1";
            await using var cmdMeta = new NpgsqlCommand(sqlMeta, conn);
            cmdMeta.Parameters.AddWithValue("@vid", vendedorId);
            cmdMeta.Parameters.AddWithValue("@dt", hoje);

            decimal valorMeta = 0;
            DateTime dataInicio = hoje, dataFim = hoje;
            string distribuicaoJson = "";

            await using (var r = await cmdMeta.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    valorMeta = r.GetDecimal(0);
                    dataInicio = r.GetDateTime(1);
                    dataFim = r.GetDateTime(2);
                    distribuicaoJson = r.GetString(3);
                }
            }

            if (valorMeta == 0)
            {
                _lblStatus.Text = "Nenhuma meta definida.";
                _lblStatus.TextColor = Color.FromArgb("#E53935");
                _lblFalta.Text = "Solicite ao gestor para definir sua meta mensal.";
                _loading.IsRunning = false;
                AtualizarGauge(_gaugeDiario, _gvDiario, 0, 0);
                AtualizarGauge(_gaugeSemanal, _gvSemanal, 0, 0);
                AtualizarGauge(_gaugeMensal, _gvMensal, 0, 0);
                return;
            }

            // 2. Calcular metas (distribuicao JSON ou proporcional)
            var diasPeriodo = (dataFim - dataInicio).Days + 1;
            decimal metaDiaria = 0, metaSemanal = 0, metaMensal = valorMeta;

            var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek + (int)DayOfWeek.Monday);
            if (hoje.DayOfWeek == DayOfWeek.Sunday) inicioSemana = inicioSemana.AddDays(-7);
            var fimSemana = inicioSemana.AddDays(6);
            if (inicioSemana < dataInicio) inicioSemana = dataInicio;
            if (fimSemana > dataFim) fimSemana = dataFim;

            bool usouDist = false;
            if (!string.IsNullOrEmpty(distribuicaoJson) && distribuicaoJson.Length > 5)
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(distribuicaoJson);
                    string[] nomesDiaJson = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];
                    var diaHoje = nomesDiaJson[(int)hoje.DayOfWeek];

                    foreach (var semEl in doc.RootElement.GetProperty("semanas").EnumerateArray())
                    {
                        var semIni = DateTime.Parse(semEl.GetProperty("inicio").GetString()!);
                        var semFim = DateTime.Parse(semEl.GetProperty("fim").GetString()!);

                        if (hoje >= semIni && hoje <= semFim && semEl.TryGetProperty("dias", out var dias))
                        {
                            if (dias.TryGetProperty(diaHoje, out var valDia))
                                metaDiaria = (decimal)valDia.GetDouble();
                        }

                        if (inicioSemana >= semIni && inicioSemana <= semFim)
                            metaSemanal = (decimal)semEl.GetProperty("valor").GetDouble();
                    }
                    usouDist = true;
                }
                catch { /* fallback proporcional */ }
            }

            if (!usouDist)
            {
                metaDiaria = valorMeta / diasPeriodo;
                var diasSemana = (fimSemana - inicioSemana).Days + 1;
                metaSemanal = metaDiaria * diasSemana;
            }

            // 3. Buscar vendas fechadas + pedidos faturados/expedidos
            var sqlDiario = @"SELECT COALESCE(SUM(x.valor_total), 0) FROM (
                SELECT valor_total, vendedor_id, data_cadastro FROM vendas WHERE status = 'F'
                UNION ALL SELECT valor_total, vendedor_id, data_cadastro FROM pedidos WHERE status IN ('F','E')
            ) x WHERE x.vendedor_id = @vid AND DATE(x.data_cadastro) = CURRENT_DATE";
            await using var cmdDiario = new NpgsqlCommand(sqlDiario, conn);
            cmdDiario.Parameters.AddWithValue("@vid", vendedorId);
            var vendaDiaria = Convert.ToDecimal(await cmdDiario.ExecuteScalarAsync() ?? 0m);

            var sqlSemanal = @"SELECT COALESCE(SUM(x.valor_total), 0) FROM (
                SELECT valor_total, vendedor_id, data_cadastro FROM vendas WHERE status = 'F'
                UNION ALL SELECT valor_total, vendedor_id, data_cadastro FROM pedidos WHERE status IN ('F','E')
            ) x WHERE x.vendedor_id = @vid
                AND DATE(x.data_cadastro) >= @ini AND DATE(x.data_cadastro) <= @fim";
            await using var cmdSemanal = new NpgsqlCommand(sqlSemanal, conn);
            cmdSemanal.Parameters.AddWithValue("@vid", vendedorId);
            cmdSemanal.Parameters.AddWithValue("@ini", inicioSemana);
            cmdSemanal.Parameters.AddWithValue("@fim", fimSemana);
            var vendaSemanal = Convert.ToDecimal(await cmdSemanal.ExecuteScalarAsync() ?? 0m);

            var sqlMensal = @"SELECT COALESCE(SUM(x.valor_total), 0) FROM (
                SELECT valor_total, vendedor_id, data_cadastro FROM vendas WHERE status = 'F'
                UNION ALL SELECT valor_total, vendedor_id, data_cadastro FROM pedidos WHERE status IN ('F','E')
            ) x WHERE x.vendedor_id = @vid
                AND DATE(x.data_cadastro) >= @ini AND DATE(x.data_cadastro) <= @fim";
            await using var cmdMensal = new NpgsqlCommand(sqlMensal, conn);
            cmdMensal.Parameters.AddWithValue("@vid", vendedorId);
            cmdMensal.Parameters.AddWithValue("@ini", dataInicio);
            cmdMensal.Parameters.AddWithValue("@fim", dataFim);
            var vendaMensal = Convert.ToDecimal(await cmdMensal.ExecuteScalarAsync() ?? 0m);

            // 4. Atualizar gauges
            AtualizarGauge(_gaugeDiario, _gvDiario, (float)vendaDiaria, (float)metaDiaria,
                vendaDiaria.ToString("C0", ci), metaDiaria.ToString("C0", ci));
            AtualizarGauge(_gaugeSemanal, _gvSemanal, (float)vendaSemanal, (float)metaSemanal,
                vendaSemanal.ToString("C0", ci), metaSemanal.ToString("C0", ci));
            AtualizarGauge(_gaugeMensal, _gvMensal, (float)vendaMensal, (float)metaMensal,
                vendaMensal.ToString("C0", ci), metaMensal.ToString("C0", ci));

            // 5. Labels informativos
            var faltaMes = Math.Max(metaMensal - vendaMensal, 0);
            var pctMes = metaMensal > 0 ? vendaMensal / metaMensal * 100 : 0;

            _lblStatus.TextColor = pctMes >= 100 ? Color.FromArgb("#43A047") : Color.FromArgb("#1565C0");
            _lblStatus.Text = pctMes >= 100
                ? $"Meta batida! {pctMes:N0}% do mes"
                : $"Meta mensal: {valorMeta.ToString("C0", ci)}";

            _lblFalta.Text = faltaMes > 0
                ? $"Falta {faltaMes.ToString("C0", ci)} para bater a meta do mes"
                : "";

            var diaSemNome = NomesDia[(int)hoje.DayOfWeek];
            _lblPeriodo.Text = $"{hoje:dd/MM/yyyy} ({diaSemNome})  •  {dataInicio:dd/MM} a {dataFim:dd/MM}";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Erro ao carregar";
            _lblStatus.TextColor = Color.FromArgb("#E53935");
            _lblFalta.Text = ex.Message;
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }

    private static void AtualizarGauge(GaugeDrawable gauge, GraphicsView gv,
        float valor, float meta, string? valorFmt = null, string? metaFmt = null)
    {
        gauge.Valor = valor;
        gauge.Meta = meta;
        gauge.ValorFormatado = valorFmt ?? "";
        gauge.MetaFormatada = metaFmt ?? (meta > 0 ? $"{meta:N0}" : "—");
        gv.Invalidate();
    }
}
