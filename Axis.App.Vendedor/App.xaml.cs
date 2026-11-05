using Microsoft.Extensions.DependencyInjection;

namespace Axis.App.Vendedor
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                Log("App constructor INICIO");
                InitializeComponent();
                Log("App InitializeComponent OK");
            }
            catch (Exception ex)
            {
                Log("App constructor ERRO: " + ex.Message);
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                Log("CreateWindow INICIO");
                var page = new Pages.LoadingPage();
                Log("LoadingPage criada");
                var window = new Window(page);
                Log("Window criada OK");
                return window;
            }
            catch (Exception ex)
            {
                Log("CreateWindow ERRO: " + ex.Message);
                return new Window(CriarPaginaErro("CreateWindow", ex));
            }
        }

        public static ContentPage CriarPaginaErro(string origem, Exception ex)
        {
            return new ContentPage
            {
                BackgroundColor = Color.FromArgb("#D6E8FF"),
                Content = new ScrollView
                {
                    Padding = new Thickness(24, 60),
                    Content = new StackLayout
                    {
                        Spacing = 16,
                        Children =
                        {
                            new Label { Text = "AXIS - ERRO", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#C0392B") },
                            new Label { Text = "Origem: " + origem, FontSize = 14, TextColor = Color.FromArgb("#274176") },
                            new Label { Text = ex.ToString(), FontSize = 10, TextColor = Color.FromArgb("#333333") }
                        }
                    }
                }
            };
        }

        private static void Log(string msg)
        {
#if ANDROID
            MainApplication.GravarLog(msg);
#endif
            System.Diagnostics.Debug.WriteLine("[AXIS] " + msg);
        }
    }
}