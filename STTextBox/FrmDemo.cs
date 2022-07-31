using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STTextBoxTest
{
    public partial class FrmDemo : Form
    {
        public FrmDemo() {
            InitializeComponent();
            this.Text = "STTextBox Demos";
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            btn_single.Click += (s, e) => new Demos.FrmSingleView().Show();
            btn_wrap.Click += (s, e) => new Demos.FrmWrapView().Show();
            btn_nowrap.Click += (s, e) => new Demos.FrmNoWrapView().Show();
            btn_emoji.Click += (s, e) => new Demos.FrmEmoji().Show();
            btn_alpha.Click += (s, e) => new Demos.FrmAlpha().Show();
            btn_style.Click += (s, e) => new Demos.FrmStyle().Show();
            btn_invisablechar.Click += (s, e) => new Demos.FrmInvisableChar().Show();
            btn_about_code.Click += (s, e) => new Demos.FrmCore().Show();
            btn_about.Click += (s, e) => new Demos.FrmAbout().Show();
        }
    }
}
