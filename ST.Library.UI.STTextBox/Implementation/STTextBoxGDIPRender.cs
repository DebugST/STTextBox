using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Windows.Forms;

namespace ST.Library.UI.STTextBox
{
    public class STTextBoxGDIPRender : ISTTextBoxRender, IDisposable
    {
        public bool ShowInVisableChar { get; set; }
        public Color InVisableCharColor { get; set; }

        public float XDPIZoom { get; private set; }
        public float YDPIZoom { get; private set; }
        public int TabSize { get; private set; }
        public int SpaceWidth { get; private set; }
        public int FontHeight { get; private set; }
        public bool IsMonospaced { get; private set; }

        private Font m_ft_normal;
        private Font m_ft_bold;
        private Font m_ft_italic;
        private Font m_ft_bold_italic;
        private StringFormat m_sf;
        private SolidBrush m_brush;
        private Graphics m_g_onpaint;
        private Graphics m_g_private;
        private float m_italic_width;

        private Rectangle m_rect_temp;
        private Rectangle m_rect_line;
        private Dictionary<string, int> m_dic_width_cache;


        private Control m_ctrl;
        private int m_nCounter;

        public STTextBoxGDIPRender() {
            this.TabSize = 4;
            m_dic_width_cache = new Dictionary<string, int>();
            m_sf = new StringFormat(StringFormat.GenericTypographic);
            m_sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            m_sf.Alignment = StringAlignment.Center;
            m_sf.LineAlignment = StringAlignment.Center;
            m_brush = new SolidBrush(Color.Black);
            this.InVisableCharColor = Color.FromArgb(50, Color.Magenta);
            this.ShowInVisableChar = false;
        }

        public void BindControl(Control ctrl) {
            if (ctrl == null) {
                throw new ArgumentNullException("ctrl");
            }
            m_ctrl = ctrl;
            m_ctrl.FontChanged += m_ctrl_FontChanged;
            this.InitData(m_ctrl.Font);
        }

        public void UnbindControl() {
            m_ctrl.FontChanged -= m_ctrl_FontChanged;
        }

        public void OnBeginPaint(Graphics g) {
            //m_g_onpaint = g;
            //m_b_onpaint = true;
            this.XDPIZoom = g.DpiX / 96;
            this.YDPIZoom = g.DpiY / 96;
            if (this.XDPIZoom < 1) this.XDPIZoom = 1;
            if (this.YDPIZoom < 1) this.YDPIZoom = 1;
            m_g_onpaint = g;
        }

        public void OnEndPaint(Graphics g) {
            m_g_onpaint = null;
        }

        public void BeginPaint() {
            if (m_nCounter == 0) {
                m_g_private = m_ctrl.CreateGraphics();
            }
            var g = m_g_private;
            this.XDPIZoom = g.DpiX / 96;
            this.YDPIZoom = g.DpiY / 96;
            m_nCounter++;
        }

        public void EndPaint() {
            if (--m_nCounter == 0) {
                m_g_private.Dispose();
                m_g_private = null;
            }
        }

        public int SetTabSize(int nSize) {
            if (nSize < 0) {
                throw new ArgumentOutOfRangeException("nSize", "The value must be more than 0");
            }
            if (nSize > 10) {
                throw new ArgumentOutOfRangeException("nSize", "The value must be less than 10");
            }
            int nRet = this.TabSize;
            this.TabSize = nSize;
            return nRet;
        }

        public int GetTabSize() {
            return this.TabSize;
        }

        public int GetTabWidth(int nLeftWidth) {
            return (int)Math.Round(this.GetSpaceWidth() * this.GetTabSpaceCount(nLeftWidth, this.TabSize));
        }

        public int GetFontHeight() { return this.FontHeight; }

        public int GetSpaceWidth() { return this.SpaceWidth; }

        public float GetTabSpaceCount(int nLeftWidth, int nTabSize) {
            if (nTabSize < 0) {
                throw new ArgumentOutOfRangeException("nTabSize", "The value must be more than 0");
            }
            int nSpaceWidth = this.GetSpaceWidth();
            if (nTabSize == 1) {      //If the TAB size just one SPACE,return the SPACE width;
                return nSpaceWidth;
            }
            int nTabFullWidth = nSpaceWidth * nTabSize;
            float nSpaceCount = (nTabFullWidth - (nLeftWidth % nTabFullWidth)) / (float)nSpaceWidth;
            return nSpaceCount < 1 ? nSpaceCount + nTabSize : nSpaceCount;
        }

        public int GetStringWidth(string strText, TextStyle style, int nLeftWidth) {
            return this.GetStringWidth(strText, style, nLeftWidth, true);
        }

