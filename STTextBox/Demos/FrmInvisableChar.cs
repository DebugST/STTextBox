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
    public partial class FrmInvisableChar : Form
    {
        public FrmInvisableChar() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            STTextBoxGDIPRender render = new STTextBoxGDIPRender();
            render.ShowInVisableChar = true;
            render.InVisableCharColor = Color.Red;
            STTextBox tbx = new STTextBox();
            tbx.SetTextBoxRender(render);
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            tbx.SetTextStyleMonitors(       // use the monitor
                new CSharpStyleMonitor(),
                new SelectionStyleMonitor()
                );
            this.Controls.Add(tbx);
            string strCode = @"
//tab:	,space: ,newline:
STTextBoxGDIPRender render = new STTextBoxGDIPRender();
render.ShowInVisableChar = true;    //default: false
render.InVisableCharColor = Color.Red;
STTextBox tbx = new STTextBox();
tbx.SetTextBoxRender(render);
tbx.Dock = DockStyle.Fill;
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
tbx.SetTextStyleMonitors(       // use the monitor
    new CSharpStyleMonitor(),
    new SelectionStyleMonitor()
    );
this.Controls.Add(tbx);
// ====
// STTextBoxGDIPRender is build-in class use gdip render the textbox
// There are some method:
//      protected virtual void OnDrawNewLineChar(Rectangle rect);
//      protected virtual void OnDrawSpaceChar(Rectangle rect);
//      protected virtual void OnDrawTabChar(Rectangle rect);";
            tbx.SetText(strCode.Trim());
        }
    }
}
