using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ST.Library.Drawing;
using ST.Library.UI.STTextBox;
using System.Text.RegularExpressions;

namespace STTextBoxTest.Demos
{
    public partial class FrmEmoji : Form
    {
        public FrmEmoji() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);

            STTextBox tbx = new STTextBox();
            var emoji = new EmojiRender("./svg_mix_twe");
            tbx.SetEmojiRender(emoji);
            tbx.SetTextStyleMonitors(
                new LinkStyleMonitor(),
                new CSharpStyleMonitor()
                );
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            this.Controls.Add(tbx);

            string strCode = @"
//this is the code
STTextBox tbx = new STTextBox();
var emoji = new EmojiRender(""./svg_mix_twe"");
tbx.SetEmojiRender(emoji);
tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
tbx.Dock = DockStyle.Fill;
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
this.Controls.Add(tbx);

//🌹🍀🍎💰📱🌙🍁🍂🍃🌷💎🔪🔫🏀⚽⚡👄👍🔥
//😀😁😂😃😄😅😆😉😊😋😎😍😘😗😙😚☺😇😐
//for more info:https://github.com/DebugST/emoji-svg-render";
            tbx.SetText(strCode.Trim());
        }
    }
}
