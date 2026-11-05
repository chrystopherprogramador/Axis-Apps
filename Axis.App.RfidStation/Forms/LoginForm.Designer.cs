namespace Axis.App.RfidStation.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblTitulo;
    private Label lblSubdominio;
    private TextBox txtSubdominio;
    private Label lblSufixo;
    private Label lblUsuario;
    private TextBox txtUsuario;
    private Label lblSenha;
    private TextBox txtSenha;
    private Button btnEntrar;
    private Label lblStatus;
    private Panel pnlCard;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlCard = new Panel();
        lblTitulo = new Label();
        lblSubdominio = new Label();
        txtSubdominio = new TextBox();
        lblSufixo = new Label();
        lblUsuario = new Label();
        txtUsuario = new TextBox();
        lblSenha = new Label();
        txtSenha = new TextBox();
        btnEntrar = new Button();
        lblStatus = new Label();
        pnlCard.SuspendLayout();
        SuspendLayout();
        // 
        // pnlCard
        // 
        pnlCard.BackColor = Color.White;
        pnlCard.Controls.Add(lblTitulo);
        pnlCard.Controls.Add(lblSubdominio);
        pnlCard.Controls.Add(txtSubdominio);
        pnlCard.Controls.Add(lblSufixo);
        pnlCard.Controls.Add(lblUsuario);
        pnlCard.Controls.Add(txtUsuario);
        pnlCard.Controls.Add(lblSenha);
        pnlCard.Controls.Add(txtSenha);
        pnlCard.Controls.Add(btnEntrar);
        pnlCard.Controls.Add(lblStatus);
        pnlCard.Location = new Point(40, 30);
        pnlCard.Name = "pnlCard";
        pnlCard.Size = new Size(360, 380);
        pnlCard.TabIndex = 0;
        // 
        // lblTitulo
        // 
        lblTitulo.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        lblTitulo.ForeColor = Color.FromArgb(0, 90, 158);
        lblTitulo.Location = new Point(24, 24);
        lblTitulo.Name = "lblTitulo";
        lblTitulo.Size = new Size(312, 40);
        lblTitulo.TabIndex = 0;
        lblTitulo.Text = "Axis RFID Station";
        lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblSubdominio
        // 
        lblSubdominio.AutoSize = true;
        lblSubdominio.Location = new Point(24, 80);
        lblSubdominio.Name = "lblSubdominio";
        lblSubdominio.Size = new Size(72, 15);
        lblSubdominio.TabIndex = 1;
        lblSubdominio.Text = "Subdominio";
        // 
        // txtSubdominio
        // 
        txtSubdominio.Location = new Point(24, 100);
        txtSubdominio.Name = "txtSubdominio";
        txtSubdominio.PlaceholderText = "ex.: minhaempresa";
        txtSubdominio.Size = new Size(210, 23);
        txtSubdominio.TabIndex = 2;
        // 
        // lblSufixo
        // 
        lblSufixo.AutoSize = true;
        lblSufixo.ForeColor = Color.Gray;
        lblSufixo.Location = new Point(240, 103);
        lblSufixo.Name = "lblSufixo";
        lblSufixo.Size = new Size(70, 15);
        lblSufixo.TabIndex = 3;
        lblSufixo.Text = ".axis.app.br";
        // 
        // lblUsuario
        // 
        lblUsuario.AutoSize = true;
        lblUsuario.Location = new Point(24, 140);
        lblUsuario.Name = "lblUsuario";
        lblUsuario.Size = new Size(49, 15);
        lblUsuario.TabIndex = 4;
        lblUsuario.Text = "Usuario";
        // 
        // txtUsuario
        // 
        txtUsuario.Location = new Point(24, 160);
        txtUsuario.Name = "txtUsuario";
        txtUsuario.Size = new Size(312, 23);
        txtUsuario.TabIndex = 5;
        // 
        // lblSenha
        // 
        lblSenha.AutoSize = true;
        lblSenha.Location = new Point(24, 200);
        lblSenha.Name = "lblSenha";
        lblSenha.Size = new Size(39, 15);
        lblSenha.TabIndex = 6;
        lblSenha.Text = "Senha";
        // 
        // txtSenha
        // 
        txtSenha.Location = new Point(24, 220);
        txtSenha.Name = "txtSenha";
        txtSenha.PasswordChar = '•';
        txtSenha.Size = new Size(312, 23);
        txtSenha.TabIndex = 7;
        // 
        // btnEntrar
        // 
        btnEntrar.BackColor = Color.FromArgb(0, 120, 215);
        btnEntrar.FlatStyle = FlatStyle.Flat;
        btnEntrar.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnEntrar.ForeColor = Color.White;
        btnEntrar.Location = new Point(24, 265);
        btnEntrar.Name = "btnEntrar";
        btnEntrar.Size = new Size(312, 40);
        btnEntrar.TabIndex = 8;
        btnEntrar.Text = "Entrar";
        btnEntrar.UseVisualStyleBackColor = false;
        btnEntrar.Click += btnEntrar_Click;
        // 
        // lblStatus
        // 
        lblStatus.ForeColor = Color.DimGray;
        lblStatus.Location = new Point(24, 315);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(312, 50);
        lblStatus.TabIndex = 9;
        lblStatus.TextAlign = ContentAlignment.TopCenter;
        // 
        // LoginForm
        // 
        AcceptButton = btnEntrar;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(240, 242, 245);
        ClientSize = new Size(440, 440);
        Controls.Add(pnlCard);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Axis RFID Station - Login";
        Load += LoginForm_Load;
        pnlCard.ResumeLayout(false);
        pnlCard.PerformLayout();
        ResumeLayout(false);
    }
}
