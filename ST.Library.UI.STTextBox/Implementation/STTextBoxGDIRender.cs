using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ST.Library.UI.STTextBox
{
    /// <summary>
    /// High-performance GDI-based text renderer for STTextBox using Windows GDI TextRenderer.
    /// Provides pixel-perfect text rendering, fast cached measurements, and native Windows rendering speed.
    /// </summary>
    public class STTextBoxGDIRender : ISTTextBoxRender, IDisposable
    {
        public const TextFormatFlags TEXT_FORMAT_FLAGS =
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.PreserveGraphicsClipping;

        public bool ShowInVisableChar { get; set; }
        public Color InVisableCharColor { get; set; }

        public float XDPIZoom { get; private set; }
        public float YDPIZoom { get; private set; }
        public int TabSize { get; private set; }
        public int SpaceWidth { get; private set; }
        public int FontHeight { get; private set; }
        public bool IsMonospaced { get; private set; }
        public float CharWidthFloat { get { return (float)this.SpaceWidth; } }

        private Font m_ft_normal;
        private Font m_ft_bold;
        private Font m_ft_italic;
        private Font m_ft_bold_italic;
        private SolidBrush m_brush;
        private Graphics m_g_onpaint;
        private Graphics m_g_private;
        private Control m_ctrl;
        private int m_nCounter;

        private readonly Dictionary<char, int> m_measure_cache = new Dictionary<char, int>();
        private readonly Dictionary<string, int> m_dic_width_cache = new Dictionary<string, int>();

        public STTextBoxGDIRender() {
            this.TabSize = 4;
            this.m_brush = new SolidBrush(Color.Black);
            this.InVisableCharColor = Color.FromArgb(50, Color.Magenta);
            this.ShowInVisableChar = false;
        }

        public void BindControl(Control ctrl) {
            if (ctrl == null) throw new ArgumentNullException("ctrl");
            m_ctrl = ctrl;
            m_ctrl.FontChanged += m_ctrl_FontChanged;
            this.InitData(m_ctrl.Font);
        }

        public void UnbindControl() {
            if (m_ctrl != null) {
                m_ctrl.FontChanged -= m_ctrl_FontChanged;
                m_ctrl = null;
            }
        }

        public void OnBeginPaint(Graphics g) {
            this.XDPIZoom = g.DpiX / 96f;
            this.YDPIZoom = g.DpiY / 96f;
            if (this.XDPIZoom < 1f) this.XDPIZoom = 1f;
            if (this.YDPIZoom < 1f) this.YDPIZoom = 1f;
            m_g_onpaint = g;
        }

        public void OnEndPaint(Graphics g) {
            m_g_onpaint = null;
        }

        public void BeginPaint() {
            if (m_nCounter == 0 && m_ctrl != null) {
                m_g_private = m_ctrl.CreateGraphics();
            }
            if (m_g_private != null) {
                this.XDPIZoom = m_g_private.DpiX / 96f;
                this.YDPIZoom = m_g_private.DpiY / 96f;
            }
            m_nCounter++;
        }

        public void EndPaint() {
            if (--m_nCounter == 0 && m_g_private != null) {
                m_g_private.Dispose();
                m_g_private = null;
            }
        }

        public int SetTabSize(int nSize) {
            if (nSize < 0 || nSize > 10) throw new ArgumentOutOfRangeException("nSize", "The value must be between 0 and 10");
            int old = this.TabSize;
            this.TabSize = nSize;
            return old;
        }

        public int GetTabSize() => this.TabSize;
        public int GetFontHeight() => this.FontHeight;
        public int GetSpaceWidth() => this.SpaceWidth;

        public int GetTabWidth(int nLeftWidth) {
            return (int)Math.Round(this.GetSpaceWidth() * this.GetTabSpaceCount(nLeftWidth, this.TabSize));
        }

        public float GetTabSpaceCount(int nLeftWidth, int nTabSize) {
            if (nTabSize < 0) throw new ArgumentOutOfRangeException("nTabSize");
            int nSpaceWidth = this.GetSpaceWidth();
            if (nTabSize == 1) return nSpaceWidth;
            int nTabFullWidth = nSpaceWidth * nTabSize;
            float nSpaceCount = (nTabFullWidth - (nLeftWidth % nTabFullWidth)) / (float)nSpaceWidth;
            return nSpaceCount < 1 ? nSpaceCount + nTabSize : nSpaceCount;
        }

        public int GetStringWidth(string strText, TextStyle style, int nLeftWidth) {
            if (string.IsNullOrEmpty(strText) || m_ft_normal == null) return 0;
            if (strText == "\t") return this.GetTabWidth(nLeftWidth);

            // Single character fast cache
            if (strText.Length == 1 && (style.FontStyle == FontStyle.Regular || style.FontStyle == 0)) {
                char ch = strText[0];
                if (m_measure_cache.TryGetValue(ch, out int w)) return w;
                w = MeasureChar(ch, m_ft_normal);
                m_measure_cache[ch] = w;
                return w;
            }

            string flag = GetFontFlag(style);
            string key = flag + ":" + strText;
            if (m_dic_width_cache.TryGetValue(key, out int cachedWidth)) return cachedWidth;

            Font ft = GetFontFromFlag(flag);
            int width = MeasureStringWidth(strText, ft);
            m_dic_width_cache[key] = width;
            return width;
        }

        public float GetStringWidthFloat(string strText, TextStyle style, float fLeftWidth) {
            return (float)this.GetStringWidth(strText, style, (int)fLeftWidth);
        }

        public void SetClip(Rectangle rect) {
            GetGraphics().SetClip(rect);
        }

        public void ResetClip() {
            GetGraphics().ResetClip();
        }

        public void DrawImage(Image img, Rectangle rect) {
            GetGraphics().DrawImage(img, rect);
        }

        public void DrawString(string strText, Font ft, Color color, Rectangle rect, StringFormat sf) {
            if (string.IsNullOrEmpty(strText)) return;
            TextRenderer.DrawText(GetGraphics(), strText, ft ?? m_ft_normal, rect.Location, color, TEXT_FORMAT_FLAGS);
        }

        public void DrawString(string strText, TextStyle style, RectangleF rect) {
            this.DrawString(strText, style, Rectangle.Round(rect));
        }

        public void DrawString(string strText, TextStyle style, Rectangle rect) {
            if (string.IsNullOrEmpty(strText)) return;
            Graphics g = GetGraphics();
            if (style.BackColor.A > 0) {
                m_brush.Color = style.BackColor;
                g.FillRectangle(m_brush, rect);
            }
            if ((style.FontStyle & FontStyle.Underline) == FontStyle.Underline && style.UnderLineColor.A > 0) {
                m_brush.Color = style.UnderLineColor;
                g.FillRectangle(m_brush, rect.X, rect.Bottom - 1, rect.Width, 1);
            }
            if (style.ForeColor.A > 0) {
                if (strText.Length == 1) {
                    switch (strText[0]) {
                        case ' ':
                            if (this.ShowInVisableChar) OnDrawSpaceChar(rect);
                            return;
                        case '\t':
                            if (this.ShowInVisableChar) OnDrawTabChar(rect);
                            return;
                        case '\r':
                        case '\n':
                            if (this.ShowInVisableChar) OnDrawNewLineChar(rect);
                            return;
                    }
                }
                Font ft = GetFontFromFlag(GetFontFlag(style));
                TextRenderer.DrawText(g, strText, ft, rect.Location, style.ForeColor, TEXT_FORMAT_FLAGS);
            }
            if ((style.FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout && style.StrikeOutColor.A > 0) {
                m_brush.Color = style.StrikeOutColor;
                g.FillRectangle(m_brush, rect.X, rect.Y + (rect.Height / 2), rect.Width, 1);
            }
        }

        public void FillRectangle(Color backColor, Rectangle rect) {
            if (backColor.A == 0 || rect.Width <= 0 || rect.Height <= 0) return;
            m_brush.Color = backColor;
            GetGraphics().FillRectangle(m_brush, rect);
        }

        public void FillRectangle(Color backColor, int nX, int nY, int nWidth, int nHeight) {
            if (backColor.A == 0 || nWidth <= 0 || nHeight <= 0) return;
            m_brush.Color = backColor;
            GetGraphics().FillRectangle(m_brush, nX, nY, nWidth, nHeight);
        }

        private int MeasureChar(char c, Font ft) {
            Graphics g = null;
            try {
                g = GetGraphics();
            } catch { }

            if (g != null) {
                return TextRenderer.MeasureText(g, c.ToString(), ft, new Size(int.MaxValue, int.MaxValue), TEXT_FORMAT_FLAGS).Width;
            }
            return TextRenderer.MeasureText(c.ToString(), ft, new Size(int.MaxValue, int.MaxValue), TEXT_FORMAT_FLAGS).Width;
        }

        private int MeasureStringWidth(string text, Font ft) {
            Graphics g = null;
            try {
                g = GetGraphics();
            } catch { }

            if (g != null) {
                return TextRenderer.MeasureText(g, text, ft, new Size(int.MaxValue, int.MaxValue), TEXT_FORMAT_FLAGS).Width;
            }
            return TextRenderer.MeasureText(text, ft, new Size(int.MaxValue, int.MaxValue), TEXT_FORMAT_FLAGS).Width;
        }

        protected virtual void OnDrawNewLineChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            g.FillRectangle(m_brush, rect.X + 1, rect.Y + rect.Height / 2 - 1, 1, 3);
            g.FillRectangle(m_brush, rect.X + 2, rect.Y + rect.Height / 2, Math.Max(1, rect.Width - 4), 1);
            g.FillRectangle(m_brush, rect.Right - 2, rect.Y + rect.Height / 2 - 4, 1, 5);
        }

        protected virtual void OnDrawTabChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            g.FillRectangle(m_brush, rect.X + 1, rect.Y + rect.Height / 2 - 1, 1, 3);
            g.FillRectangle(m_brush, rect.Right - 2, rect.Y + rect.Height / 2 - 1, 1, 3);
            g.FillRectangle(m_brush, rect.X + 2, rect.Y + rect.Height / 2, Math.Max(1, rect.Width - 4), 1);
        }

        protected virtual void OnDrawSpaceChar(Rectangle rect) {
            Graphics g = this.GetGraphics();
            m_brush.Color = this.InVisableCharColor;
            g.FillRectangle(m_brush, rect.X + 1, rect.Bottom - 2, Math.Max(1, rect.Width - 2), 1);
        }

        private void m_ctrl_FontChanged(object sender, EventArgs e) {
            this.InitData(m_ctrl.Font);
        }

        private void InitData(Font ft) {
            if (m_ft_normal == ft) return;
            this.BeginPaint();
            m_ft_normal = ft;
            if (m_ft_bold != null) m_ft_bold.Dispose();
            m_ft_bold = new Font(ft, FontStyle.Bold);
            if (m_ft_italic != null) m_ft_italic.Dispose();
            m_ft_italic = new Font(ft, FontStyle.Italic);
            if (m_ft_bold_italic != null) m_ft_bold_italic.Dispose();
            m_ft_bold_italic = new Font(ft, FontStyle.Bold | FontStyle.Italic);
            this.FontHeight = ft.Height;
            m_dic_width_cache.Clear();
            m_measure_cache.Clear();

            int nWidth = MeasureStringWidth(" ", ft);
            if (nWidth <= 0) nWidth = 8;
            this.SpaceWidth = nWidth;
            m_measure_cache[' '] = nWidth;
            m_measure_cache['\r'] = 0;
            m_measure_cache['\n'] = 0;

            int iWidth = MeasureStringWidth("i", ft);
            int wWidth = MeasureStringWidth("W", ft);
            this.IsMonospaced = (iWidth == wWidth);
            this.EndPaint();
        }

        private Graphics GetGraphics() {
            if (m_g_onpaint != null) return m_g_onpaint;
            if (m_g_private != null) return m_g_private;
            throw new InvalidOperationException("The Graphics is null, please call BeginPaint() to initialize it.");
        }

        private string GetFontFlag(TextStyle style) {
            string strKey = "N";
            if ((style.FontStyle & FontStyle.Bold) == FontStyle.Bold) strKey += "B";
            if ((style.FontStyle & FontStyle.Italic) == FontStyle.Italic) strKey += "I";
            return strKey;
        }

        private Font GetFontFromFlag(string strFlag) {
            switch (strFlag) {
                case "NBI": return m_ft_bold_italic;
                case "NB": return m_ft_bold;
                case "NI": return m_ft_italic;
            }
            return m_ft_normal;
        }

        public void Dispose() {
            if (m_g_private != null) {
                m_g_private.Dispose();
                m_g_private = null;
            }
            if (m_ft_bold != null) m_ft_bold.Dispose();
            if (m_ft_italic != null) m_ft_italic.Dispose();
            if (m_ft_bold_italic != null) m_ft_bold_italic.Dispose();
            if (m_brush != null) m_brush.Dispose();
            m_measure_cache.Clear();
            m_dic_width_cache.Clear();
        }
    }
}
