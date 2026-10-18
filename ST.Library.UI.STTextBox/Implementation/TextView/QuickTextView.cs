using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ST.Library.UI.STTextBox
{
    /// <summary>
    /// Ultra-fast text view implementing techniques from QuickTextboxPrototype:
    /// - Cached line character X positions (instant hit testing without font measurement)
    /// - Chunk-based text rendering (unselected / selected / suffix in batched draw calls)
    /// - High-contrast dual-color selection (contrasting text on accent selection background)
    /// - Binary search hit-testing during selection drag
    /// </summary>
    public class QuickTextView : TextView
    {
        private const int LINE_OVERLAP_MARGIN = 1;
        private int m_nMaxLineWidth;
        private readonly Dictionary<TextLine, float[]> m_dic_line_widths = new Dictionary<TextLine, float[]>();
        private readonly Dictionary<char, float> m_dic_char_widths = new Dictionary<char, float>();

        private readonly StringFormat m_sf = new StringFormat(StringFormat.GenericTypographic) {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        public QuickTextView() {
            this.ShowLineNumber = true;
        }

        public override void Init(STTextBox.Core core) {
            base.Init(core);
            if (core != null && core.TextBox != null) {
                core.TextBox.FontChanged += (s, e) => {
                    m_dic_line_widths.Clear();
                    m_dic_char_widths.Clear();
                };
            }
        }

        public override void OnInitAllText() {
            m_dic_line_widths.Clear();
            m_dic_char_widths.Clear();
            m_nMaxLineWidth = 0;
            var c = base.Core;
            if (c == null) return;
            float mono = c.ITextBoxRender.CharWidthFloat;
            if (c.ITextBoxRender.IsMonospaced && mono > 0f) {
                int maxLen = 0;
                for (int i = 0; i < c.TextManager.LineCount; i++) {
                    int len = c.TextManager[i].RawString.Length;
                    if (len > maxLen) maxLen = len;
                }
                m_nMaxLineWidth = (int)Math.Ceiling(maxLen * mono);
            }
        }

        public override void OnLineAdded(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            // Lazy caching: line will be measured on demand
        }

        public override void OnLineRemoved(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            m_dic_line_widths.Remove(e.Line);
        }

        public override void OnLineChanged(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            m_dic_line_widths.Remove(e.Line);
        }

        public override int GetCurrentCharOffset() {
            var c = base.Core;
            return c.Caret.X - base.TextRectangle.X + c.Scroll.XOffset;
        }

        public override int GetLineIndexFromYScroll(int nYValue) {
            return nYValue;
        }

        public override FindInfo FindFromPoint(Point pt) {
            var c = base.Core;
            FindInfo fi = new FindInfo();
            int nIndexLine = c.Scroll.YValue + ((pt.Y - base.TextRectangle.Y) / c.LineHeight);
            if (nIndexLine >= c.TextManager.LineCount) {
                nIndexLine = c.TextManager.LineCount - 1;
            } else if (nIndexLine < 0) {
                nIndexLine = 0;
            }

            TextLine line = c.TextManager[nIndexLine];
            fi.Line = line;
            fi.IndexOfLine = nIndexLine;
            float[] widths = GetOrCreateLineWidthCache(line, line.RawString);
            float targetX = (float)(pt.X - base.TextRectangle.Left + c.Scroll.XOffset);

            // Fast binary search for column
            int col = 0;
            int low = 0, high = widths.Length - 1;
            while (low <= high) {
                int mid = (low + high) >> 1;
                if (widths[mid] <= targetX) {
                    col = mid;
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }
            if (col < widths.Length - 1) {
                float charWidth = widths[col + 1] - widths[col];
                if (targetX - widths[col] > (charWidth * 0.5f)) {
                    col++;
                }
            }

            // Trim newline characters from caret landing position
            string raw = line.RawString;
            while (col > 0 && col <= raw.Length && (raw[col - 1] == '\r' || raw[col - 1] == '\n')) {
                col--;
            }

            fi.IndexOfCharInLine = col;
            int finalX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[col]);
            int finalY = base.TextRectangle.Y + (nIndexLine - c.Scroll.YValue) * c.LineHeight;
            fi.Location = new Point(finalX, finalY);
            fi.Find = true;
            return fi;
        }

        public override FindInfo FindFromCharIndex(int nIndex) {
            var c = base.Core;
            int nLineIndex = c.TextManager.GetLineIndexFromCharIndex(nIndex);
            if (nLineIndex < 0 || nLineIndex >= c.TextManager.LineCount) {
                return new FindInfo();
            }
            TextLine line = c.TextManager[nLineIndex];
            int col = nIndex - line.IndexOfFirstChar;
            float[] widths = GetOrCreateLineWidthCache(line, line.RawString);
            int colClamped = Math.Max(0, Math.Min(col, widths.Length - 1));
            int x = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[colClamped]);
            int y = base.TextRectangle.Y + (nLineIndex - c.Scroll.YValue) * c.LineHeight;
            return new FindInfo() {
                Line = line,
                IndexOfLine = nLineIndex,
                IndexOfCharInLine = col,
                Location = new Point(x, y),
                Find = true
            };
        }

        public override void SetCaretPostion(int nCharIndex) {
            var fi = this.FindFromCharIndex(nCharIndex);
            if (fi.Line == null) return;
            base.Core.Caret.IndexOfLine = fi.IndexOfLine;
            base.Core.Caret.IndexOfChar = nCharIndex;
            this.SetCaretPostion(fi.Location.X, fi.Location.Y);
        }

        protected override void OnDrawLineSelectionBackground(ISTTextBoxRender render, TextLine line, int nLineIndex, STTextBoxSelectionInfo selection, int nY) {
            var c = base.Core;
            int lineStart = line.IndexOfFirstChar;
            int lineLen = line.RawString.Length;
            int lineEnd = lineStart + lineLen;
            if (lineStart >= selection.EndIndex || lineEnd <= selection.StartIndex) return;

            int selStartCol = Math.Max(0, selection.StartIndex - lineStart);
            int selEndCol = Math.Min(lineLen, selection.EndIndex - lineStart);
            if (selEndCol <= selStartCol) return;

            float[] widths = GetOrCreateLineWidthCache(line, line.RawString);
            int selStartX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[selStartCol]);
            int selEndX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[selEndCol]);
            int selWidth = selEndX - selStartX;
            if (selWidth <= 0) return;

            Color selColor = c.TextBox.SelectionColor.A == 0 ? Color.FromArgb(0, 120, 215) : c.TextBox.SelectionColor;
            render.FillRectangle(selColor, selStartX, nY, selWidth, c.LineHeight + LINE_OVERLAP_MARGIN);
        }

        protected override int OnDrawLine(ISTTextBoxRender render, TextLine line, int nX, int nY) {
            var c = base.Core;
            string raw = line.RawString;
            if (string.IsNullOrEmpty(raw)) return c.LineHeight;

            // Trim newline characters from drawing string
            int drawLength = raw.Length;
            while (drawLength > 0 && (raw[drawLength - 1] == '\r' || raw[drawLength - 1] == '\n')) {
                drawLength--;
            }
            if (drawLength == 0) return c.LineHeight;

            float[] widths = GetOrCreateLineWidthCache(line, raw);

            int lineStart = line.IndexOfFirstChar;
            int lineEnd = lineStart + drawLength;

            bool isSelected = !c.Selection.IsEmptySelection && !(lineStart >= c.Selection.EndIndex || lineEnd <= c.Selection.StartIndex);

            float baseX = (float)(nX - c.Scroll.XOffset);

            if (!isSelected) {
                // Entire line unselected: Draw as single chunk
                string text = raw.Substring(0, drawLength);
                DrawTextChunk(render, text, baseX, nY, widths[drawLength], false);
                return c.LineHeight;
            }

            int selStartCol = Math.Max(0, Math.Min(drawLength, c.Selection.StartIndex - lineStart));
            int selEndCol = Math.Max(0, Math.Min(drawLength, c.Selection.EndIndex - lineStart));

            // Fully selected line
            if (selStartCol == 0 && selEndCol == drawLength) {
                string text = raw.Substring(0, drawLength);
                DrawTextChunk(render, text, baseX, nY, widths[drawLength], true);
                return c.LineHeight;
            }

            // Partially selected line:
            // Use dual-pass clip rendering to eliminate GDI+ subpixel character rearrangement / glyph jitter.
            // Both passes draw from the exact identical line origin (baseX), ensuring ClearType grid-fitting
            // never shifts glyphs horizontally when text transitions between unselected and selected states.
            string fullText = raw.Substring(0, drawLength);
            DrawTextChunk(render, fullText, baseX, nY, widths[drawLength], false);

            if (selEndCol > selStartCol) {
                Color selTextColor = (c.TextBox.SelectedTextColor != Color.Empty) ? c.TextBox.SelectedTextColor : Color.White;
                if (selTextColor != c.TextBox.ForeColor) {
                    int selStartX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[selStartCol]);
                    int selEndX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(widths[selEndCol]);
                    int selWidth = selEndX - selStartX;
                    if (selWidth > 0) {
                        Rectangle selClip = new Rectangle(selStartX, nY, selWidth, c.LineHeight + LINE_OVERLAP_MARGIN);
                        selClip.Intersect(base.TextRectangle);
                        if (selClip.Width > 0 && selClip.Height > 0) {
                            render.SetClip(selClip);
                            DrawTextChunk(render, fullText, baseX, nY, widths[drawLength], true);
                            render.SetClip(base.TextRectangle);
                        }
                    }
                }
            }

            return c.LineHeight;
        }

        private void DrawTextChunk(ISTTextBoxRender render, string text, float x, float y, float width, bool isSelected) {
            if (string.IsNullOrEmpty(text) || width <= 0f) return;
            var c = base.Core;
            TextStyle style = new TextStyle();
            if (isSelected) {
                style.ForeColor = (c.TextBox.SelectedTextColor != Color.Empty) ? c.TextBox.SelectedTextColor : Color.White;
            } else {
                style.ForeColor = c.TextBox.ForeColor;
            }
            render.DrawString(text, style, new RectangleF(x, y, width, (float)c.LineHeight));
        }

        protected override void OnDrawHead(ISTTextBoxRender render, int nStartLineIndex) {
            render.FillRectangle(base.HeadBackgroundColor, base.HeadRectangle);
            var c = base.Core;
            Rectangle rect_linenumber = new Rectangle(base.HeadRectangle.X, base.HeadRectangle.Y, base.LineNumberWidth - c.TextBox.GetIntXSize(10), c.LineHeight);
            for (int i = nStartLineIndex; i < c.TextManager.LineCount; i++) {
                if (base.ShowLineNumber) {
                    render.DrawString((i + 1).ToString(), c.TextBox.Font, base.LineNumberColor, rect_linenumber, m_sf);
                }
                rect_linenumber.Y += c.LineHeight;
                if (rect_linenumber.Y >= base.HeadRectangle.Bottom) break;
            }
        }

        public override void OnCalcScroll(STTextBoxScrollInfo scrollInfo) {
            var c = base.Core;
            c.Scroll.MaxYValue = c.TextManager.LineCount - base.TextRectangle.Height / c.LineHeight + 2;
            if (c.Scroll.MaxYValue < 0) c.Scroll.MaxYValue = 0;
            c.Scroll.MaxXValue = (m_nMaxLineWidth - base.TextRectangle.Width) / c.Scroll.XIncrement + 2;
            if (c.Scroll.MaxXValue < 0) c.Scroll.MaxXValue = 0;
        }

        private float[] GetOrCreateLineWidthCache(TextLine line, string text) {
            if (m_dic_line_widths.TryGetValue(line, out float[] cached)) {
                return cached;
            }
            float[] widths = new float[text.Length + 1];
            float currentWidth = 0f;
            var c = base.Core;
            float monoAdvance = c.ITextBoxRender.CharWidthFloat;
            bool isMono = c.ITextBoxRender.IsMonospaced && monoAdvance > 0f;

            // Fast ASCII check: if all characters are regular ASCII, we can use fast mono advance
            bool isPureAsciiNoTabs = isMono;
            if (isPureAsciiNoTabs) {
                for (int i = 0; i < text.Length; i++) {
                    char ch = text[i];
                    if (ch < 32 || ch > 126) {
                        if (ch != '\r' && ch != '\n') {
                            isPureAsciiNoTabs = false;
                            break;
                        }
                    }
                }
            }

            if (isPureAsciiNoTabs) {
                for (int i = 0; i < text.Length; i++) {
                    char ch = text[i];
                    if (ch == '\r' || ch == '\n') {
                        widths[i + 1] = currentWidth;
                    } else {
                        currentWidth += monoAdvance;
                        widths[i + 1] = currentWidth;
                    }
                }
            } else {
                for (int i = 0; i < text.Length; i++) {
                    char ch = text[i];
                    if (ch == '\r' || ch == '\n') {
                        widths[i + 1] = currentWidth;
                        continue;
                    }
                    if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                        string surrogateStr = text.Substring(i, 2);
                        float sw;
                        if (!m_dic_char_widths.TryGetValue(ch, out sw)) {
                            sw = c.GetStringWidthFloat(surrogateStr, TextStyle.Empty, currentWidth);
                            m_dic_char_widths[ch] = sw;
                        }
                        currentWidth += sw;
                        widths[i + 1] = currentWidth;
                        widths[i + 2] = currentWidth;
                        i++;
                        continue;
                    }
                    float w;
                    if (isMono && ch >= 32 && ch <= 126) {
                        w = monoAdvance;
                    } else if (ch == '\t') {
                        w = c.GetStringWidthFloat("\t", TextStyle.Empty, currentWidth);
                    } else {
                        if (!m_dic_char_widths.TryGetValue(ch, out w)) {
                            w = c.GetStringWidthFloat(ch.ToString(), TextStyle.Empty, currentWidth);
                            m_dic_char_widths[ch] = w;
                        }
                    }
                    currentWidth += w;
                    widths[i + 1] = currentWidth;
                }
            }
            int intTotalWidth = (int)Math.Ceiling(currentWidth);
            if (intTotalWidth > m_nMaxLineWidth) {
                m_nMaxLineWidth = intTotalWidth;
            }
            m_dic_line_widths[line] = widths;
            return widths;
        }
    }
}
