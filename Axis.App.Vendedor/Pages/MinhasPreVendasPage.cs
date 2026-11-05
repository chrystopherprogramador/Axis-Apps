using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class MinhasPreVendasPage : ContentPage
{
    private readonly StackLayout _listaStack;
    private readonly Label _lblStatus;

    public MinhasPreVendasPage()
    {
        BackgroundColor = Color.FromArgb("#D6E8FF");
        var cAzul = Color.FromArgb("#2A66B8");

        _listaStack = new StackLayout { Spacing = 8 };
        _lblStatus = new Label { FontSize = 12, TextColor = Color.FromArgb("#666"), HorizontalTextAlignment = TextAlignment.Center };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                new StackLayout { BackgroundColor = cAzul, Padding = new Thickness(20, 50, 20, 16), Children = {
                    new Label { Text = "Pre-Vendas Abertas", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Toque para editar", FontSize = 12, TextColor = Color.FromArgb("#BFD7FF"), HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0,4,0,0) }
                }},
                new ScrollView { Padding = new Thickness(16, 12), Content = new StackLayout { Spacing = 10, Children = { _lblStatus, _listaStack } } }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        _lblStatus.Text = "Carregando...";
        _listaStack.Children.Clear();

        try
        {
            var lista = await DataService.ListarPreVendasVendedorAsync(AuthService.VendedorId);

            if (lista.Count == 0)
            {
                _lblStatus.Text = "Nenhuma pre-venda aberta";
                return;
            }

            _lblStatus.Text = lista.Count + " pre-venda(s) aberta(s)";
            var ptBr = new System.Globalization.CultureInfo("pt-BR");

            foreach (var pv in lista)
            {
                var card = new StackLayout
                {
                    BackgroundColor = Colors.White, Padding = new Thickness(14), Spacing = 4,
                    Children =
                    {
                        new Grid
                        {
                            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                            Children =
                            {
                                new Label { Text = "#" + pv.Numero + " - " + pv.NomeCliente, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#274176"), LineBreakMode = LineBreakMode.TailTruncation },
                            }
                        },
                        new Grid
                        {
                            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                            Children =
                            {
                                new Label { Text = pv.Data.ToString("dd/MM/yyyy HH:mm"), FontSize = 12, TextColor = Color.FromArgb("#888") },
                            }
                        }
                    }
                };

                var lblAberta = new Label { Text = "Aberta", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2A66B8"), HorizontalTextAlignment = TextAlignment.End };
                ((Grid)card.Children[0]).Children.Add(lblAberta); Grid.SetColumn(lblAberta, 1);

                var lblTotal = new Label { Text = pv.ValorTotal.ToString("C2", ptBr), FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2EA043"), HorizontalTextAlignment = TextAlignment.End };
                ((Grid)card.Children[1]).Children.Add(lblTotal); Grid.SetColumn(lblTotal, 1);

                // Toque para editar
                var preVenda = pv;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    // Buscar cliente_id da pré-venda
                    long clienteId = 0;
                    try
                    {
                        await using var conn = new Npgsql.NpgsqlConnection(ConfigService.ConnectionString);
                        await conn.OpenAsync();
                        await using var cmd = new Npgsql.NpgsqlCommand("SELECT COALESCE(pessoa_id, 0) FROM pre_vendas WHERE codigo = @id", conn);
                        cmd.Parameters.AddWithValue("@id", preVenda.Codigo);
                        clienteId = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
                    }
                    catch { }

                    await Navigation.PushAsync(new PreVendaPage(preVenda.Codigo, clienteId, preVenda.NomeCliente));
                };
                card.GestureRecognizers.Add(tap);

                _listaStack.Children.Add(card);
            }
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
    }
}
