using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

/// <summary>
/// Formulário dedicado para criar um novo agendamento pelo app.
/// Recebe a data/hora sugerida e um callback para atualizar a agenda ao concluir.
/// </summary>
public class NovoAgendamentoPage : ContentPage
{
    private readonly DateTime _dataSugerida;
    private readonly Func<Task> _aoConcluir;

    private Entry _txtNome = null!;
    private Entry _txtTelefone = null!;
    private DatePicker _dtData = null!;
    private TimePicker _tpHora = null!;
    private Picker _pickerServico = null!;
    private Entry _txtValor = null!;
    private Entry _txtDuracao = null!;
    private Editor _txtObs = null!;
    private Switch _swEncaixe = null!;
    private Label _lblErro = null!;
    private Button _btnSalvar = null!;
    private List<ServicoDto> _servicos = new();

    private static readonly Color CRoxo = Color.FromArgb("#6f42c1");
    private static readonly Color CTexto = Color.FromArgb("#4A148C");
    private static readonly Color CLaranja = Color.FromArgb("#E67E22");

    public NovoAgendamentoPage(DateTime dataSugerida, Func<Task> aoConcluir)
    {
        _dataSugerida = dataSugerida;
        _aoConcluir = aoConcluir;
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");
        BuildUI();
    }

    private void BuildUI()
    {
        _txtNome = new Entry { Placeholder = "Nome do cliente", FontSize = 15, BackgroundColor = Colors.White };
        _txtTelefone = new Entry { Placeholder = "Telefone / WhatsApp", Keyboard = Keyboard.Telephone, FontSize = 15, BackgroundColor = Colors.White };
        _dtData = new DatePicker { Date = _dataSugerida.Date, FontSize = 15, BackgroundColor = Colors.White };
        _tpHora = new TimePicker { Time = _dataSugerida.TimeOfDay, FontSize = 15, BackgroundColor = Colors.White };
        _pickerServico = new Picker { Title = "Selecione o serviço", FontSize = 15, BackgroundColor = Colors.White };
        _pickerServico.SelectedIndexChanged += OnServicoChanged;
        _txtValor = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "0,00", FontSize = 15, BackgroundColor = Colors.White };
        _txtDuracao = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "30", Text = "30", FontSize = 15, BackgroundColor = Colors.White };
        _txtObs = new Editor { HeightRequest = 60, FontSize = 14, BackgroundColor = Colors.White };
        _swEncaixe = new Switch { OnColor = CLaranja };
        _lblErro = new Label { FontSize = 13, TextColor = Color.FromArgb("#C0392B"), IsVisible = false };

        _btnSalvar = new Button { Text = "Salvar Agendamento", BackgroundColor = CRoxo, TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 52 };
        _btnSalvar.Clicked += OnSalvarClicked;

        var btnVoltar = new Button { Text = "‹", FontSize = 26, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = CRoxo,
            Padding = new Thickness(12, 48, 12, 16),
            Children =
            {
                btnVoltar,
                new Label { Text = "Novo Agendamento", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            }
        };

        var encaixeRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new Label { Text = "Marcar como encaixe", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = CLaranja, VerticalOptions = LayoutOptions.Center },
                _swEncaixe
            }
        };
        Grid.SetColumn(_swEncaixe, 1);

        var form = new StackLayout
        {
            Padding = new Thickness(20, 20),
            Spacing = 10,
            Children =
            {
                Rotulo("Cliente"), _txtNome,
                Rotulo("Telefone"), _txtTelefone,
                new Grid
                {
                    ColumnSpacing = 10,
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    Children =
                    {
                        new StackLayout { Children = { Rotulo("Data"), _dtData } },
                        new StackLayout { Children = { Rotulo("Hora"), _tpHora } }.Column(1)
                    }
                },
                Rotulo("Serviço"), _pickerServico,
                new Grid
                {
                    ColumnSpacing = 10,
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    Children =
                    {
                        new StackLayout { Children = { Rotulo("Valor (R$)"), _txtValor } },
                        new StackLayout { Children = { Rotulo("Duração (min)"), _txtDuracao } }.Column(1)
                    }
                },
                encaixeRow,
                Rotulo("Observação"), _txtObs,
                _lblErro,
                _btnSalvar
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children = { header, new ScrollView { Content = form } }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    private static Label Rotulo(string t) => new() { Text = t, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = CTexto };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _servicos = await SalaoDataService.ListarServicosAsync();
            _pickerServico.ItemsSource = _servicos.Select(s => s.Nome).ToList();
        }
        catch (Exception ex)
        {
            _lblErro.Text = "Erro ao carregar serviços: " + ex.Message;
            _lblErro.IsVisible = true;
        }
    }

    private void OnServicoChanged(object? sender, EventArgs e)
    {
        if (_pickerServico.SelectedIndex < 0 || _pickerServico.SelectedIndex >= _servicos.Count) return;
        var s = _servicos[_pickerServico.SelectedIndex];
        _txtValor.Text = s.Valor.ToString("0.00");
        _txtDuracao.Text = s.DuracaoMinutos.ToString();
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        _lblErro.IsVisible = false;

        if (string.IsNullOrWhiteSpace(_txtNome.Text))
        {
            MostrarErro("Informe o nome do cliente.");
            return;
        }

        decimal.TryParse(_txtValor.Text?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valor);
        int.TryParse(_txtDuracao.Text, out var duracao);
        if (duracao <= 0) duracao = 30;

        var dataHora = _dtData.Date.Date.Add(_tpHora.Time);

        List<ServicoDto> servicos = new();
        if (_pickerServico.SelectedIndex >= 0 && _pickerServico.SelectedIndex < _servicos.Count)
        {
            var sel = _servicos[_pickerServico.SelectedIndex];
            servicos.Add(new ServicoDto { Codigo = sel.Codigo, Nome = sel.Nome, Valor = valor, DuracaoMinutos = duracao });
        }

        _btnSalvar.IsEnabled = false;
        try
        {
            var obs = _txtObs.Text ?? "";
            if (_swEncaixe.IsToggled)
                obs = string.IsNullOrWhiteSpace(obs) ? "[ENCAIXE]" : "[ENCAIXE] " + obs;

            await SalaoDataService.CriarAgendamentoAsync(
                _txtNome.Text.Trim(), _txtTelefone.Text?.Trim() ?? "",
                AuthService.ProfissionalId, dataHora, duracao, valor, obs, servicos);

            await DisplayAlert("Sucesso", "Agendamento criado!", "OK");
            await _aoConcluir();
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            MostrarErro("Erro: " + ex.Message);
            _btnSalvar.IsEnabled = true;
        }
    }

    private void MostrarErro(string msg)
    {
        _lblErro.Text = msg;
        _lblErro.IsVisible = true;
    }
}
