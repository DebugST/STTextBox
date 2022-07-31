using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ST.Library.UI.STTextBox;

namespace STTextBoxTest.Demos
{
    public partial class FrmAlpha : Form
    {
        public FrmAlpha() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            this.BackgroundImage = Image.FromFile("./back.jpeg");
            this.BackgroundImageLayout = ImageLayout.Stretch;
            STTextBox tbx = new STTextBox();
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
            tbx.BackColor = Color.FromArgb(220, Color.White);
            this.Controls.Add(tbx);

            string strCode = @"
//this is the code, but slowly.
this.BackgroundImage = Image.FromFile(""./back.jpeg"");
this.BackgroundImageLayout = ImageLayout.Stretch;
STTextBox tbx = new STTextBox();
tbx.Dock = DockStyle.Fill;
tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
tbx.BackColor = Color.FromArgb(200, Color.White);
this.Controls.Add(tbx);";
            tbx.SetText(strCode.Trim());
        }
    }
}
