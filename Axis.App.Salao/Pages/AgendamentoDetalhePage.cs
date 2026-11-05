using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

[QueryProperty(nameof(Id), "id")]
public class AgendamentoDetalhePage : ContentPage
{
    public string Id { set { _id = long.TryParse(value, out var v) ? v : 0; } }
    private long _id;
    private AgendamentoDto? _ag;

    private readonly StackLayout _conteudo;
    private readonly ActivityIndicator _loading;

    private static readonly Color CRoxo = Color.FromArgb("#6f42c1");
    private static readonly Color CTexto = Color.FromArgb("#4A148C");

    public AgendamentoDetalhePage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");

        _loading = new ActivityIndicator { Color = CRoxo, IsRunning = true, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 30) };
        _conteudo = new StackLayout { Spacing = 14, Padding = new Thickness(20, 20) };

        var btnVoltar = new Button { Text = "‹", FontSize = 26, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = CRoxo,
            Padding = new Thickness(12, 48, 12, 16),
            Children =
            {
                btnVoltar,
                new Label { Text = "Atendimento", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children = { header, new ScrollView { Content = new StackLayout { Children = { _loading, _conteudo } } } }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        _loading.IsRunning = true;
        _conteudo.Children.Clear();
        try
        {
            _ag = await SalaoDataService.ObterAgendamentoAsync(_id);
            if (_ag == null)
            {
                _conteudo.Children.Add(new Label { Text = "Atendimento não encontrado.", TextColor = Colors.Red });
                return;
            }

            var servicos = await SalaoDataService.ListarServicosDoAgendamentoAsync(_id);

            _conteudo.Children.Add(CriarInfoCard(_ag, servicos));

            switch (_ag.Status)
            {
                case "M":
                    _conteudo.Children.Add(BotaoAcao("Confirmar Presença", Color.FromArgb("#2980B9"), () => AlterarStatus("C")));
                    _conteudo.Children.Add(BotaoAcao("Finalizar Atendimento", Color.FromArgb("#2EA043"), FinalizarAsync));
                    _conteudo.Children.Add(BotaoAcao("Marcar Falta", Color.FromArgb("#7F8C8D"), () => AlterarStatus("F")));
                    _conteudo.Children.Add(BotaoAcao("Cancelar", Color.FromArgb("#C0392B"), () => AlterarStatus("X")));
                    break;
                case "C":
                    _conteudo.Children.Add(BotaoAcao("Finalizar Atendimento", Color.FromArgb("#2EA043"), FinalizarAsync));
                    _conteudo.Children.Add(BotaoAcao("Marcar Falta", Color.FromArgb("#7F8C8D"), () => AlterarStatus("F")));
                    _conteudo.Children.Add(BotaoAcao("Cancelar", Color.FromArgb("#C0392B"), () => AlterarStatus("X")));
                    break;
                default:
                    _conteudo.Children.Add(new Label { Text = "Este atendimento está " + _ag.StatusDescricao.ToLower() + ".", FontSize = 14, TextColor = CTexto, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10) });
                    break;
            }
        }
        catch (Exception ex)
        {
            _conteudo.Children.Add(new Label { Text = "Erro: " + ex.Message, TextColor = Colors.Red });
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }

    private Frame CriarInfoCard(AgendamentoDto ag, List<ServicoAgendamentoDto> servicos)
    {
        var stack = new StackLayout
        {
            Spacing = 8,
            Children =
            {
                Linha("Cliente", string.IsNullOrWhiteSpace(ag.ClienteNome) ? "-" : ag.ClienteNome),
                Linha("Telefone", string.IsNullOrWhiteSpace(ag.ClienteTelefone) ? "-" : ag.ClienteTelefone),
                Linha("Data/Hora", ag.DataHora.ToString("dd/MM/yyyy HH:mm")),
                Linha("Duração", ag.DuracaoMinutos + " min"),
                Linha("Situação", ag.StatusDescricao)
            }
        };

        if (servicos.Count > 0)
        {
            stack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0D6F0"), Margin = new Thickness(0, 6) });
            stack.Children.Add(new Label { Text = "Serviços", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = CTexto });
            foreach (var s in servicos)
                stack.Children.Add(Linha(s.ServicoNome, s.Valor.ToString("C")));
        }

        stack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0D6F0"), Margin = new Thickness(0, 6) });
        stack.Children.Add(Linha("Total", ag.ValorLiquido.ToString("C"), true));

        return new Frame { BackgroundColor = Colors.White, CornerRadius = 12, Padding = new Thickness(16), HasShadow = false, Content = stack };
    }

    private static View Linha(string titulo, string valor, bool destaque = false) => new Grid
    {
        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        Children =
        {
            new Label { Text = titulo, FontSize = destaque ? 15 : 13, TextColor = Color.FromArgb("#777777"), FontAttributes = destaque ? FontAttributes.Bold : FontAttributes.None },
            new Label { Text = valor, FontSize = destaque ? 16 : 14, FontAttributes = FontAttributes.Bold, TextColor = destaque ? Color.FromArgb("#2EA043") : Color.FromArgb("#4A148C") }.Column(1)
        }
    };

    private Button BotaoAcao(string texto, Color cor, Func<Task> acao)
    {
        var btn = new Button { Text = texto, BackgroundColor = cor, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 50 };
        btn.Clicked += async (s, e) => await acao();
        return btn;
    }

    private async Task AlterarStatus(string status)
    {
        var nomes = new Dictionary<string, string> { ["C"] = "confirmar", ["F"] = "marcar falta", ["X"] = "cancelar" };
        var acao = nomes.TryGetValue(status, out var n) ? n : "alterar";
        if (!await DisplayAlert("Confirmar", $"Deseja {acao} este atendimento?", "Sim", "Não")) return;
        try
        {
            await SalaoDataService.AtualizarStatusAsync(_id, status);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private async Task FinalizarAsync()
    {
        await Navigation.PushAsync(new FinalizarAtendimentoPage(_ag!, async () => await CarregarAsync()));
    }
}

public static class ViewGridExtensions
{
    public static T Column<T>(this T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
