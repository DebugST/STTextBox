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
    public partial class FrmWrapView : Form
    {
        public FrmWrapView() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            var textView = new WrapTextView();      //default: word, left
            textView.ShowLineNumber = true;         //default: true
            textView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
            textView.ShowLineNumberGuide = true;    //default: true
            textView.LineNumberGuideColor = Color.Magenta;  //default: Magenta

            STTextBox tbx = new STTextBox();
            tbx.SetTextView(textView);
            tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            this.Controls.Add(tbx);

            string strCode = @"
//this is code
var textView = new WrapTextView();      //default: word, left
textView.ShowLineNumber = true;         //default: true
textView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
textView.ShowLineNumberGuide = true;    //default: true
textView.LineNumberGuideColor = Color.Magenta;  //default: Magenta

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
