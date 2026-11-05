namespace Axis.App.RfidStation.Forms;

partial class EstacaoForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlTopo;
    private Label lblTenant;
    private Panel pnlBarra;
    private Label lblLeitor;
    private ComboBox cboLeitor;
    private Button btnConectar;
    private Button btnIniciar;
    private Button btnImprimir;
    private ListBox lstTags;
    private Label lblTotais;
    private Label lblStatus;
    private Label lblWeb;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlTopo = new Panel();
        lblTenant = new Label();
        pnlBarra = new Panel();
        lblLeitor = new Label();
        cboLeitor = new ComboBox();
        btnConectar = new Button();
        btnIniciar = new Button();
        btnImprimir = new Button();
        lstTags = new ListBox();
        lblTotais = new Label();
        lblStatus = new Label();
        lblWeb = new Label();
        pnlTopo.SuspendLayout();
        pnlBarra.SuspendLayout();
        SuspendLayout();
        // 
        // pnlTopo
        // 
        pnlTopo.BackColor = Color.FromArgb(0, 90, 158);
        pnlTopo.Controls.Add(lblTenant);
        pnlTopo.Dock = DockStyle.Top;
        pnlTopo.Location = new Point(0, 0);
        pnlTopo.Name = "pnlTopo";
        pnlTopo.Size = new Size(760, 44);
        pnlTopo.TabIndex = 0;
        // 
        // lblTenant
        // 
        lblTenant.Dock = DockStyle.Fill;
        lblTenant.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        lblTenant.ForeColor = Color.White;
        lblTenant.Location = new Point(0, 0);
        lblTenant.Name = "lblTenant";
        lblTenant.Padding = new Padding(12, 0, 0, 0);
        lblTenant.Size = new Size(760, 44);
        lblTenant.TabIndex = 0;
        lblTenant.Text = "Tenant:";
        lblTenant.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pnlBarra
        // 
        pnlBarra.Controls.Add(lblLeitor);
        pnlBarra.Controls.Add(cboLeitor);
        pnlBarra.Controls.Add(btnConectar);
        pnlBarra.Controls.Add(btnIniciar);
        pnlBarra.Controls.Add(btnImprimir);
        pnlBarra.Dock = DockStyle.Top;
        pnlBarra.Location = new Point(0, 44);
        pnlBarra.Name = "pnlBarra";
        pnlBarra.Padding = new Padding(12, 8, 12, 8);
        pnlBarra.Size = new Size(760, 56);
        pnlBarra.TabIndex = 1;
        // 
        // lblLeitor
        // 
        lblLeitor.AutoSize = true;
        lblLeitor.Location = new Point(12, 20);
        lblLeitor.Name = "lblLeitor";
        lblLeitor.Size = new Size(42, 15);
        lblLeitor.TabIndex = 0;
        lblLeitor.Text = "Leitor:";
        // 
        // cboLeitor
        // 
        cboLeitor.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLeitor.Location = new Point(60, 16);
        cboLeitor.Name = "cboLeitor";
        cboLeitor.Size = new Size(180, 23);
        cboLeitor.TabIndex = 1;
        // 
        // btnConectar
        // 
        btnConectar.Location = new Point(252, 15);
        btnConectar.Name = "btnConectar";
        btnConectar.Size = new Size(130, 26);
        btnConectar.TabIndex = 2;
        btnConectar.Text = "Conectar leitor";
        btnConectar.UseVisualStyleBackColor = true;
        btnConectar.Click += btnConectar_Click;
        // 
        // btnIniciar
        // 
        btnIniciar.Enabled = false;
        btnIniciar.Location = new Point(392, 15);
        btnIniciar.Name = "btnIniciar";
        btnIniciar.Size = new Size(140, 26);
        btnIniciar.TabIndex = 3;
        btnIniciar.Text = "Iniciar leitura";
        btnIniciar.UseVisualStyleBackColor = true;
        btnIniciar.Click += btnIniciar_Click;
        // 
        // btnImprimir
        // 
        btnImprimir.Location = new Point(600, 15);
        btnImprimir.Name = "btnImprimir";
        btnImprimir.Size = new Size(140, 26);
        btnImprimir.TabIndex = 4;
        btnImprimir.Text = "Imprimir etiquetas";
        btnImprimir.UseVisualStyleBackColor = true;
        btnImprimir.Click += btnImprimir_Click;
        // 
        // lstTags
        // 
        lstTags.Dock = DockStyle.Fill;
        lstTags.Font = new Font("Consolas", 9.5F);
        lstTags.FormattingEnabled = true;
        lstTags.ItemHeight = 16;
        lstTags.Location = new Point(0, 100);
        lstTags.Name = "lstTags";
        lstTags.Size = new Size(760, 350);
        lstTags.TabIndex = 2;
        // 
        // lblTotais
        // 
        lblTotais.Dock = DockStyle.Bottom;
        lblTotais.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        lblTotais.Location = new Point(0, 450);
        lblTotais.Name = "lblTotais";
        lblTotais.Padding = new Padding(12, 0, 0, 0);
        lblTotais.Size = new Size(760, 28);
        lblTotais.TabIndex = 3;
        lblTotais.Text = "EPCs distintos: 0   |   Transmitidas: 0";
        lblTotais.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblStatus
        // 
        lblStatus.BackColor = Color.FromArgb(240, 242, 245);
        lblStatus.Dock = DockStyle.Bottom;
        lblStatus.ForeColor = Color.DimGray;
        lblStatus.Location = new Point(0, 478);
        lblStatus.Name = "lblStatus";
        lblStatus.Padding = new Padding(12, 0, 0, 0);
        lblStatus.Size = new Size(760, 22);
        lblStatus.TabIndex = 4;
        lblStatus.Text = "Pronto.";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblWeb
        // 
        lblWeb.BackColor = Color.FromArgb(232, 240, 254);
        lblWeb.Dock = DockStyle.Bottom;
        lblWeb.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblWeb.ForeColor = Color.DimGray;
        lblWeb.Location = new Point(0, 500);
        lblWeb.Name = "lblWeb";
        lblWeb.Padding = new Padding(12, 0, 0, 0);
        lblWeb.Size = new Size(760, 24);
        lblWeb.TabIndex = 5;
        lblWeb.Text = "Axis Web: desconectado";
        lblWeb.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // EstacaoForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(760, 524);
        Controls.Add(lstTags);
        Controls.Add(lblTotais);
        Controls.Add(lblStatus);
        Controls.Add(lblWeb);
        Controls.Add(pnlBarra);
        Controls.Add(pnlTopo);
        MinimumSize = new Size(700, 460);
        Name = "EstacaoForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Axis RFID Station - Estacao";
        FormClosing += EstacaoForm_FormClosing;
        Load += EstacaoForm_Load;
        pnlTopo.ResumeLayout(false);
        pnlBarra.ResumeLayout(false);
        pnlBarra.PerformLayout();
        ResumeLayout(false);
    }
}
