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
    public partial class FrmSingleView : Form
    {
        public FrmSingleView() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            STTextBox tbx_single = new STTextBox();
            tbx_single.SetTextView(new SingleTextView());
            tbx_single.Dock = DockStyle.Top;
            tbx_single.Height = 30;
            tbx_single.AllowScrollBar = false;
            tbx_single.SetText("this is a single text box");

            STTextBox tbx_code = new STTextBox();
            tbx_code.SetTextStyleMonitors(new CSharpStyleMonitor());
            tbx_code.Dock = DockStyle.Fill;
            tbx_code.Font = new System.Drawing.Font("Consolas", 10);

            this.Controls.Add(tbx_code);
            this.Controls.Add(tbx_single);

            string strCode = @"
//this is code
STTextBox tbx_single = new STTextBox();
tbx_single.SetTextView(new SingleTextView());
tbx_single.Dock = DockStyle.Top;
tbx_single.Height = 30;
tbx_single.AllowScrollBar = false;
tbx_single.SetText(""this is a single text box"");
this.Controls.Add(tbx_single);";
            tbx_code.SetText(strCode.Trim());
        }
    }
}
