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
    public partial class FrmNoWrapView : Form
    {
        public FrmNoWrapView() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            var textView = new NoWrapTextView(); 
            textView.ShowLineNumber = true;         //default: true

            STTextBox tbx = new STTextBox();
            tbx.SetTextView(textView);
            tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            this.Controls.Add(tbx);

            string strCode = @"
//this is the code
var textView = new NoWrapTextView(); 
textView.ShowLineNumber = true;         //default: true

STTextBox tbx = new STTextBox();
tbx.SetTextView(textView);
tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
tbx.Dock = DockStyle.Fill;
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
this.Controls.Add(tbx);";
            tbx.SetText(strCode.Trim());
        }
    }
}
