using System;
using System.Collections.Generic;
using System.Drawing;

namespace ST.Library.UI.STTextBox
{
    /// <summary>
    /// Standard non-wrapping text view for STTextBox.
    /// Optimized with cached character offsets, fast binary-search hit testing,
    /// dual-pass clip rendering for jitter-free selection, and batched runs for emojis/tabs.
    /// </summary>
    public class NoWrapTextView : TextView
    {
        private const int LINE_OVERLAP_MARGIN = 1;
        private const int MAX_OFFSETS_CACHE = 512;
        private int m_nMaxLineWidth;
        private readonly Dictionary<TextLine, int[]> m_dic_line_offsets = new Dictionary<TextLine, int[]>();
        private readonly Queue<TextLine> m_offsets_lru = new Queue<TextLine>();
        private readonly Dictionary<char, float> m_dic_char_widths = new Dictionary<char, float>();

        private readonly StringFormat m_sf = new StringFormat(StringFormat.GenericTypographic) {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        public override void Init(STTextBox.Core core) {
            base.Init(core);
            m_dic_line_offsets.Clear();
            m_offsets_lru.Clear();
            m_dic_char_widths.Clear();
            m_nMaxLineWidth = 0;
            if (core != null && core.TextBox != null) {
                core.TextBox.FontChanged += (s, e) => {
                    m_dic_line_offsets.Clear();
                    m_offsets_lru.Clear();
                    m_dic_char_widths.Clear();
                    m_nMaxLineWidth = 0;
                };
            }
        }

        public override void OnInitAllText() {
            m_dic_line_offsets.Clear();
            m_offsets_lru.Clear();
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
            m_dic_line_offsets.Remove(e.Line);
        }

        public override void OnLineChanged(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            m_dic_line_offsets.Remove(e.Line);
        }

        public override int GetCurrentCharOffset() {
            var c = base.Core;
            return c.Caret.X - base.TextRectangle.X + c.Scroll.XOffset;
        }

        private bool CanUseFastMono(out float monoAdvance) {
            var c = base.Core;
            monoAdvance = 0f;
            if (c == null) return false;
            if (!c.ITextBoxRender.IsMonospaced) return false;
            if (c.TextBox.CharSpacing != 0) return false;
            monoAdvance = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
            return monoAdvance > 0f;
        }

        private static bool IsPureAsciiNoTabs(string text) {
            if (string.IsNullOrEmpty(text)) return true;
            for (int i = 0; i < text.Length; i++) {
                char ch = text[i];
                if (ch < 32 || ch > 126) {
                    if (ch != '\r' && ch != '\n') return false;
                }
            }
            return true;
        }

        private static int GetLineDrawLength(string raw) {
            if (string.IsNullOrEmpty(raw)) return 0;
            int len = raw.Length;
            while (len > 0 && (raw[len - 1] == '\r' || raw[len - 1] == '\n')) {
                len--;
            }
            return len;
        }

        private int[] GetOrCreateLineOffsets(TextLine line) {
            if (m_dic_line_offsets.TryGetValue(line, out int[] cached)) {
                return cached;
            }
            var c = base.Core;
            string text = line.RawString ?? string.Empty;
            int[] offsets = new int[text.Length + 1];
            offsets[0] = 0;

            bool isMono = c.ITextBoxRender.IsMonospaced;
            float monoAdvance = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
            float charSpacing = (float)c.TextBox.GetIntXSize(c.TextBox.CharSpacing);

            // Fast ASCII check: if all characters are regular ASCII, we can use fast mono advance
            bool isPureAsciiNoTabs = isMono && (charSpacing == 0f) && monoAdvance > 0f;
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

            float curF = 0f;
            if (isPureAsciiNoTabs) {
                for (int i = 0; i < text.Length; i++) {
                    char ch = text[i];
                    if (ch == '\r' || ch == '\n') {
                        offsets[i + 1] = (int)Math.Round(curF);
                    } else {
                        curF += monoAdvance;
                        offsets[i + 1] = (int)Math.Round(curF);
                    }
                }
            } else {
                c.IGraphemeSplitter.Each(text, (str, nStart, nLen) => {
                    string strChar = str.Substring(nStart, nLen);
                    float w;
                    if (c.IsEmoji(strChar)) {
                        w = (float)c.FontHeight;
                    } else if (nLen == 1 && strChar[0] == '\t') {
                        w = c.GetStringWidthFloat("\t", TextStyle.Empty, curF);
                    } else if (nLen == 1 && (strChar[0] == '\r' || strChar[0] == '\n')) {
                        w = 0f;
                    } else if (nLen == 1 && isMono && charSpacing == 0f && strChar[0] >= 32 && strChar[0] <= 126) {
                        w = monoAdvance;
                    } else {
                        if (nLen > 1 && strChar[nLen - 1] >= '\uFE00' && strChar[nLen - 1] <= '\uFE0F') {
                            strChar = str.Substring(nStart, nLen - 1);
                        }
                        if (nLen == 1 && m_dic_char_widths.TryGetValue(strChar[0], out float cachedW)) {
                            w = cachedW + charSpacing;
                        } else {
                            w = c.GetStringWidthFloat(strChar, c.GetStyleFromCharIndex(line.IndexOfFirstChar + nStart), curF) + charSpacing;
                            if (nLen == 1) {
                                m_dic_char_widths[strChar[0]] = w - charSpacing;
                            }
                        }
                    }
                    curF += w;
                    int off = (int)Math.Round(curF);
                    for (int k = 1; k <= nLen; k++) {
                        offsets[nStart + k] = off;
                    }
                    return true;
                });
            }

            int finalWidth = (int)Math.Round(curF);
            line.Tag = finalWidth;
            if (finalWidth > m_nMaxLineWidth) {
                m_nMaxLineWidth = finalWidth;
            }

            if (m_dic_line_offsets.Count >= MAX_OFFSETS_CACHE && m_offsets_lru.Count > 0) {
                TextLine oldLine = m_offsets_lru.Dequeue();
                m_dic_line_offsets.Remove(oldLine);
            }
            m_dic_line_offsets[line] = offsets;
            m_offsets_lru.Enqueue(line);
            return offsets;
        }

        public override FindInfo FindFromPoint(Point pt) {
            var c = base.Core;
            if (c.TextManager.LineCount == 0) return new FindInfo();

            int nIndexLine = c.Scroll.YValue + ((pt.Y - base.TextRectangle.Y) / c.LineHeight);
            if (nIndexLine >= c.TextManager.LineCount) nIndexLine = c.TextManager.LineCount - 1;
            if (nIndexLine < 0) nIndexLine = 0;

            TextLine line = c.TextManager[nIndexLine];
            if (line == null) return new FindInfo();

            if (CanUseFastMono(out float monoAdvance) && IsPureAsciiNoTabs(line.RawString)) {
                string raw = line.RawString ?? string.Empty;
                int drawLength = GetLineDrawLength(raw);
                int targetOffset = pt.X - base.TextRectangle.Left + c.Scroll.XOffset;
                int col = (int)Math.Round(targetOffset / monoAdvance);
                if (col < 0) col = 0;
                if (col > drawLength) col = drawLength;
                int x = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(col * monoAdvance);
                int y = base.TextRectangle.Y + (nIndexLine - c.Scroll.YValue) * c.LineHeight;
                return new FindInfo() {
                    Line = line,
                    IndexOfLine = nIndexLine,
                    IndexOfCharInLine = col,
                    Location = new Point(x, y),
                    Find = true
                };
            }

            int[] offsets = GetOrCreateLineOffsets(line);
            int targetOffsetComplex = pt.X - base.TextRectangle.Left + c.Scroll.XOffset;

            int colComplex = 0;
            int low = 0, high = offsets.Length - 1;
            while (low <= high) {
                int mid = (low + high) >> 1;
                if (offsets[mid] <= targetOffsetComplex) {
                    colComplex = mid;
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }
            if (colComplex < offsets.Length - 1) {
                int charWidth = offsets[colComplex + 1] - offsets[colComplex];
                if (charWidth > 0 && targetOffsetComplex - offsets[colComplex] >= charWidth / 2) {
                    colComplex++;
                }
            }

            // Do not position caret on or past trailing newlines of the line
            string text = line.RawString ?? string.Empty;
            int textLen = text.Length;
            while (textLen > 0 && (text[textLen - 1] == '\r' || text[textLen - 1] == '\n')) {
                textLen--;
            }
            if (colComplex > textLen) {
                colComplex = textLen;
            }

            int xComplex = base.TextRectangle.Left - c.Scroll.XOffset + offsets[colComplex];
            int yComplex = base.TextRectangle.Y + (nIndexLine - c.Scroll.YValue) * c.LineHeight;

            return new FindInfo() {
                Line = line,
                IndexOfLine = nIndexLine,
                IndexOfCharInLine = colComplex,
                Location = new Point(xComplex, yComplex),
                Find = true
            };
        }

        public override FindInfo FindFromCharIndex(int nIndex) {
            var c = base.Core;
            if (c.TextManager.LineCount == 0) return new FindInfo();
            int nIndexOfLine = c.TextManager.GetLineIndexFromCharIndex(nIndex);
            TextLine line = c.TextManager[nIndexOfLine];
            if (line == null) return new FindInfo();

            if (CanUseFastMono(out float monoAdvance) && IsPureAsciiNoTabs(line.RawString)) {
                string raw = line.RawString ?? string.Empty;
                int drawLength = GetLineDrawLength(raw);
                int col = Math.Max(0, Math.Min(nIndex - line.IndexOfFirstChar, drawLength));
                int x = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(col * monoAdvance);
                int y = base.TextRectangle.Y + (nIndexOfLine - c.Scroll.YValue) * c.LineHeight;
                return new FindInfo() {
                    Line = line,
                    IndexOfLine = nIndexOfLine,
                    IndexOfCharInLine = col,
                    Location = new Point(x, y),
                    Find = true
                };
            }

            int[] offsets = GetOrCreateLineOffsets(line);
            int colComplex = Math.Max(0, Math.Min(nIndex - line.IndexOfFirstChar, offsets.Length - 1));
            int xComplex = base.TextRectangle.Left - c.Scroll.XOffset + offsets[colComplex];
            int yComplex = base.TextRectangle.Y + (nIndexOfLine - c.Scroll.YValue) * c.LineHeight;
            return new FindInfo() {
                Line = line,
                IndexOfLine = nIndexOfLine,
                IndexOfCharInLine = colComplex,
                Location = new Point(xComplex, yComplex),
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

            if (CanUseFastMono(out float monoAdvance) && IsPureAsciiNoTabs(line.RawString)) {
                int selStartX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(selStartCol * monoAdvance);
                int selEndX = base.TextRectangle.Left - c.Scroll.XOffset + (int)Math.Round(selEndCol * monoAdvance);
                int selWidth = selEndX - selStartX;
                if (selWidth <= 0) return;

                Color selColor = c.TextBox.SelectionColor.A == 0 ? Color.FromArgb(0, 120, 215) : c.TextBox.SelectionColor;
                render.FillRectangle(selColor, selStartX, nY, selWidth, c.LineHeight + LINE_OVERLAP_MARGIN);
                return;
            }

            int[] offsets = GetOrCreateLineOffsets(line);
            int selStartXComplex = base.TextRectangle.Left - c.Scroll.XOffset + offsets[selStartCol];
            int selEndXComplex = base.TextRectangle.Left - c.Scroll.XOffset + offsets[selEndCol];
            int selWidthComplex = selEndXComplex - selStartXComplex;
            if (selWidthComplex <= 0) return;

            Color selColorComplex = c.TextBox.SelectionColor.A == 0 ? Color.FromArgb(0, 120, 215) : c.TextBox.SelectionColor;
            render.FillRectangle(selColorComplex, selStartXComplex, nY, selWidthComplex, c.LineHeight + LINE_OVERLAP_MARGIN);
        }

        protected override int OnDrawLine(ISTTextBoxRender render, TextLine line, int nX, int nY) {
            var c = base.Core;
            string raw = line.RawString;
            if (string.IsNullOrEmpty(raw)) return c.LineHeight;

            int drawLength = GetLineDrawLength(raw);
            if (drawLength == 0) return c.LineHeight;

            int baseX = nX - c.Scroll.XOffset;
            int lineStart = line.IndexOfFirstChar;
            int selStart = c.Selection.StartIndex;
            int selEnd = c.Selection.EndIndex;
            bool hasSelection = !c.Selection.IsEmptySelection && !(lineStart >= selEnd || (lineStart + drawLength) <= selStart);
            Color selTextColor = (c.TextBox.SelectedTextColor != Color.Empty) ? c.TextBox.SelectedTextColor : Color.White;
            int nLineSpacing = c.TextBox.GetIntYSize(c.TextBox.LineSpacing) / 2;
            int nCharSpacing = c.TextBox.GetIntXSize(c.TextBox.CharSpacing);

            if (CanUseFastMono(out float monoAdvance) && IsPureAsciiNoTabs(raw)) {
                int finalWidth = (int)Math.Round(drawLength * monoAdvance);
                line.Tag = finalWidth;
                if (finalWidth > m_nMaxLineWidth) m_nMaxLineWidth = finalWidth;

                void DrawRunsFast(bool isSelectionPass) {
                    int runStart = 0;
                    TextStyle syntaxRunStyle = TextStyle.Empty;
                    bool hasRun = false;

                    for (int i = 0; i < drawLength; i++) {
                        int charIdx = lineStart + i;
                        var syntaxStyle = c.GetStyleFromCharIndex(charIdx);
                        if (syntaxStyle.ForeColor.A == 0) syntaxStyle.ForeColor = c.TextBox.ForeColor;

                        if (!hasRun) {
                            runStart = i;
                            syntaxRunStyle = syntaxStyle;
                            hasRun = true;
                        } else if (syntaxRunStyle.ForeColor != syntaxStyle.ForeColor || syntaxRunStyle.BackColor != syntaxStyle.BackColor || syntaxRunStyle.FontStyle != syntaxStyle.FontStyle) {
                            int runLen = i - runStart;
                            if (runLen > 0) {
                                string runStr = raw.Substring(runStart, runLen);
                                int rStartX = baseX + (int)Math.Round(runStart * monoAdvance);
                                int rEndX = baseX + (int)Math.Round(i * monoAdvance);
                                TextStyle drawStyle = syntaxRunStyle;
                                if (isSelectionPass) {
                                    drawStyle.ForeColor = selTextColor;
                                    drawStyle.BackColor = Color.Transparent;
                                }
                                render.DrawString(runStr, drawStyle, new Rectangle(rStartX, nY, rEndX - rStartX, c.LineHeight));
                            }
                            runStart = i;
                            syntaxRunStyle = syntaxStyle;
                        }
                    }

                    if (hasRun && drawLength > runStart) {
                        int runLen = drawLength - runStart;
                        string runStr = raw.Substring(runStart, runLen);
                        int rStartX = baseX + (int)Math.Round(runStart * monoAdvance);
                        int rEndX = baseX + (int)Math.Round(drawLength * monoAdvance);
                        TextStyle drawStyle = syntaxRunStyle;
                        if (isSelectionPass) {
                            drawStyle.ForeColor = selTextColor;
                            drawStyle.BackColor = Color.Transparent;
                        }
                        render.DrawString(runStr, drawStyle, new Rectangle(rStartX, nY, rEndX - rStartX, c.LineHeight));
                    }
                }

                DrawRunsFast(false);

                if (hasSelection && selTextColor != Color.Empty) {
                    int selStartCol = Math.Max(0, Math.Min(drawLength, selStart - lineStart));
                    int selEndCol = Math.Max(0, Math.Min(drawLength, selEnd - lineStart));
                    if (selEndCol > selStartCol) {
                        int selStartX = baseX + (int)Math.Round(selStartCol * monoAdvance);
                        int selEndX = baseX + (int)Math.Round(selEndCol * monoAdvance);
                        int selWidth = selEndX - selStartX;
                        if (selWidth > 0) {
                            Rectangle selClip = new Rectangle(selStartX, nY, selWidth, c.LineHeight + LINE_OVERLAP_MARGIN);
                            selClip.Intersect(base.TextRectangle);
                            if (selClip.Width > 0 && selClip.Height > 0) {
                                render.SetClip(selClip);
                                DrawRunsFast(true);
                                render.SetClip(base.TextRectangle);
                            }
                        }
                    }
                }
                return c.LineHeight;
            }

            int[] offsets = GetOrCreateLineOffsets(line);

            void DrawRuns(bool isSelectionPass) {
                int runStart = 0;
                int runStartX = baseX;
                TextStyle syntaxRunStyle = TextStyle.Empty;
                bool hasRun = false;

                for (int i = 0; i < drawLength; i++) {
                    char ch = raw[i];
                    int charIdx = lineStart + i;

                    if (ch == '\t') {
                        if (hasRun && i > runStart) {
                            string runStr = raw.Substring(runStart, i - runStart);
                            int runWidth = offsets[i] - offsets[runStart];
                            TextStyle drawStyle = syntaxRunStyle;
                            if (isSelectionPass) {
                                drawStyle.ForeColor = selTextColor;
                                drawStyle.BackColor = Color.Transparent;
                            }
                            render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runWidth, c.LineHeight));
                            hasRun = false;
                        }
                        continue;
                    }

                    int emojiLen;
                    string emojiStr;
                    if (c.TryGetEmoji(raw, i, drawLength, out emojiLen, out emojiStr)) {
                        if (hasRun && i > runStart) {
                            string runStr = raw.Substring(runStart, i - runStart);
                            int runWidth = offsets[i] - offsets[runStart];
                            TextStyle drawStyle = syntaxRunStyle;
                            if (isSelectionPass) {
                                drawStyle.ForeColor = selTextColor;
                                drawStyle.BackColor = Color.Transparent;
                            }
                            render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runWidth, c.LineHeight));
                            hasRun = false;
                        }
                        if (c.IEmojiRender != null) {
                            c.IEmojiRender.DrawEmoji(render, emojiStr, baseX + offsets[i] + (nCharSpacing >> 1), nY + nLineSpacing, c.FontHeight, isSelectionPass);
                        }
                        i += emojiLen - 1;
                        continue;
                    }

                    var syntaxStyle = c.GetStyleFromCharIndex(charIdx);
                    if (syntaxStyle.ForeColor.A == 0) syntaxStyle.ForeColor = c.TextBox.ForeColor;

                    if (!hasRun) {
                        runStart = i;
                        runStartX = baseX + offsets[i];
                        syntaxRunStyle = syntaxStyle;
                        hasRun = true;
                    } else if (syntaxRunStyle.ForeColor != syntaxStyle.ForeColor || syntaxRunStyle.BackColor != syntaxStyle.BackColor || syntaxRunStyle.FontStyle != syntaxStyle.FontStyle) {
                        int runLen = i - runStart;
                        if (runLen > 0) {
                            string runStr = raw.Substring(runStart, runLen);
                            int runWidth = offsets[i] - offsets[runStart];
                            TextStyle drawStyle = syntaxRunStyle;
                            if (isSelectionPass) {
                                drawStyle.ForeColor = selTextColor;
                                drawStyle.BackColor = Color.Transparent;
                            }
                            render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runWidth, c.LineHeight));
                        }
                        runStart = i;
                        runStartX = baseX + offsets[i];
                        syntaxRunStyle = syntaxStyle;
                    }
                }

