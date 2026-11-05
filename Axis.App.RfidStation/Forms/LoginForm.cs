using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Forms;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
    }

    private async void btnEntrar_Click(object sender, EventArgs e)
    {
        await FazerLoginAsync();
    }

    private async Task FazerLoginAsync()
    {
        btnEntrar.Enabled = false;
        lblStatus.ForeColor = Color.DimGray;
        try
        {
            var subdominio = txtSubdominio.Text.Trim();
            var usuario = txtUsuario.Text.Trim();
            var senha = txtSenha.Text;

            if (string.IsNullOrWhiteSpace(subdominio))
            {
                MostrarErro("Informe o subdominio.");
                txtSubdominio.Focus();
                return;
            }

            lblStatus.Text = "Resolvendo tenant...";
            var (okTenant, msgTenant, cs, nome) =
                await TenantResolver.ResolverAsync(subdominio, AppConfig.Atual.MasterConnectionString);
            if (!okTenant || cs is null)
            {
                MostrarErro(msgTenant);
                return;
            }

            AppConfig.Atual.Subdominio = subdominio;
            AppConfig.Atual.TenantConnectionString = cs;

            lblStatus.Text = "Autenticando...";
            var (okLogin, msgLogin) = await AuthService.LoginAsync(usuario, senha);
            if (!okLogin)
            {
                MostrarErro(msgLogin);
                return;
            }

            // Dados da ponte com o Axis Web
            AppConfig.Atual.UsuarioId = AuthService.UsuarioId;
            AppConfig.Atual.UsuarioNome = AuthService.UsuarioNome;
            AppConfig.Atual.ServerUrl = ResolverServerUrl(subdominio);
            AppConfig.Atual.Token = GerarToken(subdominio, AuthService.UsuarioId);
            AppConfig.Atual.Salvar();

            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = msgLogin;

            var estacao = new EstacaoForm();
            Hide();
            estacao.FormClosed += (_, _) => Close();
            estacao.Show();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro: {ex.Message}");
        }
        finally
        {
            btnEntrar.Enabled = true;
        }
    }

    private void MostrarErro(string msg)
    {
        lblStatus.ForeColor = Color.Firebrick;
        lblStatus.Text = msg;
    }

    private static string ResolverServerUrl(string subdominio)
    {
        var template = LerServerUrlTemplate();
        return template.Replace("{subdominio}", subdominio.Trim().ToLowerInvariant());
    }

    private static string LerServerUrlTemplate()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(caminho))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(caminho));
                if (doc.RootElement.TryGetProperty("ServerUrlTemplate", out var v))
                    return v.GetString() ?? "https://{subdominio}.axis.app.br";
            }
        }
        catch { }
        return "https://{subdominio}.axis.app.br";
    }

    private static string GerarToken(string subdominio, long usuarioId)
    {
        // Token opaco de sessao da ponte (identifica subdominio + usuario + maquina).
        var raw = $"{subdominio}|{usuarioId}|{Environment.MachineName}|{Guid.NewGuid():N}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private void LoginForm_Load(object sender, EventArgs e)
    {
        txtSubdominio.Text = AppConfig.Atual.Subdominio;
        if (!string.IsNullOrWhiteSpace(txtSubdominio.Text))
            txtUsuario.Focus();
    }
}
