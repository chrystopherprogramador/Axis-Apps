using Axis.App.Salao.Services;

namespace Axis.App.Salao.Pages;

public class FinalizarAtendimentoPage : ContentPage
{
    private readonly AgendamentoDto _ag;
    private readonly Func<Task> _aoConcluir;

    private Entry _txtTotal = null!;
    private Entry _txtDesconto = null!;
    private Editor _txtObs = null!;
    private Picker _pickerForma = null!;
    private Label _lblErro = null!;
    private Button _btnConfirmar = null!;
    private List<FormaPagamentoDto> _formas = new();

    private static readonly Color CRoxo = Color.FromArgb("#6f42c1");
    private static readonly Color CTexto = Color.FromArgb("#4A148C");

    public FinalizarAtendimentoPage(AgendamentoDto ag, Func<Task> aoConcluir)
    {
        _ag = ag;
        _aoConcluir = aoConcluir;
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#EDE7F6");
        BuildUI();
    }

    private void BuildUI()
    {
        _txtTotal = new Entry { Keyboard = Keyboard.Numeric, FontSize = 15, Text = _ag.ValorTotal > 0 ? _ag.ValorTotal.ToString("0.00") : "", Placeholder = "0,00", BackgroundColor = Colors.White };
        _txtDesconto = new Entry { Keyboard = Keyboard.Numeric, FontSize = 15, Text = _ag.ValorDesconto > 0 ? _ag.ValorDesconto.ToString("0.00") : "", Placeholder = "0,00", BackgroundColor = Colors.White };
        _txtObs = new Editor { FontSize = 14, HeightRequest = 70, Text = _ag.Observacao, BackgroundColor = Colors.White };
        _pickerForma = new Picker { Title = "Selecione a forma de pagamento", FontSize = 15, BackgroundColor = Colors.White };
        _lblErro = new Label { FontSize = 13, TextColor = Color.FromArgb("#C0392B"), IsVisible = false };

        _btnConfirmar = new Button { Text = "Confirmar Pagamento", BackgroundColor = Color.FromArgb("#2EA043"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 52 };
        _btnConfirmar.Clicked += OnConfirmarClicked;

        var btnVoltar = new Button { Text = "‹", FontSize = 26, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = CRoxo,
            Padding = new Thickness(12, 48, 12, 16),
            Children =
            {
                btnVoltar,
                new Label { Text = "Finalizar Atendimento", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            }
        };

        var form = new StackLayout
        {
            Padding = new Thickness(20, 20),
            Spacing = 12,
            Children =
            {
                new Label { Text = "Cliente: " + (string.IsNullOrWhiteSpace(_ag.ClienteNome) ? "-" : _ag.ClienteNome), FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = CTexto },
                Rotulo("Valor Total (R$)"), _txtTotal,
                Rotulo("Desconto (R$)"), _txtDesconto,
                Rotulo("Forma de Pagamento"), _pickerForma,
                Rotulo("Observação"), _txtObs,
                _lblErro,
                _btnConfirmar
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
            _formas = await SalaoDataService.ListarFormasPagamentoAsync();
            _pickerForma.ItemsSource = _formas.Select(f => f.Nome).ToList();
        }
        catch (Exception ex)
        {
            _lblErro.Text = "Erro ao carregar formas de pagamento: " + ex.Message;
            _lblErro.IsVisible = true;
        }
    }

    private async void OnConfirmarClicked(object? sender, EventArgs e)
    {
        _lblErro.IsVisible = false;

        if (!decimal.TryParse(_txtTotal.Text?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var total) || total <= 0)
        {
            _lblErro.Text = "Informe um valor total válido.";
            _lblErro.IsVisible = true;
            return;
        }
        decimal.TryParse(_txtDesconto.Text?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var desconto);

        if (_pickerForma.SelectedIndex < 0)
        {
            _lblErro.Text = "Selecione a forma de pagamento.";
            _lblErro.IsVisible = true;
            return;
        }
        var formaId = _formas[_pickerForma.SelectedIndex].Codigo;

        _btnConfirmar.IsEnabled = false;
        try
        {
            await SalaoDataService.FecharAtendimentoAsync(_ag.Codigo, total, desconto, formaId, _txtObs.Text ?? "");
            await DisplayAlert("Sucesso", "Atendimento finalizado!", "OK");
            await _aoConcluir();
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            _lblErro.Text = "Erro: " + ex.Message;
            _lblErro.IsVisible = true;
            _btnConfirmar.IsEnabled = true;
        }
    }
}
