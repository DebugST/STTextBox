using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ST.Library.UI.STTextBox;
using System.Text.RegularExpressions;

namespace STTextBoxTest.Demos
{
    public partial class FrmStyle : Form
    {
        public FrmStyle() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            STTextBox tbx = new STTextBox();
            tbx.Dock = DockStyle.Fill;
            tbx.Font = new System.Drawing.Font("Consolas", 10);
            tbx.SetTextStyleMonitors(       // use the monitor
                new StyleDemo(),            // Priority is in order.
                new CSharpStyleMonitor(),
                new SelectionStyleMonitor()
                );
            this.Controls.Add(tbx);
            string strCode = @"
STTextBox tbx = new STTextBox();
tbx.Dock = DockStyle.Fill;
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
tbx.SetTextStyleMonitors(       // use the monitor
    new StyleDemo(),            // Priority is in order.
    new CSharpStyleMonitor(),
    new SelectionStyleMonitor()
    );
this.Controls.Add(tbx);
/*
 * this class show you how to customize the style monitor.
 * It is very flexible and can be very simple or very complex.
 * It depends on what logic the developer wants to use to implement it.
 * The following is a very simple implementation, 
 * while the built-in CSharpStyleMonitor is a complex implementation.
 */
public class StyleDemo : ITextStyleMonitor
{
    /*in this class just want to highlight the keyword `StyleDemo` in all text*/
    private static Regex m_reg_key = new Regex(@""\bStyleDemo\b"");
    private static TextStyle m_style = new TextStyle() {    // the key word style
        ForeColor = Color.Red,
        UnderLineColor = Color.Blue,
        StrikeOutColor = Color.FromArgb(200, Color.Lime),
        FontStyle = FontStyle.Underline | FontStyle.Strikeout | FontStyle.Italic
    };

    private List<TextStyleRange> m_lst; // save the style range

    public void Init(string strText) {
        List<TextStyleRange> lst = new List<TextStyleRange>();
        foreach (Match m in m_reg_key.Matches(strText)) {
            lst.Add(new TextStyleRange() {
                Index = m.Index,
                Length = m.Length,
                Style = m_style
            });
        }
        //FillDefaultStyle() will auto fill the rang that not have style.
        //m_lst must include all the index range in the strText.
        //because get the style use GetStyleFromCharIndex().
        m_lst = TextStyleMonitor.FillDefaultStyle(lst, strText.Length, TextStyle.Empty);
    }

    public void OnSelectionChanged(TextManager textManager, int nStart, int nLen) {
        //throw new NotImplementedException();
    }

    public void OnTextChanged(TextManager textManager, List<TextHistoryRecord> thrs) {
        this.Init(textManager.GetText());
    }

    public TextStyleRange GetStyleFromCharIndex(int nIndex) {
        return TextStyleMonitor.GetStyleFromCharIndex(nIndex, m_lst);
    }
}";
            tbx.SetText(strCode.Trim());
        }
    }
    /*
     * this class show you how to customize the style monitor.
     * It is very flexible and can be very simple or very complex.
     * It depends on what logic the developer wants to use to implement it.
     * The following is a very simple implementation, 
     * while the built-in CSharpStyleMonitor is a complex implementation.
     */
    public class StyleDemo : ITextStyleMonitor
    {
        /*in this class just want to highlight the keyword `StyleDemo` in all text*/
        private static Regex m_reg_key = new Regex(@"\bStyleDemo\b");
        private static TextStyle m_style = new TextStyle() {    // the key word style
            ForeColor = Color.Red,
            UnderLineColor = Color.Blue,
            StrikeOutColor = Color.FromArgb(200, Color.Lime),
            FontStyle = FontStyle.Underline | FontStyle.Strikeout | FontStyle.Italic
        };

        private List<TextStyleRange> m_lst; // save the style range

        public void Init(string strText) {
            List<TextStyleRange> lst = new List<TextStyleRange>();
            foreach (Match m in m_reg_key.Matches(strText)) {
                lst.Add(new TextStyleRange() {
                    Index = m.Index,
                    Length = m.Length,
                    Style = m_style
                });
            }
            //FillDefaultStyle() will auto fill the rang that not have style.
            //m_lst must include all the index range in the strText.
            //because get the style use GetStyleFromCharIndex().
            m_lst = TextStyleMonitor.FillDefaultStyle(lst, strText.Length, TextStyle.Empty);
        }

        public void OnSelectionChanged(TextManager textManager, int nStart, int nLen) {
            //throw new NotImplementedException();
        }

        public void OnTextChanged(TextManager textManager, List<TextHistoryRecord> thrs) {
            this.Init(textManager.GetText());
        }

        public TextStyleRange GetStyleFromCharIndex(int nIndex) {
            return TextStyleMonitor.GetStyleFromCharIndex(nIndex, m_lst);
        }
    }
}
