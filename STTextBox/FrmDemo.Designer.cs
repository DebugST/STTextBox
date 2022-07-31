namespace STTextBoxTest
{
    partial class FrmDemo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.btn_single = new System.Windows.Forms.Button();
            this.btn_wrap = new System.Windows.Forms.Button();
            this.btn_nowrap = new System.Windows.Forms.Button();
            this.btn_emoji = new System.Windows.Forms.Button();
            this.btn_alpha = new System.Windows.Forms.Button();
            this.btn_style = new System.Windows.Forms.Button();
            this.btn_invisablechar = new System.Windows.Forms.Button();
            this.btn_about_code = new System.Windows.Forms.Button();
            this.btn_about = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_single
            // 
            this.btn_single.Location = new System.Drawing.Point(20, 12);
            this.btn_single.Name = "btn_single";
            this.btn_single.Size = new System.Drawing.Size(80, 23);
            this.btn_single.TabIndex = 0;
            this.btn_single.Text = "Single";
            this.btn_single.UseVisualStyleBackColor = true;
            // 
            // btn_wrap
            // 
            this.btn_wrap.Location = new System.Drawing.Point(106, 12);
            this.btn_wrap.Name = "btn_wrap";
            this.btn_wrap.Size = new System.Drawing.Size(80, 23);
            this.btn_wrap.TabIndex = 1;
            this.btn_wrap.Text = "WrapView";
            this.btn_wrap.UseVisualStyleBackColor = true;
            // 
            // btn_nowrap
            // 
            this.btn_nowrap.Location = new System.Drawing.Point(192, 12);
            this.btn_nowrap.Name = "btn_nowrap";
            this.btn_nowrap.Size = new System.Drawing.Size(80, 23);
            this.btn_nowrap.TabIndex = 2;
            this.btn_nowrap.Text = "NoWrapView";
            this.btn_nowrap.UseVisualStyleBackColor = true;
            // 
            // btn_emoji
            // 
            this.btn_emoji.Location = new System.Drawing.Point(20, 41);
            this.btn_emoji.Name = "btn_emoji";
            this.btn_emoji.Size = new System.Drawing.Size(80, 23);
            this.btn_emoji.TabIndex = 3;
            this.btn_emoji.Text = "emoji";
            this.btn_emoji.UseVisualStyleBackColor = true;
            // 
            // btn_alpha
            // 
            this.btn_alpha.Location = new System.Drawing.Point(106, 41);
            this.btn_alpha.Name = "btn_alpha";
            this.btn_alpha.Size = new System.Drawing.Size(80, 23);
            this.btn_alpha.TabIndex = 4;
            this.btn_alpha.Text = "Alpha";
            this.btn_alpha.UseVisualStyleBackColor = true;
            // 
            // btn_style
            // 
            this.btn_style.Location = new System.Drawing.Point(192, 41);
            this.btn_style.Name = "btn_style";
            this.btn_style.Size = new System.Drawing.Size(80, 23);
            this.btn_style.TabIndex = 5;
            this.btn_style.Text = "StyleMonitor";
            this.btn_style.UseVisualStyleBackColor = true;
            // 
            // btn_invisablechar
            // 
            this.btn_invisablechar.Location = new System.Drawing.Point(91, 70);
            this.btn_invisablechar.Name = "btn_invisablechar";
            this.btn_invisablechar.Size = new System.Drawing.Size(110, 23);
            this.btn_invisablechar.TabIndex = 6;
            this.btn_invisablechar.Text = "InvisableChar";
            this.btn_invisablechar.UseVisualStyleBackColor = true;
            // 
            // btn_about_code
            // 
            this.btn_about_code.Location = new System.Drawing.Point(91, 99);
            this.btn_about_code.Name = "btn_about_code";
            this.btn_about_code.Size = new System.Drawing.Size(110, 23);
            this.btn_about_code.TabIndex = 7;
            this.btn_about_code.Text = "About Code";
            this.btn_about_code.UseVisualStyleBackColor = true;
            // 
            // btn_about
            // 
            this.btn_about.Location = new System.Drawing.Point(91, 128);
            this.btn_about.Name = "btn_about";
            this.btn_about.Size = new System.Drawing.Size(110, 23);
            this.btn_about.TabIndex = 8;
            this.btn_about.Text = "About STTextBox";
            this.btn_about.UseVisualStyleBackColor = true;
            // 
            // FrmDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 158);
            this.Controls.Add(this.btn_about);
            this.Controls.Add(this.btn_about_code);
            this.Controls.Add(this.btn_invisablechar);
            this.Controls.Add(this.btn_style);
            this.Controls.Add(this.btn_alpha);
            this.Controls.Add(this.btn_emoji);
            this.Controls.Add(this.btn_nowrap);
            this.Controls.Add(this.btn_wrap);
            this.Controls.Add(this.btn_single);
            this.Name = "FrmDemo";
            this.Text = "FrmDemo";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_single;
        private System.Windows.Forms.Button btn_wrap;
        private System.Windows.Forms.Button btn_nowrap;
        private System.Windows.Forms.Button btn_emoji;
        private System.Windows.Forms.Button btn_alpha;
        private System.Windows.Forms.Button btn_style;
        private System.Windows.Forms.Button btn_invisablechar;
        private System.Windows.Forms.Button btn_about_code;
        private System.Windows.Forms.Button btn_about;
    }
}