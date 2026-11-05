using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class BuscarClientePage : ContentPage
{
    private readonly Entry _txtBusca;
    private readonly StackLayout _listaStack;
    private readonly Label _lblStatus;
    private readonly Action<ClienteDto>? _onSelecionado;

    public BuscarClientePage(Action<ClienteDto>? onSelecionado = null)
    {
        _onSelecionado = onSelecionado;
        BackgroundColor = Color.FromArgb("#D6E8FF");

        var cAzul = Color.FromArgb("#2A66B8");

        _txtBusca = new Entry { Placeholder = "Nome, CPF/CNPJ ou celular...", FontSize = 15 };
        _txtBusca.Completed += async (s, e) => await BuscarAsync();

        var btnBuscar = new Button { Text = "Buscar", BackgroundColor = cAzul, TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnBuscar.Clicked += async (s, e) => await BuscarAsync();

        var btnNovo = new Button { Text = "+ Cadastrar Novo Cliente", BackgroundColor = Color.FromArgb("#2EA043"), TextColor = Colors.White, FontSize = 14, CornerRadius = 8, HeightRequest = 44 };
        btnNovo.Clicked += OnCadastrarNovoClicked;

        _lblStatus = new Label { FontSize = 12, TextColor = Color.FromArgb("#666"), HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
        _listaStack = new StackLayout { Spacing = 8 };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                new StackLayout { BackgroundColor = cAzul, Padding = new Thickness(20, 50, 20, 16), Children = {
                    new Label { Text = "Buscar Cliente", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center }
                }},
                new ScrollView { Padding = new Thickness(16, 12), Content = new StackLayout { Spacing = 10, Children = { _txtBusca, btnBuscar, btnNovo, _lblStatus, _listaStack } } }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    private async Task BuscarAsync()
    {
        var termo = _txtBusca.Text?.Trim() ?? "";
        if (termo.Length < 2) { _lblStatus.Text = "Digite ao menos 2 caracteres"; _lblStatus.IsVisible = true; return; }

        _lblStatus.Text = "Buscando..."; _lblStatus.IsVisible = true;
        _listaStack.Children.Clear();

        try
        {
            var lista = await DataService.BuscarClientesAsync(termo);
            _lblStatus.Text = lista.Count + " cliente(s) encontrado(s)";

            foreach (var c in lista)
            {
                var card = new StackLayout
                {
                    BackgroundColor = Colors.White, Padding = new Thickness(14), Spacing = 2,
                    Children =
                    {
                        new Label { Text = c.NomeRazao, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#274176") },
                        new Label { Text = (string.IsNullOrEmpty(c.Documento) ? "" : "Doc: " + c.Documento + "  ") + (string.IsNullOrEmpty(c.Celular) ? c.Telefone : c.Celular), FontSize = 12, TextColor = Color.FromArgb("#666") },
                        new Label { Text = (string.IsNullOrEmpty(c.Cidade) ? "" : c.Cidade + "/" + c.Uf), FontSize = 12, TextColor = Color.FromArgb("#999") }
                    }
                };

                if (_onSelecionado != null)
                {
                    var cliente = c;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += (s, e) => _onSelecionado(cliente);
                    card.GestureRecognizers.Add(tap);
                }
                _listaStack.Children.Add(card);
            }
        }
        catch (Exception ex) { _lblStatus.Text = "Erro: " + ex.Message; }
    }

    private async void OnCadastrarNovoClicked(object? sender, EventArgs e)
    {
        try
        {
            var tcs = new TaskCompletionSource<ClienteCadastroResult?>();

            var page = new CadastrarClientePage(result =>
            {
                tcs.TrySetResult(result);
            });

            await Navigation.PushAsync(page);

            var cli = await tcs.Task;
            if (cli != null && _onSelecionado != null)
            {
                _onSelecionado(new ClienteDto
                {
                    Codigo = cli.Codigo,
                    NomeRazao = cli.NomeRazao,
                    Documento = cli.Documento ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}

// Classe auxiliar para retornar do cadastro
public record ClienteCadastroResult(long Codigo, string NomeRazao, string? Documento);
