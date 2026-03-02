namespace TestFlaUI;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        panel1 = new Panel();
        label13 = new Label();
        tbWono = new TextBox();
        cbStyle = new ComboBox();
        tbCu = new TextBox();
        label11 = new Label();
        lbWn = new Label();
        label3 = new Label();
        tbCom = new TextBox();
        dt = new DataGridView();
        dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
        dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
        dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
        btSend = new Button();
        panel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dt).BeginInit();
        SuspendLayout();
        // 
        // panel1
        // 
        panel1.Controls.Add(label13);
        panel1.Controls.Add(tbWono);
        panel1.Controls.Add(cbStyle);
        panel1.Controls.Add(tbCu);
        panel1.Controls.Add(label11);
        panel1.Controls.Add(lbWn);
        panel1.Controls.Add(label3);
        panel1.Controls.Add(tbCom);
        panel1.Dock = DockStyle.Top;
        panel1.Location = new Point(0, 0);
        panel1.Name = "panel1";
        panel1.Size = new Size(385, 217);
        panel1.TabIndex = 16;
        // 
        // label13
        // 
        label13.BackColor = SystemColors.ControlDarkDark;
        label13.ForeColor = Color.Transparent;
        label13.Location = new Point(21, 66);
        label13.Name = "label13";
        label13.Padding = new Padding(2);
        label13.Size = new Size(172, 29);
        label13.TabIndex = 4;
        label13.Text = "Custom input";
        // 
        // tbWono
        // 
        tbWono.Location = new Point(216, 15);
        tbWono.Name = "tbWono";
        tbWono.Size = new Size(150, 31);
        tbWono.TabIndex = 2;
        // 
        // cbStyle
        // 
        cbStyle.FormattingEnabled = true;
        cbStyle.Location = new Point(216, 168);
        cbStyle.Name = "cbStyle";
        cbStyle.Size = new Size(150, 33);
        cbStyle.TabIndex = 12;
        // 
        // tbCu
        // 
        tbCu.Location = new Point(216, 66);
        tbCu.Name = "tbCu";
        tbCu.Size = new Size(150, 31);
        tbCu.TabIndex = 2;
        // 
        // label11
        // 
        label11.BackColor = SystemColors.ControlDarkDark;
        label11.ForeColor = Color.Transparent;
        label11.Location = new Point(21, 168);
        label11.Name = "label11";
        label11.Padding = new Padding(2, 2, 36, 2);
        label11.Size = new Size(172, 29);
        label11.TabIndex = 11;
        label11.Text = "Kiểu đóng gói";
        // 
        // lbWn
        // 
        lbWn.BackColor = SystemColors.ControlDarkDark;
        lbWn.ForeColor = Color.Transparent;
        lbWn.Location = new Point(21, 15);
        lbWn.Name = "lbWn";
        lbWn.Padding = new Padding(2);
        lbWn.Size = new Size(172, 29);
        lbWn.TabIndex = 3;
        lbWn.Text = "WOno";
        // 
        // label3
        // 
        label3.BackColor = SystemColors.ControlDarkDark;
        label3.ForeColor = Color.Transparent;
        label3.Location = new Point(21, 117);
        label3.Name = "label3";
        label3.Padding = new Padding(2, 2, 36, 2);
        label3.Size = new Size(172, 29);
        label3.TabIndex = 6;
        label3.Text = "Số hoàn thành";
        // 
        // tbCom
        // 
        tbCom.Location = new Point(216, 117);
        tbCom.Name = "tbCom";
        tbCom.Size = new Size(150, 31);
        tbCom.TabIndex = 5;
        // 
        // dt
        // 
        dt.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dt.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3 });
        dt.Dock = DockStyle.Top;
        dt.Location = new Point(0, 217);
        dt.Name = "dt";
        dt.ReadOnly = false;
        dt.RowHeadersVisible = false;
        dt.RowHeadersWidth = 62;
        dt.Size = new Size(385, 145);
        dt.TabIndex = 15;
        // 
        // dataGridViewTextBoxColumn1
        // 
        dataGridViewTextBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        dataGridViewTextBoxColumn1.HeaderText = "Số lượng đóng";
        dataGridViewTextBoxColumn1.MinimumWidth = 8;
        dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
        dataGridViewTextBoxColumn1.ReadOnly = false;
        dataGridViewTextBoxColumn1.Width = 169;
        // 
        // dataGridViewTextBoxColumn2
        // 
        dataGridViewTextBoxColumn2.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        dataGridViewTextBoxColumn2.HeaderText = "Số lô";
        dataGridViewTextBoxColumn2.MinimumWidth = 8;
        dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
        dataGridViewTextBoxColumn2.ReadOnly = false;
        dataGridViewTextBoxColumn2.Width = 89;
        // 
        // dataGridViewTextBoxColumn3
        // 
        dataGridViewTextBoxColumn3.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        dataGridViewTextBoxColumn3.HeaderText = "DEL";
        dataGridViewTextBoxColumn3.MinimumWidth = 8;
        dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
        dataGridViewTextBoxColumn3.ReadOnly = true;
        dataGridViewTextBoxColumn3.Width = 78;
        // 
        // btSend
        // 
        btSend.Location = new Point(83, 386);
        btSend.Name = "btSend";
        btSend.Size = new Size(138, 52);
        btSend.TabIndex = 17;
        btSend.Text = "Send";
        btSend.UseVisualStyleBackColor = true;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(385, 450);
        Controls.Add(btSend);
        Controls.Add(dt);
        Controls.Add(panel1);
        Name = "Form1";
        Text = "Form1";
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dt).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private Label lbWn;
    private TextBox tbWono;
    private Label label13;
    private TextBox tbCu;
    private Label label3;
    private TextBox tbCom;
    private Label label11;
    private ComboBox cbStyle;
    private DataGridView dt;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
    private Panel panel1;
    private Button btSend;
}