        public int GetStringWidth(string strText, TextStyle style, int nLeftWidth, bool bCache) {
            if (string.IsNullOrEmpty(strText) || m_ft_normal == null) {
                return 0;
            }
            Graphics g = this.GetGraphics();
            string strKey = this.GetFontFlag(style);
            Font ft = this.GetFontFromFlag(strKey);
            strKey += ":" + strText;
            if (m_dic_width_cache.ContainsKey(strKey)) {
                return m_dic_width_cache[strKey];
            }
            if (strText == "\t") {
                return this.GetTabWidth(nLeftWidth);
            }
            float fWidth = g.MeasureString(strText, ft, 10000, m_sf).Width;
            if ((ft.Style & FontStyle.Italic) == FontStyle.Italic) {
                fWidth += m_italic_width;
            }
            int nWidth = (int)Math.Ceiling(fWidth);
            if (bCache) {
                m_dic_width_cache.Add(strKey, nWidth);
            }
            return nWidth;
        }

        public void DrawString(string strText, Font ft, Color color, Rectangle rect, StringFormat sf) {
            m_brush.Color = color;
            this.GetGraphics().DrawString(strText, ft, m_brush, rect, sf);
        }

        public void DrawString(string strText, TextStyle style, Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_rect_line.X = rect.X;
            m_rect_line.Width = rect.Width;
            if (style.BackColor.A > 0) {
                m_brush.Color = style.BackColor;
                g.FillRectangle(m_brush, rect);
            }
            if ((style.FontStyle & FontStyle.Underline) == FontStyle.Underline && style.UnderLineColor.A > 0) {
                m_rect_line.Y = rect.Bottom - m_rect_line.Height;
                m_brush.Color = style.UnderLineColor;
                g.FillRectangle(m_brush, m_rect_line);
            }
            if (style.ForeColor.A > 0) {
                m_brush.Color = style.ForeColor;
                switch (strText[0]) {
                    case ' ':
                        if (this.ShowInVisableChar) this.OnDrawSpaceChar(rect);
                        return;
                    case '\t':
                        if (this.ShowInVisableChar) this.OnDrawTabChar(rect);
                        return;
                    case '\r':
                    case '\n':
                        if (this.ShowInVisableChar) this.OnDrawNewLineChar(rect);
                        return;
                    default:
                        g.DrawString(strText, this.GetFontFromFlag(this.GetFontFlag(style)), m_brush, rect, m_sf);
                        break;
                }
            }
            if ((style.FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout && style.StrikeOutColor.A > 0) {
                m_rect_line.Y = rect.Y + rect.Height / 2;
                m_brush.Color = style.StrikeOutColor;
                g.FillRectangle(m_brush, m_rect_line);
            }
        }

        public void DrawImage(Image image, Rectangle rect) {
            this.GetGraphics().DrawImage(image, rect);
        }

        public void FillRectangle(Color backColor, Rectangle rect) {
            m_brush.Color = backColor;
            this.GetGraphics().FillRectangle(m_brush, rect);
        }

        public void FillRectangle(Color backColor, int nX, int nY, int nWidth, int nHeight) {
            m_brush.Color = backColor;
            this.GetGraphics().FillRectangle(m_brush, nX, nY, nWidth, nHeight);
        }

        public void SetClip(Rectangle rect) {
            this.GetGraphics().SetClip(rect);
        }

        public void ResetClip() {
            this.GetGraphics().ResetClip();
        }

        // [protected] ==========================================

        protected virtual void OnDrawNewLineChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            m_rect_temp.X = rect.X + (int)Math.Round(this.XDPIZoom);
            m_rect_temp.Y = rect.Y + rect.Height / 2 - (int)Math.Round(this.YDPIZoom);
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom * 3);
            m_rect_temp.Width = (int)Math.Round(this.XDPIZoom);
            g.FillRectangle(m_brush, m_rect_temp);      // draw -> |
            m_rect_temp.X += (int)Math.Round(this.XDPIZoom);
            m_rect_temp.Y += (int)Math.Round(this.YDPIZoom);
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom);
            m_rect_temp.Width = rect.Width - (int)Math.Round(this.XDPIZoom * 4);
            g.FillRectangle(m_brush, m_rect_temp);      // draw -> -
            m_rect_temp.X = m_rect_temp.Right;
            m_rect_temp.Width = (int)Math.Round(this.XDPIZoom);
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom * 5);
            m_rect_temp.Y -= (int)Math.Round(this.YDPIZoom * 4);
            g.FillRectangle(m_brush, m_rect_temp);      // draw -> |
        }

        protected virtual void OnDrawTabChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom * 3);
            m_rect_temp.Width = (int)Math.Round(this.XDPIZoom);// *2;
            m_rect_temp.Y = rect.Y + rect.Height / 2 - (int)Math.Round(this.YDPIZoom);
            m_rect_temp.X = rect.X + (int)Math.Round(this.XDPIZoom);
            g.FillRectangle(m_brush, m_rect_temp);
            m_rect_temp.X = rect.Right - (int)Math.Round(this.XDPIZoom * 2);
            g.FillRectangle(m_brush, m_rect_temp);
            m_rect_temp.Y += (int)Math.Round(this.YDPIZoom);
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom);
            m_rect_temp.X = rect.X + (int)Math.Round(this.XDPIZoom * 2);
            m_rect_temp.Width = rect.Width - (int)Math.Round(this.XDPIZoom * 4);
            g.FillRectangle(m_brush, m_rect_temp);
        }

        protected virtual void OnDrawSpaceChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            m_rect_temp.Height = (int)Math.Round(this.YDPIZoom);
            m_rect_temp.Width = rect.Width - (int)Math.Round(this.XDPIZoom * 2);// *2;
            m_rect_temp.X = rect.X + (int)Math.Round(this.XDPIZoom);
            m_rect_temp.Y = rect.Bottom - (int)Math.Round(this.YDPIZoom * 2);
            g.FillRectangle(m_brush, m_rect_temp);
            m_rect_temp.Width = m_rect_temp.Height;
            m_rect_temp.X = rect.X + m_rect_temp.Width;
            m_rect_temp.Y -= m_rect_temp.Height;
            g.FillRectangle(m_brush, m_rect_temp);
            m_rect_temp.X = rect.Right - (int)Math.Round(this.XDPIZoom * 2);
            g.FillRectangle(m_brush, m_rect_temp);
        }

        // [private] ============================================

        private void m_ctrl_FontChanged(object sender, EventArgs e) {
            this.InitData(m_ctrl.Font);
        }

        private void InitData(Font ft) {
            if (m_ft_normal == ft) {
                return;
            }
            this.BeginPaint();
            m_rect_line.Height = (int)Math.Round(this.YDPIZoom * 1);
            if (m_rect_line.Height == 0) m_rect_line.Height = 1;
            m_ft_normal = ft;
            if (m_ft_bold != null) m_ft_bold.Dispose();
            m_ft_bold = new Font(ft, FontStyle.Bold);
            if (m_ft_italic != null) m_ft_italic.Dispose();
            m_ft_italic = new Font(ft, FontStyle.Italic);
            if (m_ft_bold_italic != null) m_ft_bold_italic.Dispose();
            m_ft_bold_italic = new Font(ft, FontStyle.Bold | FontStyle.Italic);
            this.FontHeight = ft.Height;
            m_dic_width_cache.Clear();
            //SPACE are used to calculate TAB width.
            int nWidth = this.GetStringWidth(" ", TextStyle.Empty, 0);
            m_dic_width_cache.Add(" ", nWidth);
            m_dic_width_cache.Add("\r", nWidth);
            m_dic_width_cache.Add("\n", nWidth);
            m_dic_width_cache.Add("\r\n", nWidth);
            this.SpaceWidth = nWidth;
            this.IsMonospaced = this.GetStringWidth("i", TextStyle.Empty, 0) == this.GetStringWidth("W", TextStyle.Empty, 0);
            if (!this.IsMonospaced) {
                m_italic_width = (float)(this.FontHeight * Math.Tan(12 * Math.PI / 180));
            } else {
                m_italic_width = 0;
            }
            this.EndPaint();
        }

        private Graphics GetGraphics() {
            if (m_g_onpaint != null) return m_g_onpaint;
            if (m_g_private != null) return m_g_private;
            throw new InvalidOperationException("The Graphics is null, please call BeginPaint() to init it.");
        }

        private string GetFontFlag(TextStyle style) {
            string strKey = "N";
            if ((style.FontStyle & FontStyle.Bold) == FontStyle.Bold) {
                strKey += "B";
            }
            if ((style.FontStyle & FontStyle.Italic) == FontStyle.Italic) {
                strKey += "I";
            }
            return strKey;
        }

        private Font GetFontFromFlag(string strFlag) {
            switch (strFlag) {
                case "NBI":
                    return m_ft_bold_italic;
                case "NB":
                    return m_ft_bold;
                case "NI":
                    return m_ft_italic;
            }
            return m_ft_normal;
        }

        // [interface] ==========================================

        public void Dispose() {
            if (m_g_private != null) m_g_private.Dispose();
            if (m_ft_bold != null) m_ft_bold.Dispose();
            if (m_ft_italic != null) m_ft_italic.Dispose();
            if (m_ft_bold_italic != null) m_ft_bold_italic.Dispose();
            if (m_brush != null) m_brush.Dispose();
            if (m_sf != null) m_sf.Dispose();
            if (m_dic_width_cache != null) m_dic_width_cache.Clear();
        }
    }
}
