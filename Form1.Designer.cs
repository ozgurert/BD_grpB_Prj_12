namespace RequirementsConverter
{
    partial class Form1
    {
        /// <summary>
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.cmbFormats = new System.Windows.Forms.ComboBox();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblResult = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(284, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Veri Dosyası Seçin";
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(123, 23);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(469, 37);
            this.btnSelectFile.TabIndex = 2;
            this.btnSelectFile.Text = "Dosya Seç";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // cmbFormats
            // 
            this.cmbFormats.FormattingEnabled = true;
            this.cmbFormats.Items.AddRange(new object[] {
            "XML",
            "YAML",
            "CSV"});
            this.cmbFormats.Location = new System.Drawing.Point(43, 204);
            this.cmbFormats.Name = "cmbFormats";
            this.cmbFormats.Size = new System.Drawing.Size(205, 24);
            this.cmbFormats.TabIndex = 4;
            // 
            // btnConvert
            // 
            this.btnConvert.Location = new System.Drawing.Point(306, 211);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(130, 36);
            this.btnConvert.TabIndex = 5;
            this.btnConvert.Text = "Dönüştür";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(54, 316);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(25, 16);
            this.lblResult.TabIndex = 6;
            this.lblResult.Text = "~~";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 33);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Dosya Yolu";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(40, 231);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(177, 16);
            this.label4.TabIndex = 8;
            this.label4.Text = "Hangi formata dönüştürelim?";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.cmbFormats);
            this.Controls.Add(this.btnSelectFile);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.ComboBox cmbFormats;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}

