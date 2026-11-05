using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class PreVendaPage : ContentPage
{
    private long _preVendaId;
    private long _clienteId;
    private string _clienteNome = "Sem cliente";
    private Label _lblCliente = null!, _lblTotal = null!, _lblNumero = null!;
    private StackLayout _itensStack = null!;
    private Entry _txtObs = null!;

    /// <summary>Nova pré-venda</summary>
    public PreVendaPage() : this(0, 0, "Sem cliente") { }

    /// <summary>Editar pré-venda existente</summary>
    public PreVendaPage(long preVendaId, long clienteId, string clienteNome)
    {
        _preVendaId = preVendaId;
        _clienteId = clienteId;
        _clienteNome = clienteNome;
        BackgroundColor = Color.FromArgb("#D6E8FF");

        var cAzul = Color.FromArgb("#2A66B8");
        var cTexto = Color.FromArgb("#274176");
        var cVerde = Color.FromArgb("#2EA043");

        _lblNumero = new Label { Text = "Nova Pre-Venda", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        _lblCliente = new Label { Text = "Cliente: Nenhum selecionado", FontSize = 13, TextColor = Color.FromArgb("#BFD7FF"), HorizontalTextAlignment = TextAlignment.Center };
        _lblTotal = new Label { Text = "R$ 0,00", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = cVerde, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8) };
        _itensStack = new StackLayout { Spacing = 6 };
        _txtObs = new Entry { Placeholder = "Observacao (opcional)", FontSize = 13 };

        var btnCliente = new Button { Text = "Selecionar Cliente", BackgroundColor = cAzul, TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnCliente.Clicked += OnSelecionarCliente;

        var btnProduto = new Button { Text = "+ Adicionar Produto", BackgroundColor = cVerde, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 48 };
        btnProduto.Clicked += OnAdicionarProduto;

        var btnFinalizar = new Button { Text = "Salvar Pre-Venda", BackgroundColor = Color.FromArgb("#2EA043"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 50 };
        btnFinalizar.Clicked += OnFinalizarClicked;

        var btnCancelar = new Button { Text = "Cancelar", BackgroundColor = Color.FromArgb("#C0392B"), TextColor = Colors.White, FontSize = 13, CornerRadius = 8, HeightRequest = 38 };
        btnCancelar.Clicked += OnCancelarClicked;

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children =
            {
                // Header
                new StackLayout { BackgroundColor = cAzul, Padding = new Thickness(20, 50, 20, 16), Children = { _lblNumero, _lblCliente } },
                // Body
                new ScrollView
                {
                    Padding = new Thickness(16, 12),
                    Content = new StackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            btnCliente,
                            _lblTotal,
                            new Label { Text = "Itens:", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                            _itensStack,
                            btnProduto,
                            _txtObs
                        }
                    }
                },
                // Bottom
                new StackLayout { Padding = new Thickness(16, 8, 16, 24), Spacing = 8, BackgroundColor = Colors.White, Children = { btnFinalizar, btnCancelar } }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
        Grid.SetRow((Content as Grid)!.Children[2] as View, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lblCliente.Text = "Cliente: " + _clienteNome;
        if (_preVendaId == 0)
            await CriarPreVendaAsync();
        else
        {
            _lblNumero.Text = "Pre-Venda #" + _preVendaId;
            await AtualizarItensAsync();
        }
    }

    private async Task CriarPreVendaAsync()
    {
        try
        {
            _preVendaId = await DataService.InserirPreVendaAsync(
                _clienteId, AuthService.VendedorId, AuthService.UsuarioId, _txtObs.Text ?? "");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", "Erro ao criar pre-venda: " + ex.Message, "OK");
            return;
        }

        try
        {
            _lblNumero.Text = "Pre-Venda #" + _preVendaId;
        }
        catch { }
    }

    private async void OnSelecionarCliente(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new BuscarClientePage(async cli =>
        {
            _clienteId = cli.Codigo;
            _clienteNome = cli.NomeRazao;
            _lblCliente.Text = "Cliente: " + cli.NomeRazao;

            // Atualizar pré-venda com o cliente
            if (_preVendaId > 0)
            {
                try
                {
                    await using var conn = new Npgsql.NpgsqlConnection(ConfigService.ConnectionString);
                    await conn.OpenAsync();
                    await using var cmd = new Npgsql.NpgsqlCommand("UPDATE pre_vendas SET pessoa_id = @pid WHERE codigo = @id", conn);
                    cmd.Parameters.AddWithValue("@pid", _clienteId);
                    cmd.Parameters.AddWithValue("@id", _preVendaId);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }
            }
            await Navigation.PopAsync();
        }));
    }

    private async void OnAdicionarProduto(object? sender, EventArgs e)
    {
        if (_preVendaId == 0) { await DisplayAlert("Aviso", "Aguarde a pre-venda ser criada.", "OK"); return; }

        await Navigation.PushAsync(new BuscarProdutoPage(async produto =>
        {
            // Pedir quantidade
            var qtdStr = await DisplayPromptAsync("Quantidade", "Informe a quantidade de " + produto.Nome + ":", "OK", "Cancelar", "1", keyboard: Keyboard.Numeric, initialValue: "1");
            if (string.IsNullOrWhiteSpace(qtdStr) || !decimal.TryParse(qtdStr, out var qtd) || qtd <= 0) return;

            try
            {
                await DataService.InserirItemPreVendaAsync(_preVendaId, produto.VariacaoId, qtd, produto.PrecoVenda, 0, "");
                await Navigation.PopAsync();
                await AtualizarItensAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Erro ao adicionar item: " + ex.Message, "OK");
            }
        }));
    }

    private async Task AtualizarItensAsync()
    {
        if (_preVendaId == 0) return;
        try
        {
            var itens = await DataService.ListarItensPreVendaAsync(_preVendaId);
            _itensStack.Children.Clear();
            decimal total = 0;

            foreach (var item in itens)
            {
                total += item.ValorTotal;
                var itemView = new Grid
                {
                    BackgroundColor = Colors.White, Padding = new Thickness(12), ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    Children =
                    {
                        new StackLayout
                        {
                            Spacing = 1,
                            Children =
                            {
                                new Label { Text = item.Descricao, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#274176"), LineBreakMode = LineBreakMode.TailTruncation },
                                new Label { Text = item.Quantidade.ToString("N2") + " x " + item.ValorUnitario.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), FontSize = 12, TextColor = Color.FromArgb("#666") }
                            }
                        }
                    }
                };

                var lblValor = new Label { Text = item.ValorTotal.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2EA043"), VerticalTextAlignment = TextAlignment.Center };
                itemView.Children.Add(lblValor); Grid.SetColumn(lblValor, 1);

                // Swipe para remover
                var itemId = item.Codigo;
                var tapRemover = new TapGestureRecognizer();
                tapRemover.Tapped += async (s, ev) =>
                {
                    var confirma = await DisplayAlert("Remover", "Remover este item?", "Sim", "Nao");
                    if (confirma)
                    {
                        await DataService.RemoverItemPreVendaAsync(itemId, _preVendaId);
                        await AtualizarItensAsync();
                    }
                };
                itemView.GestureRecognizers.Add(tapRemover);

                _itensStack.Children.Add(itemView);
            }

            var ptBr = new System.Globalization.CultureInfo("pt-BR");
            _lblTotal.Text = total.ToString("C2", ptBr);
        }
        catch (Exception ex)
        {
            _lblTotal.Text = "Erro: " + ex.Message;
        }
    }

    private async void OnFinalizarClicked(object? sender, EventArgs e)
    {
        if (_preVendaId == 0) return;

        if (_clienteId <= 0)
        {
            await DisplayAlert("Aviso", "Selecione um cliente antes de salvar a pre-venda.", "OK");
            return;
        }

        try
        {
            await DataService.SalvarPreVendaAsync(_preVendaId, _txtObs.Text ?? "");
            await DisplayAlert("Sucesso", "Pre-venda salva! Ela sera finalizada no Axis Desktop.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
    }

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        if (_preVendaId == 0) { await Navigation.PopAsync(); return; }
        var confirma = await DisplayAlert("Cancelar", "Cancelar esta pre-venda?", "Sim", "Nao");
        if (!confirma) return;

        try
        {
            await DataService.CancelarPreVendaAsync(_preVendaId);
            await Navigation.PopAsync();
        }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
    }
}
