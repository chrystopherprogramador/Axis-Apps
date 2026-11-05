using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

public class HomePage : ContentPage
{
    private readonly Label _lblProfissional;
    private readonly Label _lblResumo;
    private readonly ActivityIndicator _loading;

    public HomePage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");

        var cRoxo = Color.FromArgb("#6f42c1");
        var cTexto = Color.FromArgb("#4A148C");

        _lblProfissional = new Label { Text = "", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        _lblResumo = new Label { Text = "Carregando...", FontSize = 14, TextColor = cTexto, HorizontalTextAlignment = TextAlignment.Center };
        _loading = new ActivityIndicator { Color = cRoxo, IsRunning = false, HorizontalOptions = LayoutOptions.Center };

        var btnSair = new Button { Text = "Sair", FontSize = 13, BackgroundColor = Colors.Transparent, TextColor = Colors.White, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, WidthRequest = 70 };
        btnSair.Clicked += OnSairClicked;

        var header = new Grid
        {
            BackgroundColor = cRoxo,
            Padding = new Thickness(20, 52, 20, 24),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new StackLayout
                {
                    Children =
                    {
                        new Label { Text = "Axis Salão", FontSize = 15, TextColor = Color.FromArgb("#D1C4E9"), HorizontalTextAlignment = TextAlignment.Center },
                        _lblProfissional
                    }
                }
            }
        };
        Grid.SetColumnSpan(header.Children[0] as View, 2);
        header.Children.Add(btnSair);
        Grid.SetColumn(btnSair, 1);

        var btnAgenda = CriarCard("📅  Minha Agenda", "Ver e gerenciar os atendimentos do dia", cRoxo, async () => await Shell.Current.GoToAsync("AgendaPage"));
        var btnFinanceiro = CriarCard("💰  Financeiro", "Faturamento e comissão do período", Color.FromArgb("#2EA043"), async () => await Shell.Current.GoToAsync("FinanceiroPage"));

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Spacing = 0,
                Children =
                {
                    header,
                    new StackLayout
                    {
                        Padding = new Thickness(20, 24),
                        Spacing = 16,
                        Children =
                        {
                            new Frame
                            {
                                BackgroundColor = Colors.White,
                                BorderColor = Color.FromArgb("#D1C4E9"),
                                CornerRadius = 12,
                                Padding = new Thickness(16),
                                Content = new StackLayout
                                {
                                    Spacing = 6,
                                    Children =
                                    {
                                        new Label { Text = "Resumo de Hoje", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cTexto, HorizontalTextAlignment = TextAlignment.Center },
                                        _lblResumo,
                                        _loading
                                    }
                                }
                            },
                            btnAgenda,
                            btnFinanceiro
                        }
                    }
                }
            }
        };
    }

    private static Frame CriarCard(string titulo, string subtitulo, Color cor, Func<Task> acao)
    {
        var frame = new Frame
        {
            BackgroundColor = cor,
            CornerRadius = 12,
            Padding = new Thickness(20),
            HasShadow = true,
            Content = new StackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = titulo, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = subtitulo, FontSize = 12, TextColor = Color.FromArgb("#F0EAFB") }
                }
            }
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await acao();
        frame.GestureRecognizers.Add(tap);
        return frame;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lblProfissional.Text = AuthService.ProfissionalNome;
        await CarregarResumoAsync();
        await NotificacaoService.IniciarMonitoramentoAsync();
    }

    private async Task CarregarResumoAsync()
    {
        try
        {
            _loading.IsRunning = true;
            var hoje = DateTime.Today;
            var agenda = await SalaoDataService.ListarAgendaAsync(hoje, AuthService.ProfissionalId);
            var atendidos = agenda.Count(a => a.Status == "A");
            var pendentes = agenda.Count(a => a.Status is "M" or "C");
            var faturado = agenda.Where(a => a.Status == "A").Sum(a => a.ValorLiquido);
            _lblResumo.Text = $"{agenda.Count} agendamento(s)  •  {pendentes} pendente(s)  •  {atendidos} atendido(s)\nFaturado hoje: {faturado:C}";
        }
        catch (Exception ex)
        {
            _lblResumo.Text = "Erro ao carregar: " + ex.Message;
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlert("Sair", "Deseja realmente sair?", "Sim", "Não");
        if (!ok) return;
        NotificacaoService.PararMonitoramento();
        AuthService.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
