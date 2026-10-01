#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace NetworkingToolkit
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer? components = null;

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
            tabControl1 = new DarkTabControl();
            tabPage1 = new TabPage();
            lblBitResult = new Label();
            tblBits = new TableLayoutPanel();
            convertBinaryButton = new Button();
            binaryInput = new TextBox();
            label2 = new Label();
            hexInput = new TextBox();
            convertHexButton = new Button();
            label1 = new Label();
            convertButton = new Button();
            decimalInput = new TextBox();
            resultLabel = new Label();
            label3 = new Label();
            tabPage2 = new TabPage();
            gridRequests = new DataGridView();
            Subnet = new DataGridViewTextBoxColumn();
            Hostsneeded = new DataGridViewTextBoxColumn();
            NID = new DataGridViewTextBoxColumn();
            Prefix = new DataGridViewTextBoxColumn();
            FU = new DataGridViewTextBoxColumn();
            LU = new DataGridViewTextBoxColumn();
            Broadcast = new DataGridViewTextBoxColumn();
            UsableHosts = new DataGridViewTextBoxColumn();
            txtNetwork = new TextBox();
            label5 = new Label();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridRequests).BeginInit();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1260, 600);
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(lblBitResult);
            tabPage1.Controls.Add(tblBits);
            tabPage1.Controls.Add(convertBinaryButton);
            tabPage1.Controls.Add(binaryInput);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(hexInput);
            tabPage1.Controls.Add(convertHexButton);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(convertButton);
            tabPage1.Controls.Add(decimalInput);
            tabPage1.Controls.Add(resultLabel);
            tabPage1.Controls.Add(label3);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1252, 572);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Subnetting";
            tabPage1.UseVisualStyleBackColor = true;
            tabPage1.Click += tabPage1_Click;
            // 
            // lblBitResult
            // 
            lblBitResult.AutoSize = true;
            lblBitResult.Location = new Point(332, 273);
            lblBitResult.Name = "lblBitResult";
            lblBitResult.Size = new Size(128, 15);
            lblBitResult.TabIndex = 15;
            lblBitResult.Text = "Decimal: 0  |  Hex: 0000";
            // 
            // tblBits
            // 
            tblBits.ColumnCount = 16;
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.25F));
            tblBits.Location = new Point(20, 170);
            tblBits.Name = "tblBits";
            tblBits.RowCount = 2;
            tblBits.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblBits.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblBits.Size = new Size(764, 100);
            tblBits.TabIndex = 14;
            tblBits.Paint += tableLayoutPanel1_Paint;
            // 
            // convertBinaryButton
            // 
            convertBinaryButton.Location = new Point(218, 79);
            convertBinaryButton.Name = "convertBinaryButton";
            convertBinaryButton.Size = new Size(75, 23);
            convertBinaryButton.TabIndex = 13;
            convertBinaryButton.Text = "Convert";
            convertBinaryButton.UseVisualStyleBackColor = true;
            convertBinaryButton.Click += convertBinaryButton_Click;
            // 
            // binaryInput
            // 
            binaryInput.Location = new Point(112, 80);
            binaryInput.Name = "binaryInput";
            binaryInput.Size = new Size(100, 23);
            binaryInput.TabIndex = 12;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 83);
            label2.Name = "label2";
            label2.Size = new Size(88, 15);
            label2.TabIndex = 11;
            label2.Text = "Binary number:";
            // 
            // hexInput
            // 
            hexInput.Location = new Point(112, 51);
            hexInput.Name = "hexInput";
            hexInput.Size = new Size(100, 23);
            hexInput.TabIndex = 10;
            // 
            // convertHexButton
            // 
            convertHexButton.Location = new Point(218, 51);
            convertHexButton.Name = "convertHexButton";
            convertHexButton.Size = new Size(75, 23);
            convertHexButton.TabIndex = 9;
            convertHexButton.Text = "Convert Hex";
            convertHexButton.UseVisualStyleBackColor = true;
            convertHexButton.Click += convertHexButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(8, 55);
            label1.Name = "label1";
            label1.Size = new Size(76, 15);
            label1.TabIndex = 8;
            label1.Text = "Hex number:";
            // 
            // convertButton
            // 
            convertButton.Location = new Point(218, 22);
            convertButton.Name = "convertButton";
            convertButton.Size = new Size(75, 23);
            convertButton.TabIndex = 7;
            convertButton.Text = "Convert";
            convertButton.UseVisualStyleBackColor = true;
            convertButton.Click += convertButton_Click;
            // 
            // decimalInput
            // 
            decimalInput.Location = new Point(112, 22);
            decimalInput.Name = "decimalInput";
            decimalInput.Size = new Size(100, 23);
            decimalInput.TabIndex = 6;
            decimalInput.TextChanged += textBox1_TextChanged;
            // 
            // resultLabel
            // 
            resultLabel.Location = new Point(315, 22);
            resultLabel.Name = "resultLabel";
            resultLabel.Size = new Size(300, 50);
            resultLabel.TabIndex = 5;
            resultLabel.Text = "Results will appear here";
            resultLabel.Click += resultLabel_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(8, 22);
            label3.Name = "label3";
            label3.Size = new Size(98, 15);
            label3.TabIndex = 4;
            label3.Text = "Decimal number:";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(gridRequests);
            tabPage2.Controls.Add(txtNetwork);
            tabPage2.Controls.Add(label5);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1252, 572);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "VLSM Planner";
            tabPage2.UseVisualStyleBackColor = true;
            tabPage2.Click += tabPage2_Click;
            // 
            // gridRequests
            // 
            gridRequests.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridRequests.Columns.AddRange(new DataGridViewColumn[] { Subnet, Hostsneeded, NID, Prefix, FU, LU, Broadcast, UsableHosts });
            gridRequests.Location = new Point(6, 32);
            gridRequests.Name = "gridRequests";
            gridRequests.Size = new Size(846, 300);
            gridRequests.TabIndex = 3;
            gridRequests.CellContentClick += gridRequests_CellContentClick;
            // 
            // Subnet
            // 
            Subnet.HeaderText = "Subnet";
            Subnet.Name = "Subnet";
            // 
            // Hostsneeded
            // 
            Hostsneeded.HeaderText = "Hosts needed";
            Hostsneeded.Name = "Hostsneeded";
            // 
            // NID
            // 
            NID.HeaderText = "Network ID";
            NID.Name = "NID";
            // 
            // Prefix
            // 
            Prefix.HeaderText = "Prefix";
            Prefix.Name = "Prefix";
            // 
            // FU
            // 
            FU.HeaderText = "First usable";
            FU.Name = "FU";
            // 
            // LU
            // 
            LU.HeaderText = "Last usable";
            LU.Name = "LU";
            // 
            // Broadcast
            // 
            Broadcast.HeaderText = "Broadcast";
            Broadcast.Name = "Broadcast";
            // 
            // UsableHosts
            // 
            UsableHosts.HeaderText = "Usable hosts";
            UsableHosts.Name = "UsableHosts";
            // 
            // txtNetwork
            // 
            txtNetwork.Location = new Point(145, 14);
            txtNetwork.Name = "txtNetwork";
            txtNetwork.Size = new Size(100, 23);
            txtNetwork.TabIndex = 2;
            txtNetwork.Text = "10.0.0.0";
            txtNetwork.TextChanged += txtNetwork_TextChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(8, 14);
            label5.Name = "label5";
            label5.Size = new Size(131, 15);
            label5.TabIndex = 1;
            label5.Text = "Starting network (CIDR)";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1260, 600);
            Controls.Add(tabControl1);
            Name = "Form1";
            Text = "NetworkingToolkit";
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridRequests).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private TabPage tabPage2 = null!;
        private TabPage tabPage1 = null!;
        private Button convertBinaryButton = null!;
        private TextBox binaryInput = null!;
        private Label label2 = null!;
        private TextBox hexInput = null!;
        private Button convertHexButton = null!;
        private Label label1 = null!;
        private Button convertButton = null!;
        private TextBox decimalInput = null!;
        private Label resultLabel = null!;
        private Label label3 = null!;
        private TableLayoutPanel tblBits = null!;
        private Label lblBitResult = null!;
        private TextBox txtNetwork = null!;
        private Label label5 = null!;
        private DataGridView gridRequests = null!;
        private DataGridViewTextBoxColumn Subnet = null!;
        private DataGridViewTextBoxColumn Hostsneeded = null!;
        private DataGridViewTextBoxColumn NID = null!;
        private DataGridViewTextBoxColumn Prefix = null!;
        private DataGridViewTextBoxColumn FU = null!;
        private DataGridViewTextBoxColumn LU = null!;
        private DataGridViewTextBoxColumn Broadcast = null!;
        private DataGridViewTextBoxColumn UsableHosts = null!;
        private DarkTabControl tabControl1;
    }
}
