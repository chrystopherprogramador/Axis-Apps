using Axis.App.RFID.Services;

namespace Axis.App.RFID.Pages;

/// <summary>Selecao de empresa quando usuario tem mais de uma.</summary>
public class EmpresaSelectPage : ContentPage
{
    private readonly List<(long id, string nome)> _empresas;

    public EmpresaSelectPage(List<(long id, string nome)> empresas)
    {
        _empresas = empresas;
        BuildUI();
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");

        var header = new StackLayout
        {
            BackgroundColor = cP, Padding = new Thickness(20, 52, 20, 20),
            Children =
            {
                new Label { Text = "Selecione a Empresa", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = AuthService.UsuarioNome, FontSize = 13, TextColor = Color.FromArgb("#90A4AE"), HorizontalTextAlignment = TextAlignment.Center }
            }
        };

        var stack = new StackLayout { Spacing = 10, Padding = new Thickness(20, 16) };
        foreach (var emp in _empresas)
        {
            var btn = new Button
            {
                Text = emp.nome, FontSize = 16, BackgroundColor = Colors.White,
                TextColor = cP, CornerRadius = 10, HeightRequest = 56,
                BorderColor = Color.FromArgb("#E0E6ED"), BorderWidth = 1
            };
            var id = emp.id;
            btn.Clicked += async (s, e) =>
            {
                ConfigService.EmpresaId = id;
                await Shell.Current.GoToAsync("//HomePage");
            };
            stack.Children.Add(btn);
        }

        Content = new StackLayout { Children = { header, new ScrollView { Content = stack } } };
    }
}