                if (hasRun && drawLength > runStart) {
                    int runLen = drawLength - runStart;
                    string runStr = raw.Substring(runStart, runLen);
                    int runWidth = offsets[drawLength] - offsets[runStart];
                    TextStyle drawStyle = syntaxRunStyle;
                    if (isSelectionPass) {
                        drawStyle.ForeColor = selTextColor;
                        drawStyle.BackColor = Color.Transparent;
                    }
                    render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runWidth, c.LineHeight));
                }
            }

            // Pass 1: Render line with normal syntax styles (stable token boundaries prevent GDI+ ClearType glyph shifting)
            DrawRuns(false);

            // Pass 2: If line intersects selection, use clip to re-render selection with selTextColor without re-chunking strings
            if (hasSelection && selTextColor != Color.Empty) {
                int selStartCol = Math.Max(0, Math.Min(drawLength, selStart - lineStart));
                int selEndCol = Math.Max(0, Math.Min(drawLength, selEnd - lineStart));
                if (selEndCol > selStartCol) {
                    int selStartX = baseX + offsets[selStartCol];
                    int selEndX = baseX + offsets[selEndCol];
                    int selWidth = selEndX - selStartX;
                    if (selWidth > 0) {
                        Rectangle selClip = new Rectangle(selStartX, nY, selWidth, c.LineHeight + LINE_OVERLAP_MARGIN);
                        selClip.Intersect(base.TextRectangle);
                        if (selClip.Width > 0 && selClip.Height > 0) {
                            render.SetClip(selClip);
                            DrawRuns(true);
                            render.SetClip(base.TextRectangle);
                        }
                    }
                }
            }

            return c.LineHeight;
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

        public override int GetLineIndexFromYScroll(int nYValue) {
            return nYValue;
        }

        private int _LineWidth(TextLine line) {
            if (line == null) return 0;
            int[] offs = GetOrCreateLineOffsets(line);
            return offs[offs.Length - 1];
        }

        public override void OnCalcScroll(STTextBoxScrollInfo scrollInfo) {
            var c = base.Core;
            c.Scroll.MaxYValue = c.TextManager.LineCount - base.TextRectangle.Height / c.LineHeight + 2;
            if (c.Scroll.MaxYValue < 0) {
                c.Scroll.MaxYValue = 0;
            }
            c.Scroll.MaxXValue = (m_nMaxLineWidth - base.TextRectangle.Width) / c.Scroll.XIncrement + 2;
            if (c.Scroll.MaxXValue < 0) {
                c.Scroll.MaxXValue = 0;
            }
        }
    }
}
