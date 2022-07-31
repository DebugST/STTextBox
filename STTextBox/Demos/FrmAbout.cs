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
    public partial class FrmAbout : Form
    {
        public FrmAbout() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            STTextBox tbx = new STTextBox();
            tbx.SetTextStyleMonitors(
                new LinkStyleMonitor()
                );
            tbx.SetTextView(new WrapTextView());
            tbx.LineSpacing = 5;
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            this.Controls.Add(tbx);
            string strText = @"STTextBox是我和我的朋友[netero:https://github.com/0x54164]设计的，使用GDI绘制的一个WinForm控件。
我们采用MIT开源协议，代码使用.Net3.5 + vs2010构建，所以几乎兼容所有的VS版本。
在设计过程中还产生了两个开源项目:
    [emoji-svg-render]    - https://github.com/DebugST/emoji-svg-render
    [STGraphemeSplitter]  - https://github.com/DebugST/STGraphemeSplitter
因为我们能力有限，所以很多功能实现的并不如意，所以我们将核心功能修改成了独立的接口，
以便其他开发者去独立实现，而不用在整个控件中去找寻代码并修改。

STTextBox is a WinForm control drawn by me and my friend [netero:https://github.com/0x54164] using GDI.
We adopt the MIT open source license, and the code is built with .Net3.5 + vs2010, 
so it is compatible with almost all VS versions.
Two open source projects were also generated during the design process:
     [emoji-svg-render]   - https://github.com/DebugST/emoji-svg-render
     [STGraphemeSplitter] - https://github.com/DebugST/STGraphemeSplitter
Because of our limited capabilities, many functions are not well implemented, 
so we modified the core functions into independent interfaces,
So that other developers can implement it independently 
without having to find and modify the code in the entire control.
  ";
            tbx.SetText(strText.Trim());
        }
    }
}
