using Axis.App.Vendedor.Services;

namespace Axis.App.Vendedor.Pages;

public class CadastrarClientePage : ContentPage
{
    private readonly Action<ClienteCadastroResult>? _onSalvo;
    private Entry _txtNome = null!, _txtDoc = null!, _txtCelular = null!, _txtTelefone = null!, _txtEmail = null!;
    private Entry _txtCep = null!, _txtLogradouro = null!, _txtNumero = null!, _txtComplemento = null!, _txtBairro = null!, _txtCidade = null!, _txtUf = null!, _txtIbge = null!;
    private Label _lblCepStatus = null!;

    public CadastrarClientePage(Action<ClienteCadastroResult>? onSalvo = null)
    {
        _onSalvo = onSalvo;
        BackgroundColor = Color.FromArgb("#D6E8FF");

        var cAzul = Color.FromArgb("#2A66B8");
        var cTexto = Color.FromArgb("#274176");

        _txtNome = CriarEntry("Nome / Razão Social *");
        _txtDoc = CriarEntry("CPF ou CNPJ", Keyboard.Numeric);
        _txtCelular = CriarEntry("Celular", Keyboard.Telephone);
        _txtTelefone = CriarEntry("Telefone", Keyboard.Telephone);
        _txtEmail = CriarEntry("E-mail", Keyboard.Email);

        _txtCep = CriarEntry("CEP", Keyboard.Numeric);
        _txtCep.Completed += async (s, e) => await BuscarCepAsync();
        _lblCepStatus = new Label { FontSize = 11, TextColor = Color.FromArgb("#888"), IsVisible = false };
        _txtLogradouro = CriarEntry("Logradouro");
        _txtNumero = CriarEntry("Número");
        _txtComplemento = CriarEntry("Complemento");
        _txtBairro = CriarEntry("Bairro");
        _txtCidade = CriarEntry("Cidade");
        _txtUf = CriarEntry("UF", maxLength: 2);
        _txtIbge = CriarEntry("Cód. IBGE", isReadOnly: true);

        var btnBuscarCep = new Button { Text = "Buscar CEP", BackgroundColor = cAzul, TextColor = Colors.White, FontSize = 13, CornerRadius = 6, HeightRequest = 38 };
        btnBuscarCep.Clicked += async (s, e) => await BuscarCepAsync();

        var btnSalvar = new Button { Text = "Salvar Cliente", BackgroundColor = Color.FromArgb("#2EA043"), TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 50 };
        btnSalvar.Clicked += OnSalvarClicked;

        // Montar grid CEP com botão
        var cepBorder = CriarBordaEntry(_txtCep);
        var cepGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
        Grid.SetColumn(cepBorder, 0);
        Grid.SetColumn(btnBuscarCep, 1);
        cepGrid.Children.Add(cepBorder);
        cepGrid.Children.Add(btnBuscarCep);

        // Montar grid Cidade + UF
        var cidadeBorder = CriarBordaEntry(_txtCidade);
        var ufBorder = CriarBordaEntry(_txtUf);
        var cidadeGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(80)) }, ColumnSpacing = 8 };
        Grid.SetColumn(cidadeBorder, 0);
        Grid.SetColumn(ufBorder, 1);
        cidadeGrid.Children.Add(cidadeBorder);
        cidadeGrid.Children.Add(ufBorder);

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Spacing = 0,
                Children =
                {
                    new StackLayout { BackgroundColor = cAzul, Padding = new Thickness(20, 50, 20, 16), Children = {
                        new Label { Text = "Cadastrar Cliente", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center }
                    }},
                    new StackLayout
                    {
                        Padding = new Thickness(20, 16), Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Dados Básicos", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                            CriarBordaEntry(_txtNome),
                            CriarBordaEntry(_txtDoc),
                            CriarBordaEntry(_txtCelular),
                            CriarBordaEntry(_txtTelefone),
                            CriarBordaEntry(_txtEmail),
                            new BoxView { HeightRequest = 1, Color = Color.FromArgb("#BFD7FF"), Margin = new Thickness(0, 6) },
                            new Label { Text = "Endereço", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = cTexto },
                            cepGrid,
                            _lblCepStatus,
                            CriarBordaEntry(_txtLogradouro),
                            CriarBordaEntry(_txtNumero),
                            CriarBordaEntry(_txtComplemento),
                            CriarBordaEntry(_txtBairro),
                            cidadeGrid,
                            CriarBordaEntry(_txtIbge),
                            new BoxView { HeightRequest = 1, Color = Color.FromArgb("#BFD7FF"), Margin = new Thickness(0, 6) },
                            btnSalvar
                        }
                    }
                }
            }
        };
    }

    private async Task BuscarCepAsync()
    {
        var cep = _txtCep.Text?.Trim() ?? "";
        if (cep.Length < 8) return;

        _lblCepStatus.Text = "Buscando CEP..."; _lblCepStatus.IsVisible = true;

        var result = await DataService.BuscarCepAsync(cep);
        if (result != null)
        {
            _txtLogradouro.Text = result.Logradouro;
            _txtBairro.Text = result.Bairro;
            _txtCidade.Text = result.Cidade;
            _txtUf.Text = result.Uf;
            _txtIbge.Text = result.CodigoIbge;
            _lblCepStatus.Text = "CEP encontrado!"; _lblCepStatus.TextColor = Color.FromArgb("#2EA043");
        }
        else
        {
            _lblCepStatus.Text = "CEP nao encontrado"; _lblCepStatus.TextColor = Color.FromArgb("#C0392B");
        }
    }

    private static Entry CriarEntry(string placeholder, Keyboard? keyboard = null, bool isReadOnly = false, int maxLength = 0)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            FontSize = 14,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#1A1A2E"),
            PlaceholderColor = Color.FromArgb("#8E8E93"),
        };
        if (keyboard != null) entry.Keyboard = keyboard;
        if (isReadOnly) entry.IsReadOnly = true;
        if (maxLength > 0) entry.MaxLength = maxLength;
        return entry;
    }

    private static Border CriarBordaEntry(Entry entry)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#C8D6E5"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 2),
            Content = entry
        };
    }

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        var nome = _txtNome.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nome))
        {
            await DisplayAlert("Aviso", "Nome e obrigatorio!", "OK");
            return;
        }

        try
        {
            var doc = _txtDoc.Text?.Trim() ?? "";
            var tipoPessoa = doc.Length > 11 ? 1 : 0;

            var cli = new ClienteCadastroDto
            {
                TipoPessoa = tipoPessoa,
                NomeRazao = nome,
                Documento = doc,
                Celular = _txtCelular.Text?.Trim(),
                Telefone = _txtTelefone.Text?.Trim(),
                Email = _txtEmail.Text?.Trim(),
                Cep = _txtCep.Text?.Trim(),
                Logradouro = _txtLogradouro.Text?.Trim(),
                Numero = _txtNumero.Text?.Trim(),
                Complemento = _txtComplemento.Text?.Trim(),
                Bairro = _txtBairro.Text?.Trim(),
                Cidade = _txtCidade.Text?.Trim(),
                CodigoIbge = _txtIbge.Text?.Trim(),
                Uf = _txtUf.Text?.Trim()
            };

            var id = await DataService.InserirClienteAsync(cli);
            await DisplayAlert("Sucesso", "Cliente cadastrado! Cod: " + id, "OK");

            _onSalvo?.Invoke(new ClienteCadastroResult(id, nome, doc));
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", "Erro ao salvar: " + ex.Message, "OK");
        }
    }
}
