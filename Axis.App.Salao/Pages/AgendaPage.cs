using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

/// <summary>
/// Agenda estilo Google Calendar: calendário mensal (com os dias que possuem
/// atendimento destacados) + lista dos atendimentos do dia selecionado.
/// Suporta encaixe (2+ no mesmo horário) com cores diferentes.
/// </summary>
public class AgendaPage : ContentPage
{
    private DateTime _dia = DateTime.Today;
    private DateTime _mesExibido = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _modoCalendario = true;

    private readonly Label _lblData;
    private readonly Label _lblMes;
    private readonly ActivityIndicator _loading;
    private readonly StackLayout _listaStack;
    private readonly Label _lblVazio;
    private readonly Grid _calendarioGrid;
    private readonly StackLayout _navMes;
    private readonly Button _btnAlternar;
    private HashSet<int> _diasComAgenda = new();

    private static readonly Color CRoxo = Color.FromArgb("#6f42c1");
    private static readonly Color CTexto = Color.FromArgb("#4A148C");
    private static readonly Color CLaranja = Color.FromArgb("#E67E22");

    public AgendaPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");
        Title = "Agenda";

        _lblData = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        _lblMes = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = CTexto, HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center };
        _loading = new ActivityIndicator { Color = CRoxo, IsRunning = false, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20) };
        _listaStack = new StackLayout { Spacing = 10 };
        _lblVazio = new Label { Text = "Nenhum atendimento neste dia.", FontSize = 14, TextColor = Color.FromArgb("#888888"), HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 30), IsVisible = false };
        _calendarioGrid = new Grid { ColumnSpacing = 2, RowSpacing = 2, Padding = new Thickness(12, 8) };

        var btnVoltar = new Button { Text = "‹", FontSize = 26, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Shell.Current.GoToAsync("//HomePage");

        _btnAlternar = new Button { Text = "☰ Lista", FontSize = 13, BackgroundColor = Colors.Transparent, TextColor = Colors.White, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, WidthRequest = 90 };
        _btnAlternar.Clicked += async (s, e) => { _modoCalendario = !_modoCalendario; _btnAlternar.Text = _modoCalendario ? "☰ Lista" : "▦ Mês"; await CarregarAsync(); };

        var header = new Grid
        {
            BackgroundColor = CRoxo,
            Padding = new Thickness(12, 48, 12, 12),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children = { btnVoltar, _lblData, _btnAlternar }
        };
        Grid.SetColumn(_lblData, 1);
        Grid.SetColumn(_btnAlternar, 2);

        var btnMesAnt = new Button { Text = "◀", FontSize = 16, BackgroundColor = Color.FromArgb("#8155CB"), TextColor = Colors.White, CornerRadius = 8, WidthRequest = 48, HeightRequest = 40 };
        btnMesAnt.Clicked += async (s, e) => { _mesExibido = _mesExibido.AddMonths(-1); await CarregarAsync(); };
        var btnMesProx = new Button { Text = "▶", FontSize = 16, BackgroundColor = Color.FromArgb("#8155CB"), TextColor = Colors.White, CornerRadius = 8, WidthRequest = 48, HeightRequest = 40 };
        btnMesProx.Clicked += async (s, e) => { _mesExibido = _mesExibido.AddMonths(1); await CarregarAsync(); };

        var navMesGrid = new Grid
        {
            Padding = new Thickness(12, 10),
            BackgroundColor = Colors.White,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children = { btnMesAnt, _lblMes, btnMesProx }
        };
        Grid.SetColumn(_lblMes, 1);
        Grid.SetColumn(btnMesProx, 2);
        _navMes = new StackLayout { Children = { navMesGrid } };

        var btnNovo = new Button { Text = "＋ Novo agendamento", BackgroundColor = CRoxo, TextColor = Colors.White, FontSize = 15, FontAttributes = FontAttributes.Bold, CornerRadius = 12, HeightRequest = 50, Margin = new Thickness(16, 8, 16, 16) };
        btnNovo.Clicked += OnNovoClicked;

        var corpo = new StackLayout
        {
            Children =
            {
                _navMes,
                _calendarioGrid,
                new StackLayout { Padding = new Thickness(16, 8), Children = { _loading, _lblVazio, _listaStack } }
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Children = { header, new ScrollView { Content = corpo }, btnNovo }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
        Grid.SetRow(btnNovo, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async void OnNovoClicked(object? sender, EventArgs e)
    {
        DateTime sugestao;
        try
        {
            sugestao = await SalaoDataService.ProximoHorarioDisponivelAsync(_dia, AuthService.ProfissionalId);
        }
        catch
        {
            sugestao = _dia.Date.AddHours(9);
        }
        await Navigation.PushAsync(new NovoAgendamentoPage(sugestao, CarregarAsync));
    }

    private async Task CarregarAsync()
    {
        _lblData.Text = _dia.ToString("dddd, dd/MM/yyyy");
        _lblMes.Text = _mesExibido.ToString("MMMM 'de' yyyy");
        _loading.IsRunning = true;
        _listaStack.Children.Clear();
        _lblVazio.IsVisible = false;
        _calendarioGrid.IsVisible = _modoCalendario;
        _navMes.IsVisible = _modoCalendario;

        try
        {
            if (_modoCalendario)
            {
                _diasComAgenda = await SalaoDataService.ListarDiasComAgendamentoAsync(_mesExibido.Year, _mesExibido.Month, AuthService.ProfissionalId);
                MontarCalendario();
            }

            var lista = await SalaoDataService.ListarAgendaAsync(_dia, AuthService.ProfissionalId);
            MarcarEncaixes(lista);

            if (lista.Count == 0)
            {
                _lblVazio.IsVisible = true;
                return;
            }
            foreach (var ag in lista)
                _listaStack.Children.Add(CriarCard(ag));
        }
        catch (Exception ex)
        {
            _lblVazio.Text = "Erro: " + ex.Message;
            _lblVazio.IsVisible = true;
        }
        finally
        {
            _loading.IsRunning = false;
        }
    }

    /// <summary>Marca como encaixe o 2º+ agendamento que compartilha o mesmo horário, ou os marcados manualmente.</summary>
    private static void MarcarEncaixes(List<AgendamentoDto> lista)
    {
        var vistos = new HashSet<string>();
        foreach (var ag in lista.OrderBy(a => a.DataHora).ThenBy(a => a.Codigo))
        {
            var chave = ag.DataHora.ToString("HH:mm");
            bool encaixeManual = ag.Observacao?.Contains("[ENCAIXE]", StringComparison.OrdinalIgnoreCase) == true;
            if (!vistos.Add(chave) || encaixeManual)
                ag.EhEncaixe = true;
        }
    }

    private void MontarCalendario()
    {
        _calendarioGrid.Children.Clear();
        _calendarioGrid.ColumnDefinitions.Clear();
        _calendarioGrid.RowDefinitions.Clear();

        for (int c = 0; c < 7; c++)
            _calendarioGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        string[] cab = { "D", "S", "T", "Q", "Q", "S", "S" };
        _calendarioGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int c = 0; c < 7; c++)
        {
            var lbl = new Label { Text = cab[c], FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = CRoxo, HorizontalTextAlignment = TextAlignment.Center };
            _calendarioGrid.Add(lbl, c, 0);
        }

        var primeiro = new DateTime(_mesExibido.Year, _mesExibido.Month, 1);
        int diasNoMes = DateTime.DaysInMonth(_mesExibido.Year, _mesExibido.Month);
        int offset = (int)primeiro.DayOfWeek;

        int linha = 1;
        int col = offset;
        _calendarioGrid.RowDefinitions.Add(new RowDefinition(new GridLength(46)));

        for (int dia = 1; dia <= diasNoMes; dia++)
        {
            var dataCel = new DateTime(_mesExibido.Year, _mesExibido.Month, dia);
            bool temAgenda = _diasComAgenda.Contains(dia);
            bool selecionado = dataCel.Date == _dia.Date;
            bool hoje = dataCel.Date == DateTime.Today;

            var cell = CriarCelulaDia(dia, temAgenda, selecionado, hoje, dataCel);
            _calendarioGrid.Add(cell, col, linha);

            col++;
            if (col > 6)
            {
                col = 0;
                linha++;
                _calendarioGrid.RowDefinitions.Add(new RowDefinition(new GridLength(46)));
            }
        }
    }

    private View CriarCelulaDia(int dia, bool temAgenda, bool selecionado, bool hoje, DateTime data)
    {
        var lbl = new Label
        {
            Text = dia.ToString(),
            FontSize = 14,
            FontAttributes = (selecionado || hoje) ? FontAttributes.Bold : FontAttributes.None,
            TextColor = selecionado ? Colors.White : CTexto,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var marcador = new BoxView
        {
            WidthRequest = 6, HeightRequest = 6, CornerRadius = 3,
            Color = temAgenda ? (selecionado ? Colors.White : CRoxo) : Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var frame = new Frame
        {
            Padding = 0,
            CornerRadius = 8,
            HasShadow = false,
            BackgroundColor = selecionado ? CRoxo : (hoje ? Color.FromArgb("#E0D6F0") : Colors.White),
            BorderColor = hoje && !selecionado ? CRoxo : Colors.Transparent,
            Content = new StackLayout
            {
                Spacing = 0,
                VerticalOptions = LayoutOptions.Center,
                Children = { lbl, marcador }
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => { _dia = data; await CarregarAsync(); };
        frame.GestureRecognizers.Add(tap);
        return frame;
    }

    private Frame CriarCard(AgendamentoDto ag)
    {
        var corBorda = ag.EhEncaixe ? CLaranja : CorStatus(ag.Status);
        var rotuloTipo = ag.EhEncaixe ? "ENCAIXE" : "CLIENTE";
        var corTipo = ag.EhEncaixe ? CLaranja : CRoxo;

        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = corBorda,
            CornerRadius = 12,
            Padding = new Thickness(14),
            HasShadow = false,
            Content = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 12,
                Children =
                {
                    new StackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = ag.HoraTexto, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = corTipo },
                            new Label { Text = rotuloTipo, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = corTipo }
                        }
                    },
                    new StackLayout
                    {
                        Spacing = 2,
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = string.IsNullOrWhiteSpace(ag.ClienteNome) ? "Cliente" : ag.ClienteNome, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = CTexto },
                            new Label { Text = ag.ValorLiquido > 0 ? ag.ValorLiquido.ToString("C") : $"{ag.DuracaoMinutos} min", FontSize = 12, TextColor = Color.FromArgb("#777777") }
                        }
                    },
                    CriarBadge(ag.StatusDescricao, CorStatus(ag.Status))
                }
            }
        };
        Grid.SetColumn((frame.Content as Grid)!.Children[0] as View, 0);
        Grid.SetColumn((frame.Content as Grid)!.Children[1] as View, 1);
        Grid.SetColumn((frame.Content as Grid)!.Children[2] as View, 2);

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await Shell.Current.GoToAsync($"AgendamentoDetalhePage?id={ag.Codigo}");
        frame.GestureRecognizers.Add(tap);
        return frame;
    }

    private static Frame CriarBadge(string texto, Color cor) => new()
    {
        BackgroundColor = cor,
        CornerRadius = 10,
        Padding = new Thickness(8, 4),
        HasShadow = false,
        VerticalOptions = LayoutOptions.Center,
        Content = new Label { Text = texto, FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
    };

    private static Color CorStatus(string status) => status switch
    {
        "M" => Color.FromArgb("#F39C12"),
        "C" => Color.FromArgb("#2980B9"),
        "A" => Color.FromArgb("#2EA043"),
        "X" => Color.FromArgb("#C0392B"),
        "F" => Color.FromArgb("#7F8C8D"),
        _ => Color.FromArgb("#6f42c1")
    };
}
