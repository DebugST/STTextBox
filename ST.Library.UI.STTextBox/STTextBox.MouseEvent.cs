using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ST.Library.UI.STTextBox
{
    partial class STTextBox
    {
        private struct ScrollIncrement
        {
            public int Time;
            public int Value;

            public ScrollIncrement(int nTime, int nValue) {
                this.Time = nTime;
                this.Value = nValue;
            }
        }

        private DateTime m_dt_last_scroll_h = DateTime.Now;
        private DateTime m_dt_last_scroll_v = DateTime.Now;
        private ScrollIncrement[] m_arr_si = new ScrollIncrement[]{
            new ScrollIncrement(20, 6),
            new ScrollIncrement(30, 5),
            new ScrollIncrement(40, 4),
            new ScrollIncrement(50, 3),
            new ScrollIncrement(60, 2),
        };

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            var c = m_core;
            this.Select();
            if (c.Scroll.HoverScrollBar != STTextBoxScrollInfo.ScrollBarType.None) {
                if (c.Scroll.HBackRect.Contains(e.Location)) {
                    c.Scroll.DownScrollBar = STTextBoxScrollInfo.ScrollBarType.H;
                    c.ITextView.ScrollXToMuosePoint(e.X);
                    this.Invalidate();
                } else if (c.Scroll.VBackRect.Contains(e.Location)) {
                    c.Scroll.DownScrollBar = STTextBoxScrollInfo.ScrollBarType.V;
                    c.ITextView.ScrollYToMousePoint(e.Y);
                    this.Invalidate();
                }
                return;
            }

            m_last_sel_region = Rectangle.Empty;
            var fi = c.ITextView.SetCaretPostion(e.Location);// c.ITextView.FindFromPoint(e.Location);
            //c.Caret.CopyFromFindInfo(fi);
            if (Control.ModifierKeys != Keys.Shift) {
                c.Selection.SetIndex(c.Caret.IndexOfChar);
            } else {
                c.Selection.SetSelection(c.Selection.AnchorIndex, c.Caret.IndexOfChar);
            }
            this.Invalidate();
        }

        private Rectangle m_last_sel_region = Rectangle.Empty;

        public void InvalidateSelectionPixelYChange(int oldY, int newY) {
            var c = m_core;
            int minY = Math.Min(oldY, newY);
            int maxY = Math.Max(oldY, newY);
            int lineHeight = c.LineHeight;
            int spacing = c.TextBox.GetIntYSize(c.TextBox.LineSpacing);
            int startY = minY - 2;
            int height = (maxY - minY) + lineHeight + spacing + 4;
            startY = Math.Max(c.ViewRectangle.Top, startY);
            height = Math.Min(height, c.ViewRectangle.Bottom - startY);
            if (height <= 0) {
                this.Invalidate();
                return;
            }
            Rectangle rect = new Rectangle(c.ViewRectangle.Left, startY, c.ViewRectangle.Width, height);
            if (!m_last_sel_region.IsEmpty) {
                rect = Rectangle.Union(m_last_sel_region, rect);
            }
            m_last_sel_region = rect;
            this.Invalidate(rect);
        }

        public void InvalidateSelectionChange(int oldLine, int newLine) {
            var c = m_core;
            if (c.ITextView is WrapTextView) {
                var m = c.TextManager;
                int screenStart = c.ViewRectangle.Top;
                int screenEnd = c.ViewRectangle.Bottom;
                int minLine = Math.Min(oldLine, newLine);
                int maxLine = Math.Max(oldLine, newLine);
                if (minLine >= 0 && minLine < m.LineCount) {
                    var fi = c.ITextView.FindFromCharIndex(m[minLine].IndexOfFirstChar);
                    if (fi.Find) screenStart = fi.Location.Y;
                }
                if (maxLine >= 0 && maxLine < m.LineCount) {
                    var fi = c.ITextView.FindFromCharIndex(m[maxLine].IndexOfFirstChar + m[maxLine].RawString.Length);
                    if (fi.Find) screenEnd = fi.Location.Y + c.LineHeight;
                }
                InvalidateSelectionPixelYChange(screenStart, screenEnd);
                return;
            }
            int minL = Math.Min(oldLine, newLine);
            int maxL = Math.Max(oldLine, newLine);
            int lineHeight = c.LineHeight;
            Point ptTop = c.ITextView.ViewToControl(new Point(0, minL * lineHeight));
            int startY = ptTop.Y - 2;
            int spacing = c.TextBox.GetIntYSize(c.TextBox.LineSpacing);
            int height = ((maxL - minL + 1) * lineHeight) + spacing + 4;
            startY = Math.Max(c.ViewRectangle.Top, startY);
            height = Math.Min(height, c.ViewRectangle.Bottom - startY);
            if (height <= 0) {
                this.Invalidate();
                return;
            }
            Rectangle rect = new Rectangle(c.ViewRectangle.Left, startY, c.ViewRectangle.Width, height);
            if (!m_last_sel_region.IsEmpty) {
                rect = Rectangle.Union(m_last_sel_region, rect);
            }
            m_last_sel_region = rect;
            this.Invalidate(rect);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            var c = m_core;
            switch (c.Scroll.DownScrollBar) {
                case STTextBoxScrollInfo.ScrollBarType.V:
                    c.ITextView.ScrollYToMousePoint(e.Y);
                    this.Invalidate();
                    return;
                case STTextBoxScrollInfo.ScrollBarType.H:
                    c.ITextView.ScrollXToMuosePoint(e.X);
                    this.Invalidate();
                    return;
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Left) {
                int oldCaretY = c.Caret.Y;
                var fi = c.ITextView.SetCaretPostion(e.Location);
                if (!fi.Find) return;
                int nIndex = c.Caret.IndexOfChar;
                c.Selection.SetSelection(c.Selection.AnchorIndex, nIndex);
                bool scrolled = c.ITextView.ScrollToCaret();
                if (scrolled) {
                    m_last_sel_region = Rectangle.Empty;
                    this.Invalidate();
                } else {
                    this.InvalidateSelectionPixelYChange(oldCaretY, c.Caret.Y);
                }
                return;
            }
            if (e.Button == MouseButtons.None && c.Scroll.CountDown != 0) {
                if (c.Scroll.VBackRect.Contains(e.Location)) {
                    c.Scroll.HoverScrollBar = STTextBoxScrollInfo.ScrollBarType.V;
                } else if (c.Scroll.HBackRect.Contains(e.Location)) {
                    c.Scroll.HoverScrollBar = STTextBoxScrollInfo.ScrollBarType.H;
                } else {
                    c.Scroll.HoverScrollBar = STTextBoxScrollInfo.ScrollBarType.None;
                }
                if (c.Scroll.HoverScrollBar != STTextBoxScrollInfo.ScrollBarType.None) {
                    this.ShowScrollBar(c.Scroll.DisplayTime);
                    if (this.SetCursor(Cursors.Arrow)) {
                        this.Invalidate();
                    }
                } else {
                    if (this.SetCursor(Cursors.IBeam)) {
                        this.Invalidate();
                    }
                }
            } else {
                c.ITextView.OnSetCursor(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            m_last_sel_region = Rectangle.Empty;
            var c = m_core;
            c.Scroll.DownScrollBar = STTextBoxScrollInfo.ScrollBarType.None;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            var c = m_core;
            c.Scroll.HoverScrollBar = STTextBoxScrollInfo.ScrollBarType.None;
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);
            DateTime dt_now = DateTime.Now;
            //Console.WriteLine("Scroll: --------------------- " + dt_now.Subtract(m_dt_last_scroll_v).TotalMilliseconds);
            int nIncrement = 1;
            int nTemp = (int)dt_now.Subtract(m_dt_last_scroll_v).TotalMilliseconds;
            foreach (var v in m_arr_si) {
                if (nTemp < v.Time) {
                    nIncrement = v.Value;
                    break;
                }
            }
            m_dt_last_scroll_v = dt_now;
            var c = m_core;
            if (e.Delta > 0) {
                if (c.Scroll.YValue <= 0) return;
                if (c.Scroll.YValue - nIncrement < 0) {
                    nIncrement = c.Scroll.YValue;
                }
                c.Scroll.YValue -= nIncrement;
                c.Caret.Y += nIncrement * c.LineHeight;// this.Font.Height;
            } else {
                if (c.Scroll.YValue >= c.Scroll.MaxYValue) {
                    return;
                }
                if (c.Scroll.YValue + nIncrement > c.Scroll.MaxYValue) {
                    nIncrement = c.Scroll.MaxYValue - c.Scroll.YValue;
                }
                c.Scroll.YValue += nIncrement;
                c.Caret.Y -= c.LineHeight * nIncrement;// c.ITextRender.GetFontHeight();
            }
            c.ITextView.SetCaretPostion(c.Caret.IndexOfChar);
            //c.ITextView.SetCaretPostion(c.Caret.X, c.Caret.Y);
            this.ShowScrollBar(c.Scroll.DisplayTime);
            this.Invalidate();
            //c.Scroll.Timer = 5;
        }

        protected virtual void OnMouseHWheel(MouseEventArgs e) {
            DateTime dt_now = DateTime.Now;
            //Console.WriteLine("Scroll: --------------------- " + dt_now.Subtract(m_dt_last_scroll_h).TotalMilliseconds);
            int nIncrement = 1;
            int nTemp = (int)dt_now.Subtract(m_dt_last_scroll_h).TotalMilliseconds;
            foreach (var v in m_arr_si) {
                if (nTemp < v.Time) {
                    nIncrement = v.Value;
                    break;
                }
            }
            m_dt_last_scroll_h = dt_now;
            var c = m_core;
            if (e.Delta < 0) {
                if (c.Scroll.XValue <= 0) return;
                if (c.Scroll.XValue - nIncrement < 0) {
                    nIncrement = c.Scroll.XValue;
                }
                c.Scroll.XValue -= nIncrement;
                c.Caret.X += c.Scroll.XIncrement * nIncrement;
            } else {
                if (c.Scroll.XValue >= c.Scroll.MaxXValue) {
                    return;
                }
                if (c.Scroll.XValue + nIncrement > c.Scroll.MaxXValue) {
                    nIncrement = c.Scroll.MaxXValue - c.Scroll.XValue;
                }
                c.Scroll.XValue += nIncrement;
                c.Caret.X -= c.Scroll.XIncrement * nIncrement;
            }
            //c.ITextView.SetCaretPostion(c.Caret.X, c.Caret.Y);
            c.ITextView.SetCaretPostion(c.Caret.IndexOfChar);
            this.ShowScrollBar(c.Scroll.DisplayTime);
            this.Invalidate();
            //c.Scroll.Timer = 5;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e) {
            base.OnMouseDoubleClick(e);
            var c = m_core;
            var fi = c.ITextView.FindFromPoint(e.Location);
            int nIndex = fi.IndexOfChar - fi.Line.IndexOfFirstChar;
            int nSelectionStart = fi.IndexOfChar, nSelectionLen = 0;
            c.IWordSplitter.Each(fi.Line.RawString, (str, nStart, nLen) => {
                if (nIndex >= nStart && nIndex < nStart + nLen) {
                    switch (str[nStart]) {
                        case '\r':
                        case '\n':
                            return false;
                    }
                    nSelectionStart = fi.Line.IndexOfFirstChar + nStart;
                    nSelectionLen = nLen;
                    return false;
                }
                nSelectionStart = fi.Line.IndexOfFirstChar + nStart;
                nSelectionLen = nLen;
                return true;
            });
            c.Selection.SetSelection(nSelectionStart, nSelectionStart + nSelectionLen);
            c.Caret.IndexOfChar = c.Selection.EndIndex;
            c.ITextView.SetCaretPostion(c.Caret.IndexOfChar);
            this.Invalidate();
        }
    }
}
