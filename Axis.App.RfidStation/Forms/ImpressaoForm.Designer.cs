namespace Axis.App.RfidStation.Forms;

partial class ImpressaoForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblReferencia;
    private TextBox txtReferencia;
    private Button btnBuscar;
    private ListBox lstProdutos;
    private Label lblEpc;
    private TextBox txtEpc;
    private CheckBox chkVincular;
    private Button btnImprimir;
    private Button btnTestar;
    private Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblReferencia = new Label();
        txtReferencia = new TextBox();
        btnBuscar = new Button();
        lstProdutos = new ListBox();
        lblEpc = new Label();
        txtEpc = new TextBox();
        chkVincular = new CheckBox();
        btnImprimir = new Button();
        btnTestar = new Button();
        lblStatus = new Label();
        SuspendLayout();
        // 
        // lblReferencia
        // 
        lblReferencia.AutoSize = true;
        lblReferencia.Location = new Point(16, 18);
        lblReferencia.Name = "lblReferencia";
        lblReferencia.Size = new Size(65, 15);
        lblReferencia.TabIndex = 0;
        lblReferencia.Text = "Referencia";
        // 
        // txtReferencia
        // 
        txtReferencia.Location = new Point(16, 38);
        txtReferencia.Name = "txtReferencia";
        txtReferencia.Size = new Size(320, 23);
        txtReferencia.TabIndex = 1;
        // 
        // btnBuscar
        // 
        btnBuscar.Location = new Point(344, 37);
        btnBuscar.Name = "btnBuscar";
        btnBuscar.Size = new Size(100, 25);
        btnBuscar.TabIndex = 2;
        btnBuscar.Text = "Buscar";
        btnBuscar.UseVisualStyleBackColor = true;
        btnBuscar.Click += btnBuscar_Click;
        // 
        // lstProdutos
        // 
        lstProdutos.Font = new Font("Consolas", 9.5F);
        lstProdutos.FormattingEnabled = true;
        lstProdutos.ItemHeight = 16;
        lstProdutos.Location = new Point(16, 74);
        lstProdutos.Name = "lstProdutos";
        lstProdutos.Size = new Size(428, 196);
        lstProdutos.TabIndex = 3;
        lstProdutos.SelectedIndexChanged += lstProdutos_SelectedIndexChanged;
        // 
        // lblEpc
        // 
        lblEpc.AutoSize = true;
        lblEpc.Location = new Point(16, 282);
        lblEpc.Name = "lblEpc";
        lblEpc.Size = new Size(155, 15);
        lblEpc.TabIndex = 4;
        lblEpc.Text = "EPC (vazio = gerar novo)";
        // 
        // txtEpc
        // 
        txtEpc.CharacterCasing = CharacterCasing.Upper;
        txtEpc.Font = new Font("Consolas", 9.5F);
        txtEpc.Location = new Point(16, 302);
        txtEpc.MaxLength = 24;
        txtEpc.Name = "txtEpc";
        txtEpc.Size = new Size(320, 22);
        txtEpc.TabIndex = 5;
        // 
        // chkVincular
        // 
        chkVincular.AutoSize = true;
        chkVincular.Checked = true;
        chkVincular.CheckState = CheckState.Checked;
        chkVincular.Location = new Point(16, 334);
        chkVincular.Name = "chkVincular";
        chkVincular.Size = new Size(270, 19);
        chkVincular.TabIndex = 6;
        chkVincular.Text = "Vincular a tag a variacao apos imprimir";
        chkVincular.UseVisualStyleBackColor = true;
        // 
        // btnImprimir
        // 
        btnImprimir.BackColor = Color.FromArgb(0, 120, 215);
        btnImprimir.FlatStyle = FlatStyle.Flat;
        btnImprimir.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnImprimir.ForeColor = Color.White;
        btnImprimir.Location = new Point(16, 362);
        btnImprimir.Name = "btnImprimir";
        btnImprimir.Size = new Size(200, 34);
        btnImprimir.TabIndex = 7;
        btnImprimir.Text = "Imprimir + Encode";
        btnImprimir.UseVisualStyleBackColor = false;
        btnImprimir.Click += btnImprimir_Click;
        // 
        // btnTestar
        // 
        btnTestar.Location = new Point(244, 362);
        btnTestar.Name = "btnTestar";
        btnTestar.Size = new Size(200, 34);
        btnTestar.TabIndex = 8;
        btnTestar.Text = "Testar impressora";
        btnTestar.UseVisualStyleBackColor = true;
        btnTestar.Click += btnTestar_Click;
        // 
        // lblStatus
        // 
        lblStatus.BackColor = Color.FromArgb(240, 242, 245);
        lblStatus.Dock = DockStyle.Bottom;
        lblStatus.ForeColor = Color.DimGray;
        lblStatus.Location = new Point(0, 410);
        lblStatus.Name = "lblStatus";
        lblStatus.Padding = new Padding(12, 0, 0, 0);
        lblStatus.Size = new Size(460, 26);
        lblStatus.TabIndex = 9;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // ImpressaoForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(460, 436);
        Controls.Add(lblReferencia);
        Controls.Add(txtReferencia);
        Controls.Add(btnBuscar);
        Controls.Add(lstProdutos);
        Controls.Add(lblEpc);
        Controls.Add(txtEpc);
        Controls.Add(chkVincular);
        Controls.Add(btnImprimir);
        Controls.Add(btnTestar);
        Controls.Add(lblStatus);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ImpressaoForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Impressao de Etiquetas RFID";
        ResumeLayout(false);
        PerformLayout();
    }
}
