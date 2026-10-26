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
        public float CharWidthFloat { get; private set; }

        private Font m_ft_normal;
        private Font m_ft_bold;
        private Font m_ft_italic;
        private Font m_ft_bold_italic;
        private StringFormat m_sf;
        private SolidBrush m_brush;
        private Graphics m_g_onpaint;
        private Graphics m_g_private;
        private Bitmap m_bmp_measure;
        private Graphics m_g_measure;
        private float m_italic_width;

        private Rectangle m_rect_temp;
        private Rectangle m_rect_line;
        private Dictionary<string, int> m_dic_width_cache;
        private readonly Dictionary<char, float> m_measure_cache = new Dictionary<char, float>();
        private readonly Dictionary<string, float> m_dic_width_cache_float = new Dictionary<string, float>();
        private readonly float[] m_ascii_widths = new float[128];

        private Control m_ctrl;
        private int m_nCounter;

        public STTextBoxGDIPRender() {
            this.TabSize = 4;
            m_dic_width_cache = new Dictionary<string, int>();
            m_sf = new StringFormat(StringFormat.GenericTypographic);
            m_sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
            m_sf.Alignment = StringAlignment.Near;
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
            this.InitMeasureGraphics();
            this.InitData(m_ctrl.Font);
        }

        public void UnbindControl() {
            if (m_ctrl != null) {
                m_ctrl.FontChanged -= m_ctrl_FontChanged;
                m_ctrl = null;
            }
            if (m_g_measure != null) {
                m_g_measure.Dispose();
                m_g_measure = null;
            }
            if (m_bmp_measure != null) {
                m_bmp_measure.Dispose();
                m_bmp_measure = null;
            }
        }

        public void OnBeginPaint(Graphics g) {
            this.XDPIZoom = g.DpiX / 96f;
            this.YDPIZoom = g.DpiY / 96f;
            if (this.XDPIZoom < 1f) this.XDPIZoom = 1f;
            if (this.YDPIZoom < 1f) this.YDPIZoom = 1f;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            m_g_onpaint = g;
        }

        public void OnEndPaint(Graphics g) {
            m_g_onpaint = null;
        }

        public void BeginPaint() {
            if (m_g_measure == null && m_ctrl != null) {
                this.InitMeasureGraphics();
            }
            m_nCounter++;
        }

        public void EndPaint() {
            m_nCounter--;
        }

        private void InitMeasureGraphics() {
            if (m_g_measure != null) {
                m_g_measure.Dispose();
                m_g_measure = null;
            }
            if (m_bmp_measure != null) {
                m_bmp_measure.Dispose();
                m_bmp_measure = null;
            }
            m_bmp_measure = new Bitmap(1, 1);
            if (m_ctrl != null) {
                using (Graphics g = m_ctrl.CreateGraphics()) {
                    m_bmp_measure.SetResolution(g.DpiX, g.DpiY);
                    this.XDPIZoom = g.DpiX / 96f;
                    this.YDPIZoom = g.DpiY / 96f;
                    if (this.XDPIZoom < 1f) this.XDPIZoom = 1f;
                    if (this.YDPIZoom < 1f) this.YDPIZoom = 1f;
                }
            }
            m_g_measure = Graphics.FromImage(m_bmp_measure);
            m_g_measure.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
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
            return (int)Math.Round(this.GetTabWidthFloat(nLeftWidth));
        }

        public float GetTabWidthFloat(float fLeftWidth) {
            float fSpaceWidth = this.CharWidthFloat > 0 ? this.CharWidthFloat : (float)this.SpaceWidth;
            if (this.TabSize == 1) {
                return fSpaceWidth;
            }
            float fTabFullWidth = fSpaceWidth * this.TabSize;
            float fSpaceCount = (fTabFullWidth - (fLeftWidth % fTabFullWidth)) / fSpaceWidth;
            if (fSpaceCount < 1f) fSpaceCount += this.TabSize;
            return fSpaceCount * fSpaceWidth;
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
            return (int)Math.Ceiling(this.GetStringWidthFloat(strText, style, (float)nLeftWidth));
        }

        public float GetStringWidthFloat(string strText, TextStyle style, float fLeftWidth) {
            if (string.IsNullOrEmpty(strText) || m_ft_normal == null) {
                return 0f;
            }
            if (strText == "\t") {
                return this.GetTabWidthFloat(fLeftWidth);
            }
            bool isRegular = (style.FontStyle == FontStyle.Regular || style.FontStyle == 0);
            if (strText.Length == 1 && isRegular) {
                char ch = strText[0];
                if (ch < 128) {
                    return m_ascii_widths[ch];
                }
                if (m_measure_cache.TryGetValue(ch, out float cw)) return cw;
                Graphics g = this.GetGraphics();
                cw = g.MeasureString(strText, m_ft_normal, 10000, m_sf).Width;
                m_measure_cache[ch] = cw;
                return cw;
            }
            string strKey = ((int)style.FontStyle).ToString() + ":" + strText;
            if (m_dic_width_cache_float.TryGetValue(strKey, out float w)) {
                return w;
            }
            Graphics g2 = this.GetGraphics();
            Font ft = this.GetFont(style.FontStyle);
            float fWidth = g2.MeasureString(strText, ft, 10000, m_sf).Width;
            if ((ft.Style & FontStyle.Italic) == FontStyle.Italic) {
                fWidth += m_italic_width;
            }
            if (m_dic_width_cache_float.Count >= 1024) {
                m_dic_width_cache_float.Clear();
            }
            m_dic_width_cache_float[strKey] = fWidth;
            return fWidth;
        }

        public void DrawString(string strText, Font ft, Color color, Rectangle rect, StringFormat sf) {
            m_brush.Color = color;
            this.GetGraphics().DrawString(strText, ft, m_brush, rect, sf);
        }

        public void DrawString(string strText, TextStyle style, Rectangle rect) {
            this.DrawString(strText, style, (RectangleF)rect);
        }

        public void DrawString(string strText, TextStyle style, RectangleF rect) {
            Graphics g = this.GetGraphics();
            if (style.BackColor.A > 0) {
                m_brush.Color = style.BackColor;
                g.FillRectangle(m_brush, rect);
            }
            if ((style.FontStyle & FontStyle.Underline) == FontStyle.Underline && style.UnderLineColor.A > 0) {
                m_brush.Color = style.UnderLineColor;
                g.FillRectangle(m_brush, rect.X, rect.Bottom - m_rect_line.Height, rect.Width, (float)m_rect_line.Height);
            }
            if (style.ForeColor.A > 0) {
                if (m_brush.Color != style.ForeColor) {
                    m_brush.Color = style.ForeColor;
                }
                if (strText.Length == 1) {
                    Rectangle intRect = Rectangle.Round(rect);
                    switch (strText[0]) {
                        case ' ':
                            if (this.ShowInVisableChar) this.OnDrawSpaceChar(intRect);
                            return;
                        case '\t':
                            if (this.ShowInVisableChar) this.OnDrawTabChar(intRect);
                            return;
                        case '\r':
                        case '\n':
                            if (this.ShowInVisableChar) this.OnDrawNewLineChar(intRect);
                            return;
                    }
                }
                Font ft = this.GetFont(style.FontStyle);
                g.DrawString(strText, ft, m_brush, rect, m_sf);
            }
            if ((style.FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout && style.StrikeOutColor.A > 0) {
                m_brush.Color = style.StrikeOutColor;
                g.FillRectangle(m_brush, rect.X, rect.Y + rect.Height / 2f, rect.Width, (float)m_rect_line.Height);
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
            if (m_g_measure == null && m_ctrl != null) {
                this.InitMeasureGraphics();
            }
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
            m_dic_width_cache_float.Clear();
            m_measure_cache.Clear();

            Graphics g = this.GetGraphics();
            this.CharWidthFloat = g.MeasureString("W", ft, 10000, m_sf).Width;

            float fIWidth = g.MeasureString("i", ft, 10000, m_sf).Width;
            this.IsMonospaced = Math.Abs(fIWidth - this.CharWidthFloat) < 0.001f;
            if (!this.IsMonospaced) {
                m_italic_width = (float)(this.FontHeight * Math.Tan(12 * Math.PI / 180));
            } else {
                m_italic_width = 0;
            }

            // Pre-calculate ASCII lookup table for sub-nanosecond character measurement
            Array.Clear(m_ascii_widths, 0, m_ascii_widths.Length);
            for (char ch = ' '; ch <= '~'; ch++) {
                if (this.IsMonospaced) {
                    m_ascii_widths[ch] = this.CharWidthFloat;
                } else {
                    m_ascii_widths[ch] = g.MeasureString(ch.ToString(), ft, 10000, m_sf).Width;
                }
            }
            m_ascii_widths['\r'] = 0f;
            m_ascii_widths['\n'] = 0f;

            // SPACE are used to calculate TAB width.
            int nWidth = (int)Math.Ceiling(m_ascii_widths[' ']);
            if (nWidth <= 0) nWidth = 8;
            m_dic_width_cache.Add(" ", nWidth);
            m_dic_width_cache.Add("\r", 0);
            m_dic_width_cache.Add("\n", 0);
            m_dic_width_cache.Add("\r\n", 0);
            this.SpaceWidth = nWidth;
        }

        private Graphics GetGraphics() {
            if (m_g_onpaint != null) return m_g_onpaint;
            if (m_g_measure != null) return m_g_measure;
            if (m_ctrl != null) {
                this.InitMeasureGraphics();
                if (m_g_measure != null) return m_g_measure;
            }
            if (m_g_private != null) return m_g_private;
            throw new InvalidOperationException("The Graphics is null, please call BeginPaint() or bind a control to initialize it.");
        }

        private Font GetFont(FontStyle style) {
            bool bold = (style & FontStyle.Bold) != 0;
            bool italic = (style & FontStyle.Italic) != 0;
            if (bold && italic) return m_ft_bold_italic;
            if (bold) return m_ft_bold;
            if (italic) return m_ft_italic;
            return m_ft_normal;
        }

        // [interface] ==========================================

        public void Dispose() {
            if (m_g_measure != null) {
                m_g_measure.Dispose();
                m_g_measure = null;
            }
            if (m_bmp_measure != null) {
                m_bmp_measure.Dispose();
                m_bmp_measure = null;
            }
            if (m_g_private != null) {
                m_g_private.Dispose();
                m_g_private = null;
            }
            if (m_ft_bold != null) m_ft_bold.Dispose();
            if (m_ft_italic != null) m_ft_italic.Dispose();
            if (m_ft_bold_italic != null) m_ft_bold_italic.Dispose();
            if (m_brush != null) m_brush.Dispose();
            if (m_sf != null) m_sf.Dispose();
            if (m_dic_width_cache != null) m_dic_width_cache.Clear();
            if (m_dic_width_cache_float != null) m_dic_width_cache_float.Clear();
            if (m_measure_cache != null) m_measure_cache.Clear();
        }
    }
}
