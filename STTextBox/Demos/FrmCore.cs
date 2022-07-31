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
    public partial class FrmCore : Form
    {
        public FrmCore() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            var cs_style = new CSharpStyleMonitor();
            cs_style.CommentStyle = new TextStyle() {
                ForeColor = System.Drawing.Color.Gray,
                BackColor = System.Drawing.Color.FromArgb(20, 0, 0, 0)
            };
            STTextBox tbx = new STTextBox();
            tbx.Dock = DockStyle.Fill;
            tbx.LineSpacing = 2;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            tbx.SetTextStyleMonitors(       // use the monitor
                new LinkStyleMonitor(),
                new SelectionStyleMonitor(),
                cs_style
                );
            this.Controls.Add(tbx);
            tbx.SetText(strCode);
        }

        private string strCode = @"class STTextBox
{
    /*
    * Core is the core of STTextBox, which contains almost all the functions of STTextBox. 
    * Because the our ability is limited, many functions cannot be implemented efficiently, 
    * so it becomes an independent interface.
    *
    * Core是STTextBox的核心，里面包含了STTextBox几乎所有的功能，因为我们能力有限，
    * 很多功能无法高效的实现，所以独立成了接口。
    */
    public class Core
    {
        // save all the textline in manager
        public TextManager TextManager { get; private set; }
        // Used to get a complete character, such as Emoji may require multiple unicode combinations.
        // 用于获取一个完整的字符，比如Emoji可能需要多个unicode组合。
        public ITextBoundary IGraphemeSplitter { get; internal set; }
        // Used to get a word, like double click.
        // 用于获取一个完整的单词，比如双击的时候。
        public ITextBoundary IWordSplitter { get; internal set; }
        // All drawing functions used by STTextBox, if you need to switch other drawing engines, 
        // you can implement this interface. GDI+ is used by default.
        // STTextBox所有用到的绘图函数，如果需要切换其他绘图引擎可实现此接口。默认使用GDI+。
        public ISTTextBoxRender ITextBoxRender { get; internal set; }
        // If you need to display Emoji expressions, you need to implement this interface,
        // which has been implemented in-built.
        // 如果需要显示Emoji表情需要实现此接口，已经内置实现。
        public IEmojiRender IEmojiRender { get; internal set; }
        // Text box view, all rendering logic is implemented in this interface.
        // 文本框视图，所有渲染逻辑在此接口中实现。
        public ITextView ITextView { get; internal set; }
        // Used to save operation history.
        // 用于保存操作历史记录。
        public ITextHistory ITextHistory { get; internal set; }
        // Used to style text.
        // 用于设置文本样式。
        public ITextStyleMonitor[] ITextStyleMonitors { get; internal set; }
        /*some other code*/
        internal Core(STTextBox textBox) {
            // by default unicode 14.0 is used.
            // https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules
            this.IGraphemeSplitter = new GraphemeSplitter();
            // by default unicode 14.0 is used.
            // https://unicode.org/reports/tr29/#Word_Boundary_Rules
            this.IWordSplitter = new WordSplitter();
            // Caching can speed things up, but also requires some memory.
            //GraphemeSplitter.CreateArrayCache();
            //WordSplitter.CreateArrayCache();
            // by default GDI+ is used.
            this.ITextBoxRender = new STTextBoxGDIPRender();
            // by default nowrap view is used.
            this.ITextView = new NoWrapTextView();
            // by default just save 10 steps in memory.
            this.ITextHistory = new TextHistory(10);
            /*some other code*/
        }
    }
    /*some other code*/
}";
    }
}
