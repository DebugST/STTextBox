using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ST.Library.UI.STTextBox;

namespace STTextBoxTest
{
    public partial class FrmTextBoundaryTest : Form
    {
        public FrmTextBoundaryTest() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            textBox2.Text = string.Empty;
            string strText = textBox1.Text;
            GraphemeSplitter gs = new GraphemeSplitter();
            gs.Each(strText, (str, nStart, nLen) => {
                textBox2.Text += str.Substring(nStart, nLen) + "\r\n";
            });
        }

        private void button2_Click(object sender, EventArgs e) {
            textBox2.Text = string.Empty;
            string strText = textBox1.Text;
            WordSplitter gs = new WordSplitter();
            gs.Each(strText, (str, nStart, nLen) => {
                textBox2.Text += str.Substring(nStart, nLen) + "\r\n";
            });
        }
    }
}
