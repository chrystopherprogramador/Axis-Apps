namespace Axis.App.Salao
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                InitializeComponent();
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
                var page = new Pages.LoadingPage();
                return new Window(page);
            }
            catch (Exception ex)
            {
                return new Window(CriarPaginaErro("CreateWindow", ex));
            }
        }

        public static ContentPage CriarPaginaErro(string origem, Exception ex)
        {
            return new ContentPage
            {
                BackgroundColor = Color.FromArgb("#EDE7F6"),
                Content = new ScrollView
                {
                    Padding = new Thickness(24, 60),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 16,
                        Children =
                        {
                            new Label { Text = "AXIS SALÃO - ERRO", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#C0392B") },
                            new Label { Text = "Origem: " + origem, FontSize = 14, TextColor = Color.FromArgb("#4A148C") },
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
            System.Diagnostics.Debug.WriteLine("[AXIS-SALAO] " + msg);
        }
    }
}
