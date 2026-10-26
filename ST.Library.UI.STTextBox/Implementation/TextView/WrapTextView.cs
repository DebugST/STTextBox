using System;
using System.Collections.Generic;
using System.Drawing;

namespace ST.Library.UI.STTextBox
{
    public class WrapTextView : TextView
    {
        public Color LineNumberGuideColor { get; set; }
        public bool ShowLineNumberGuide { get; set; }

        public enum Method { Char, Word };
        public enum Alignment { Left, Center, Right/*, Justify*/ }

        private enum SubLineStatus
        {
            Start,
            Middle,
            End,
            None
        }

        private class WrapLineInfo
        {
            public int ScreenLineOffset { get; set; }
            public List<SubLineInfo> SubLines { get; set; }

            public override string ToString() {
                return "(" + this.ScreenLineOffset + "," + (this.SubLines != null ? this.SubLines.Count : 0) + ")";
            }
        }

        private class WordInfo
        {
            public float Width;
            public int CharCount;
            public int[] GraphemeLengths;
            public float[] GraphemeWidths;
            public bool IsUniformMono;
        }

        private class SubLineInfo
        {
            public int Index;           // The first char index of sub-line in line.RawString
            public int Length;          // The text length
            public int CharCount;       // Grapheme count
            public int Width;           // Sub-Line width
            public int X;               // The X position to draw
            public int[] Offsets;       // Pre-measured X offset for each char in [0..Length]
        }

        public Method WrapMethod { get; private set; }
        public Alignment WrapAlignment { get; private set; }

        private int m_n_line_count_start_change = 0;
        private HashSet<TextLine> m_hs_changed_temp = new HashSet<TextLine>();
        private readonly Dictionary<char, float> m_dic_char_widths = new Dictionary<char, float>();

        private readonly WordInfo m_scratch_word_info = new WordInfo();
        private readonly List<int> m_scratch_glens = new List<int>(64);
        private readonly List<float> m_scratch_gwidths = new List<float>(64);

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

        private int GetSubLineOffset(SubLineInfo subLine, int index) {
            if (subLine.Offsets != null) {
                if (index < subLine.Offsets.Length) return subLine.Offsets[index];
                return subLine.Width;
            }
            var c = base.Core;
            float mono = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
            return (int)Math.Round(Math.Min(index, subLine.Length) * mono);
        }

        private readonly StringFormat m_sf = new StringFormat(StringFormat.GenericTypographic) {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        public WrapTextView() : this(Method.Word, Alignment.Left) { }
        public WrapTextView(Method method, Alignment alignment) {
            this.WrapMethod = method;
            this.WrapAlignment = alignment;
            this.ShowLineNumberGuide = true;
            this.LineNumberGuideColor = Color.Magenta;
        }

        public override void Init(STTextBox.Core core) {
            base.Init(core);
            m_dic_char_widths.Clear();
            if (core != null && core.TextBox != null) {
                core.TextBox.FontChanged += (s, e) => {
                    m_dic_char_widths.Clear();
                };
            }
        }

        public void SetWrap(Method method, Alignment alignment) {
            this.WrapMethod = method;
            this.WrapAlignment = alignment;
            if (this.Core == null) return;
            this.OnCalcTextRectangle();
            this.OnInitAllText();
        }

        public override int GetCurrentCharOffset() {
            var c = base.Core;
            TextLine line = c.Caret.Line;
            var tag = _Tag(line);
            if (tag.SubLines == null || tag.SubLines.Count == 0) return 0;
            int nSubIndex = 0;
            int nCharIndex = c.Caret.IndexOfChar - line.IndexOfFirstChar;
            for (int i = 0; i < tag.SubLines.Count; i++) {
                var sub = tag.SubLines[i];
                if (nCharIndex >= sub.Index && nCharIndex <= sub.Index + sub.Length) {
                    nSubIndex = i;
                    break;
                }
                if (i == tag.SubLines.Count - 1) nSubIndex = i;
            }
            var subLine = tag.SubLines[nSubIndex];
            return c.Caret.X - base.TextRectangle.X - subLine.X;
        }

        public override void OnInitAllText() {
            var c = base.Core;
            if (c == null) return;
            m_dic_char_widths.Clear();
            if (base.TextRectangle.Width <= 0) {
                this.OnCalcTextRectangle();
            }
            int nLineOffset = 0;
            c.ITextBoxRender.BeginPaint();
            foreach (var line in base.Core.TextManager) {
                this.CacheLine(line, base.Core.ITextBoxRender);
                var tag = _Tag(line);
                tag.ScreenLineOffset = nLineOffset;
                nLineOffset += tag.SubLines.Count;
            }
            c.ITextBoxRender.EndPaint();
            this.OnCalcScroll(c.Scroll);
            if (c.TextBox != null) {
                c.TextBox.Invalidate();
            }
        }

        public override void OnLineAdded(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            this.CacheLine(e.Line, render);
            m_hs_changed_temp.Add(e.Line);
        }

        private WrapLineInfo _Tag(TextLine line) {
            WrapLineInfo ret = null;
            if (line.Tag == null || !(line.Tag is WrapLineInfo)) {
                line.Tag = ret = new WrapLineInfo();
                return ret;
            }
            return (WrapLineInfo)line.Tag;
        }

        private void CacheLine(TextLine line, ISTTextBoxRender render) {
            var tag = _Tag(line);
            tag.SubLines = this.GetCache(line, render);
        }

        public override void OnLineRemoved(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            m_hs_changed_temp.Remove(e.Line);
        }

        public override void OnLineChanged(ISTTextBoxRender render, TextManagerLineEventArgs e) {
            m_hs_changed_temp.Add(e.Line);
        }

        private float GetCharWidthFastFloat(STTextBox.Core c, TextLine line, int charIndexInLine, string strChar, float curX) {
            if (string.IsNullOrEmpty(strChar)) return 0f;
            if (strChar.Length == 1) {
                char ch = strChar[0];
                if (ch == '\r' || ch == '\n') return 0f;
                if (ch < 128 && c.ITextBoxRender.IsMonospaced) {
                    return c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
                }
                if (m_dic_char_widths.TryGetValue(ch, out float cachedW)) {
                    return cachedW;
                }
                var style = c.GetStyleFromCharIndex(line.IndexOfFirstChar + charIndexInLine);
                float w = c.GetStringWidthFloat(strChar, style, curX);
                m_dic_char_widths[ch] = w;
                return w;
            } else {
                if (strChar == "\r\n") return 0f;
                var style = c.GetStyleFromCharIndex(line.IndexOfFirstChar + charIndexInLine);
                return c.GetStringWidthFloat(strChar, style, curX);
            }
        }

        private List<SubLineInfo> GetCache(TextLine line, ISTTextBoxRender render) {
            var c = base.Core;
            List<SubLineInfo> lst = null;
            if (this.WrapMethod == Method.Char) {
                lst = new List<SubLineInfo>();
                this.GetCacheByChar(line, render, lst, 0);
            } else {
                lst = this.GetCacheByWord(line, render);
            }
            for (int i = 0; i < lst.Count; i++) {
                var sub = lst[i];
                switch (this.WrapAlignment) {
                    case Alignment.Center:
                        sub.X = (base.TextRectangle.Width - sub.Width) >> 1;
                        break;
                    case Alignment.Right:
                        sub.X = base.TextRectangle.Width - sub.Width - c.ITextBoxRender.GetSpaceWidth();
                        break;
                }
                lst[i] = sub;
            }
            return lst;
        }

        private void GetCacheByChar(TextLine line, ISTTextBoxRender render, List<SubLineInfo> lst, int nStartIndex) {
            var c = base.Core;
            float nTextViewLineWidth = (float)Math.Max(10, base.TextRectangle.Width);
            if (this.WrapAlignment != Alignment.Left) {
                nTextViewLineWidth -= (float)c.ITextBoxRender.GetSpaceWidth();
            }
            nTextViewLineWidth = Math.Max(1f, nTextViewLineWidth);

            bool isMono = c.ITextBoxRender.IsMonospaced && (c.TextBox.CharSpacing == 0);
            float monoAdvance = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
            bool isPureAsciiLine = isMono && monoAdvance > 0f && IsPureAsciiNoTabs(line.RawString);

            float nLineWidth = 0f;
            int nSubStrLen = 0;
            int nCharCount = 0;
            List<int> curOffsets = isPureAsciiLine ? null : new List<int>() { 0 };

            c.IGraphemeSplitter.Each(line.RawString, nStartIndex, (str, nStart, nLen) => {
                string strChar = str.Substring(nStart, nLen);
                float nCharWidth;
                if (strChar == "\r\n" || strChar == "\r" || strChar == "\n") {
                    nCharWidth = 0f;
                } else if (c.IsEmoji(strChar)) {
                    nCharWidth = (float)c.FontHeight;
                } else {
                    nCharWidth = GetCharWidthFastFloat(c, line, nStart, strChar, nLineWidth);
                }
                if (nLineWidth + nCharWidth >= nTextViewLineWidth && nSubStrLen > 0) {
                    lst.Add(new SubLineInfo() {
                        Index = nStartIndex,
                        Length = nSubStrLen,
                        CharCount = nCharCount,
                        Width = (int)Math.Round(nLineWidth),
                        Offsets = curOffsets != null ? curOffsets.ToArray() : null
                    });
                    if (curOffsets != null) {
                        curOffsets.Clear();
                        curOffsets.Add(0);
                    }
                    nCharCount = 1;
                    nStartIndex += nSubStrLen;
                    nSubStrLen = nLen;
                    nLineWidth = nCharWidth;
                    if (curOffsets != null) {
                        int off = (int)Math.Round(nLineWidth);
                        for (int k = 1; k <= nLen; k++) {
                            curOffsets.Add(off);
                        }
                    }
                } else {
                    nSubStrLen += nLen;
                    nLineWidth += nCharWidth;
                    nCharCount++;
                    if (curOffsets != null) {
                        int off = (int)Math.Round(nLineWidth);
                        for (int k = 1; k <= nLen; k++) {
                            curOffsets.Add(off);
                        }
                    }
                }
                return true;
            });
            lst.Add(new SubLineInfo() {
                Index = nStartIndex,
                Length = nSubStrLen,
                CharCount = nCharCount,
                Width = (int)Math.Round(nLineWidth),
                Offsets = curOffsets != null ? curOffsets.ToArray() : null
            });
        }

        private List<SubLineInfo> GetCacheByWord(TextLine line, ISTTextBoxRender render) {
            var c = base.Core;
            float nTextViewLineWidth = (float)Math.Max(10, base.TextRectangle.Width);
            if (this.WrapAlignment != Alignment.Left) {
                nTextViewLineWidth -= (float)c.ITextBoxRender.GetSpaceWidth();
            }
            nTextViewLineWidth = Math.Max(1f, nTextViewLineWidth);

            bool isMono = c.ITextBoxRender.IsMonospaced && (c.TextBox.CharSpacing == 0);
            float monoAdvance = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
            bool isPureAsciiLine = isMono && monoAdvance > 0f && IsPureAsciiNoTabs(line.RawString);

            List<SubLineInfo> lst = new List<SubLineInfo>();
            int curSubStart = 0;
            int curSubLen = 0;
            float curSubWidth = 0f;
            int curCharCount = 0;
            List<int> curOffsets = isPureAsciiLine ? null : new List<int>() { 0 };

            c.IWordSplitter.Each(line.RawString, (str, nStart, nLen) => {
                string strWord = str.Substring(nStart, nLen);
                var wi = this.GetWordInfo(line, render, strWord, nStart, curSubWidth);

                if (wi.Width >= nTextViewLineWidth) {
                    // Single word is wider than the entire line -> split character by character
                    if (wi.IsUniformMono) {
                        for (int g = 0; g < wi.CharCount; g++) {
                            if (curSubWidth + monoAdvance >= nTextViewLineWidth && curSubLen > 0) {
                                lst.Add(new SubLineInfo() {
                                    Index = curSubStart,
                                    Length = curSubLen,
                                    CharCount = curCharCount,
                                    Width = (int)Math.Round(curSubWidth),
                                    Offsets = curOffsets != null ? curOffsets.ToArray() : null
                                });
                                curSubStart += curSubLen;
                                curSubLen = 0;
                                curSubWidth = 0f;
                                curCharCount = 0;
                                if (curOffsets != null) {
                                    curOffsets.Clear();
                                    curOffsets.Add(0);
                                }
                            }
                            curSubLen += 1;
                            curSubWidth += monoAdvance;
                            curCharCount++;
                            if (curOffsets != null) {
                                curOffsets.Add((int)Math.Round(curSubWidth));
                            }
                        }
                    } else {
                        for (int g = 0; g < wi.GraphemeLengths.Length; g++) {
                            int gLen = wi.GraphemeLengths[g];
                            float gWidth = wi.GraphemeWidths[g];
                            if (curSubWidth + gWidth >= nTextViewLineWidth && curSubLen > 0) {
                                lst.Add(new SubLineInfo() {
                                    Index = curSubStart,
                                    Length = curSubLen,
                                    CharCount = curCharCount,
                                    Width = (int)Math.Round(curSubWidth),
                                    Offsets = curOffsets != null ? curOffsets.ToArray() : null
                                });
                                curSubStart += curSubLen;
                                curSubLen = 0;
                                curSubWidth = 0f;
                                curCharCount = 0;
                                if (curOffsets != null) {
                                    curOffsets.Clear();
                                    curOffsets.Add(0);
                                }
                            }
                            curSubLen += gLen;
                            curSubWidth += gWidth;
                            curCharCount++;
                            if (curOffsets != null) {
                                int off = (int)Math.Round(curSubWidth);
                                for (int k = 1; k <= gLen; k++) {
                                    curOffsets.Add(off);
                                }
                            }
                        }
                    }
                    return true;
                }

                if (curSubWidth + wi.Width >= nTextViewLineWidth && curSubLen > 0) {
                    // Word wraps to the next subline
                    lst.Add(new SubLineInfo() {
                        Index = curSubStart,
                        Length = curSubLen,
                        CharCount = curCharCount,
                        Width = (int)Math.Round(curSubWidth),
                        Offsets = curOffsets != null ? curOffsets.ToArray() : null
                    });
                    curSubStart += curSubLen;
                    curSubLen = 0;
                    curSubWidth = 0f;
                    curCharCount = 0;
                    if (curOffsets != null) {
                        curOffsets.Clear();
                        curOffsets.Add(0);
                    }
                }

                // Add the word to the current subline
                if (wi.IsUniformMono) {
                    curSubLen += wi.CharCount;
                    curSubWidth += wi.Width;
                    curCharCount += wi.CharCount;
                    if (curOffsets != null) {
                        for (int g = 0; g < wi.CharCount; g++) {
                            curOffsets.Add((int)Math.Round(curSubWidth - (wi.CharCount - 1 - g) * monoAdvance));
                        }
                    }
                } else {
                    for (int g = 0; g < wi.GraphemeLengths.Length; g++) {
                        int gLen = wi.GraphemeLengths[g];
                        float gWidth = wi.GraphemeWidths[g];
                        curSubLen += gLen;
                        curSubWidth += gWidth;
                        curCharCount++;
                        if (curOffsets != null) {
                            int off = (int)Math.Round(curSubWidth);
                            for (int k = 1; k <= gLen; k++) {
                                curOffsets.Add(off);
                            }
                        }
                    }
                }
                return true;
            });

            lst.Add(new SubLineInfo() {
                Index = curSubStart,
                Length = curSubLen,
                CharCount = curCharCount,
                Width = (int)Math.Round(curSubWidth),
                Offsets = curOffsets != null ? curOffsets.ToArray() : null
            });

            return lst;
        }

        private WordInfo GetWordInfo(TextLine line, ISTTextBoxRender render, string strWord, int nIndex, float nLeftWidth) {
            var c = base.Core;
            if (strWord == "\r\n" || strWord == "\r" || strWord == "\n") {
                m_scratch_word_info.Width = 0f;
                m_scratch_word_info.CharCount = 1;
                m_scratch_word_info.GraphemeLengths = null;
                m_scratch_word_info.GraphemeWidths = null;
                m_scratch_word_info.IsUniformMono = true;
                return m_scratch_word_info;
            }

            bool isMono = c.ITextBoxRender.IsMonospaced && (c.TextBox.CharSpacing == 0);
            float monoAdvance = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();

            if (isMono && monoAdvance > 0f && IsPureAsciiNoTabs(strWord)) {
                m_scratch_word_info.Width = strWord.Length * monoAdvance;
                m_scratch_word_info.CharCount = strWord.Length;
                m_scratch_word_info.GraphemeLengths = null;
                m_scratch_word_info.GraphemeWidths = null;
                m_scratch_word_info.IsUniformMono = true;
                return m_scratch_word_info;
            }

            float nWordWidth = 0f;
            int nCharCount = 0;
            m_scratch_glens.Clear();
            m_scratch_gwidths.Clear();

            c.IGraphemeSplitter.Each(strWord, (s, ns, nl) => {
                string strChar = s.Substring(ns, nl);
                float nCharWidth;
                if (strChar == "\r\n" || strChar == "\r" || strChar == "\n") {
                    nCharWidth = 0f;
                } else if (c.IsEmoji(strChar)) {
                    nCharWidth = (float)c.FontHeight;
                } else {
                    nCharWidth = GetCharWidthFastFloat(c, line, nIndex + ns, strChar, nLeftWidth + nWordWidth);
                }
                m_scratch_glens.Add(nl);
                m_scratch_gwidths.Add(nCharWidth);
                nWordWidth += nCharWidth;
                nCharCount++;
            });

            m_scratch_word_info.Width = nWordWidth;
            m_scratch_word_info.CharCount = nCharCount;
            m_scratch_word_info.GraphemeLengths = m_scratch_glens.ToArray();
            m_scratch_word_info.GraphemeWidths = m_scratch_gwidths.ToArray();
            m_scratch_word_info.IsUniformMono = false;
            return m_scratch_word_info;
        }

        public override void OnTextChanged(ISTTextBoxRender render, TextManagerTextEventArgs e) {
            base.OnTextChanged(render, e);
            var c = base.Core;
            var m = c.TextManager;
            int nIndexMin = int.MaxValue, nLineIndex = 0;

            if (m.LineCount.ToString().Length != m_n_line_count_start_change.ToString().Length) {
                this.OnCalcTextRectangle();
                int nOffset = 0;
                foreach (var line in m) {
                    this.CacheLine(line, render);
                    var t = _Tag(line);
                    t.ScreenLineOffset = nOffset;
                    nOffset += t.SubLines.Count;
                }
                m_hs_changed_temp.Clear();
                return;
            }

            int totalDelta = 0;
            foreach (var v in e.TextHistoryRecord) {
                if (v.Index < nIndexMin) nIndexMin = v.Index;
                nLineIndex = m.GetLineIndexFromCharIndex(v.Index);
                var line = m[nLineIndex];
                if (m_hs_changed_temp.Contains(line)) {
                    var tag = _Tag(line);
                    int oldCount = (tag.SubLines != null) ? tag.SubLines.Count : 0;
                    this.CacheLine(line, render);
                    int newCount = tag.SubLines.Count;
                    totalDelta += (newCount - oldCount);
                    m_hs_changed_temp.Remove(line);
                }
            }

            foreach (var line in m_hs_changed_temp) {
                var tag = _Tag(line);
                int oldCount = (tag.SubLines != null) ? tag.SubLines.Count : 0;
                this.CacheLine(line, render);
                int newCount = tag.SubLines.Count;
                totalDelta += (newCount - oldCount);
            }
            m_hs_changed_temp.Clear();

            // Only update subsequent screen line offsets if subline counts actually shifted or lines were added/removed
            if (totalDelta != 0 || m.LineCount != m_n_line_count_start_change) {
                nLineIndex = m.GetLineIndexFromCharIndex(nIndexMin);
                for (int i = nLineIndex + 1; i < m.LineCount; i++) {
                    var tag_prev = _Tag(m[i - 1]);
                    _Tag(m[i]).ScreenLineOffset = tag_prev.ScreenLineOffset + tag_prev.SubLines.Count;
                }
            }
        }

        public override void OnTextStartChange(ISTTextBoxRender render, TextManagerTextEventArgs e) {
            m_hs_changed_temp.Clear();
            m_n_line_count_start_change = base.Core.TextManager.LineCount;
        }

        public override FindInfo FindFromPoint(Point pt) {
            var c = base.Core;
            FindInfo fi = new FindInfo();
            int nYOffset = ((pt.Y - base.TextRectangle.Y) / c.LineHeight);
            int nYValue = c.Scroll.YValue + nYOffset;
            if (nYValue < 0) nYValue = 0;
            int nIndexLine = this.GetLineIndexFromYScroll(nYValue);
            TextLine line = c.TextManager[nIndexLine];
            var tag = _Tag(line);
            if (tag.SubLines == null || tag.SubLines.Count == 0) {
                fi.Line = line;
                fi.IndexOfLine = nIndexLine;
                fi.IndexOfCharInLine = 0;
                fi.Location = new Point(base.TextRectangle.X, base.TextRectangle.Y + nYOffset * c.LineHeight);
                fi.Find = true;
                return fi;
            }
            if (nYValue >= tag.ScreenLineOffset + tag.SubLines.Count) {
                nYOffset -= nYValue - tag.ScreenLineOffset - tag.SubLines.Count + 1;
            }
            var temp = nYValue - tag.ScreenLineOffset;
            if (temp < 0) temp = 0;
            if (temp >= tag.SubLines.Count) {
                temp = tag.SubLines.Count - 1;
            }
            var subLine = tag.SubLines[temp];
            fi.Line = line;
            fi.IndexOfLine = nIndexLine;

            int targetX = pt.X - base.TextRectangle.X - subLine.X;
            if (subLine.Offsets == null) {
                float mono = c.ITextBoxRender.CharWidthFloat > 0f ? c.ITextBoxRender.CharWidthFloat : (float)c.ITextBoxRender.GetSpaceWidth();
                int monoCol = 0;
                if (mono > 0f) {
                    monoCol = (int)Math.Round(targetX / mono);
                }
                if (monoCol < 0) monoCol = 0;
                if (monoCol > subLine.Length) monoCol = subLine.Length;
                if (monoCol > 0 && subLine.Index + monoCol <= line.RawString.Length) {
                    if (subLine.Index + monoCol == line.RawString.Length && (line.RawString[line.RawString.Length - 1] == '\n' || line.RawString[line.RawString.Length - 1] == '\r')) {
                        if (monoCol == subLine.Length && (line.RawString[subLine.Index + monoCol - 1] == '\r' || line.RawString[subLine.Index + monoCol - 1] == '\n')) {
                            monoCol--;
                            if (monoCol > 0 && line.RawString[subLine.Index + monoCol - 1] == '\r') {
                                monoCol--;
                            }
                        }
                    }
                }
                int offX = (int)Math.Round(monoCol * mono);
                fi.IndexOfCharInLine = subLine.Index + monoCol;
                fi.Location = new Point(base.TextRectangle.X + subLine.X + offX, base.TextRectangle.Y + nYOffset * c.LineHeight);
                fi.Find = true;
                return fi;
            }

            int[] offsets = subLine.Offsets;
            if (offsets.Length <= 1) {
                fi.IndexOfCharInLine = subLine.Index;
                fi.Location = new Point(base.TextRectangle.X + subLine.X, base.TextRectangle.Y + nYOffset * c.LineHeight);
                fi.Find = true;
                return fi;
            }

            int col = 0;
            int low = 0, high = offsets.Length - 1;
            while (low <= high) {
                int mid = (low + high) >> 1;
                if (offsets[mid] <= targetX) {
                    col = mid;
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }
            if (col < offsets.Length - 1) {
                int charWidth = offsets[col + 1] - offsets[col];
                if (charWidth > 0 && targetX - offsets[col] >= charWidth / 2) {
                    col++;
                }
            }

            if (col > 0 && subLine.Index + col <= line.RawString.Length) {
                if (subLine.Index + col == line.RawString.Length && (line.RawString[line.RawString.Length - 1] == '\n' || line.RawString[line.RawString.Length - 1] == '\r')) {
                    if (col == subLine.Length && (line.RawString[subLine.Index + col - 1] == '\r' || line.RawString[subLine.Index + col - 1] == '\n')) {
                        col--;
                        if (col > 0 && line.RawString[subLine.Index + col - 1] == '\r') {
                            col--;
                        }
                    }
                }
            }

            fi.IndexOfCharInLine = subLine.Index + col;
            fi.Location = new Point(base.TextRectangle.X + subLine.X + offsets[col], base.TextRectangle.Y + nYOffset * c.LineHeight);
            fi.Find = true;
            return fi;
        }

        public override FindInfo FindFromCharIndex(int nIndex) {
            var c = base.Core;
            int nIndexOfLine = c.TextManager.GetLineIndexFromCharIndex(nIndex);
            TextLine stLine = c.TextManager[nIndexOfLine];
            if (stLine == null) {
                return new FindInfo();
            }
            return this.FindFromCharIndex(nIndexOfLine, nIndex - stLine.IndexOfFirstChar);
        }

        private FindInfo FindFromCharIndex(int nIndexOfLine, int nIndex) {
            var c = base.Core;
            return this.FindFromCharIndex(c.ITextBoxRender, nIndexOfLine, nIndex);
        }

        private FindInfo FindFromCharIndex(ISTTextBoxRender render, int nIndexOfLine, int nIndex) {
            var c = base.Core;
            TextLine line = c.TextManager[nIndexOfLine];
            var tag = _Tag(line);
            if (tag.SubLines == null || tag.SubLines.Count == 0) {
                return new FindInfo() {
                    Find = true,
                    Line = line,
                    IndexOfLine = nIndexOfLine,
                    IndexOfCharInLine = 0,
                    Location = new Point(base.TextRectangle.X, base.TextRectangle.Y + (tag.ScreenLineOffset - c.Scroll.YValue) * c.LineHeight)
                };
            }
            int nSubIndex = 0;
            for (int i = 0; i < tag.SubLines.Count; i++) {
                var sub = tag.SubLines[i];
                if (nIndex >= sub.Index && nIndex < sub.Index + sub.Length) {
                    nSubIndex = i;
                    break;
                }
                if (i == tag.SubLines.Count - 1) {
                    nSubIndex = i;
                }
            }
            var subLine = tag.SubLines[nSubIndex];
            int relChar = Math.Max(0, Math.Min(subLine.Length, nIndex - subLine.Index));
            int charX = GetSubLineOffset(subLine, relChar);

            return new FindInfo() {
                Find = true,
                Line = line,
                IndexOfLine = nIndexOfLine,
                IndexOfCharInLine = nIndex,
                Location = new Point(base.TextRectangle.X + subLine.X + charX, base.TextRectangle.Y + (tag.ScreenLineOffset + nSubIndex - c.Scroll.YValue) * c.LineHeight)
            };
        }

        protected override int OnDrawLine(ISTTextBoxRender render, TextLine line, int nX, int nY) {
            var c = base.Core;
            if (string.IsNullOrEmpty(line.RawString)) {
                return c.LineHeight;
            }
            var tag = _Tag(line);
            if (tag.SubLines == null || tag.SubLines.Count == 0) {
                return c.LineHeight;
            }

            int nRet = 0;
            string raw = line.RawString;
            int lineStartChar = line.IndexOfFirstChar;
            int selStart = c.Selection.StartIndex;
            int selEnd = c.Selection.EndIndex;
            bool isSelectionEmpty = c.Selection.IsEmptySelection;
            Color selTextColor = (c.TextBox.SelectedTextColor != Color.Empty) ? c.TextBox.SelectedTextColor : Color.White;
            int nCharSpacing = c.TextBox.GetIntXSize(c.TextBox.CharSpacing);
            int nLineSpacing = c.TextBox.GetIntYSize(c.TextBox.LineSpacing) / 2;

            int temp = tag.ScreenLineOffset < c.Scroll.YValue ? c.Scroll.YValue - tag.ScreenLineOffset : 0;
            for (int i = temp; i < tag.SubLines.Count; i++) {
                var subLine = tag.SubLines[i];
                int subStartChar = lineStartChar + subLine.Index;
                int subLen = subLine.Length;
                while (subLen > 0 && (raw[subLine.Index + subLen - 1] == '\r' || raw[subLine.Index + subLen - 1] == '\n')) {
                    subLen--;
                }

                if (subLen > 0) {
                    int drawX = nX + subLine.X;
                    int subWidth = GetSubLineOffset(subLine, subLen);

                    bool hasSelection = !isSelectionEmpty && !(subStartChar >= selEnd || (subStartChar + subLen) <= selStart);

                    void DrawSubLineRuns(bool isSelectionPass) {
                        int runStart = 0;
                        int runStartX = drawX;
                        TextStyle syntaxRunStyle = TextStyle.Empty;
                        bool hasRun = false;

                        for (int j = 0; j < subLen; j++) {
                            char ch = raw[subLine.Index + j];
                            int charIdx = subStartChar + j;

                            if (ch == '\t') {
                                if (hasRun && j > runStart) {
                                    string runStr = raw.Substring(subLine.Index + runStart, j - runStart);
                                    int runW = GetSubLineOffset(subLine, j) - GetSubLineOffset(subLine, runStart);
                                    TextStyle drawStyle = syntaxRunStyle;
                                    if (isSelectionPass) {
                                        drawStyle.ForeColor = selTextColor;
                                        drawStyle.BackColor = Color.Transparent;
                                    }
                                    render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runW, c.LineHeight));
                                    hasRun = false;
                                }
                                continue;
                            }

                            int emojiLen;
                            string emojiStr;
                            if (c.TryGetEmoji(raw, subLine.Index + j, subLine.Index + subLen, out emojiLen, out emojiStr)) {
                                if (hasRun && j > runStart) {
                                    string runStr = raw.Substring(subLine.Index + runStart, j - runStart);
                                    int runW = GetSubLineOffset(subLine, j) - GetSubLineOffset(subLine, runStart);
                                    TextStyle drawStyle = syntaxRunStyle;
                                    if (isSelectionPass) {
                                        drawStyle.ForeColor = selTextColor;
                                        drawStyle.BackColor = Color.Transparent;
                                    }
                                    render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runW, c.LineHeight));
                                    hasRun = false;
                                }
                                if (c.IEmojiRender != null) {
                                    int emojiX = drawX + GetSubLineOffset(subLine, j);
                                    c.IEmojiRender.DrawEmoji(render, emojiStr, emojiX + (nCharSpacing >> 1), nY + nLineSpacing, c.FontHeight, isSelectionPass);
                                }
                                j += emojiLen - 1;
                                continue;
                            }

                            var syntaxStyle = c.GetStyleFromCharIndex(charIdx);
                            if (syntaxStyle.ForeColor.A == 0) syntaxStyle.ForeColor = c.TextBox.ForeColor;

                            if (!hasRun) {
                                runStart = j;
                                runStartX = drawX + GetSubLineOffset(subLine, j);
                                syntaxRunStyle = syntaxStyle;
                                hasRun = true;
                            } else if (syntaxStyle.ForeColor != syntaxRunStyle.ForeColor || syntaxStyle.BackColor != syntaxRunStyle.BackColor || syntaxStyle.FontStyle != syntaxRunStyle.FontStyle) {
                                string runStr = raw.Substring(subLine.Index + runStart, j - runStart);
                                int runW = GetSubLineOffset(subLine, j) - GetSubLineOffset(subLine, runStart);
                                TextStyle drawStyle = syntaxRunStyle;
                                if (isSelectionPass) {
                                    drawStyle.ForeColor = selTextColor;
                                    drawStyle.BackColor = Color.Transparent;
                                }
                                render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runW, c.LineHeight));
                                runStart = j;
                                runStartX = drawX + GetSubLineOffset(subLine, j);
                                syntaxRunStyle = syntaxStyle;
                            }
                        }

                        if (hasRun && subLen > runStart) {
                            string runStr = raw.Substring(subLine.Index + runStart, subLen - runStart);
                            int runW = GetSubLineOffset(subLine, subLen) - GetSubLineOffset(subLine, runStart);
                            TextStyle drawStyle = syntaxRunStyle;
                            if (isSelectionPass) {
                                drawStyle.ForeColor = selTextColor;
                                drawStyle.BackColor = Color.Transparent;
                            }
                            render.DrawString(runStr, drawStyle, new Rectangle(runStartX, nY, runW, c.LineHeight));
                        }
                    }

                    // Pass 1: Render subline with syntax styles
                    DrawSubLineRuns(false);

                    // Pass 2: Clip to selection if selected
                    if (hasSelection && selTextColor != Color.Empty) {
                        int relSelStart = Math.Max(0, selStart - subStartChar);
                        int relSelEnd = Math.Min(subLen, selEnd - subStartChar);
                        if (relSelEnd > relSelStart) {
                            int selLeftX = drawX + GetSubLineOffset(subLine, relSelStart);
                            int selRightX = drawX + GetSubLineOffset(subLine, relSelEnd);
                            int selW = selRightX - selLeftX;
                            if (selW > 0) {
                                Rectangle selClip = new Rectangle(selLeftX, nY, selW, c.LineHeight + 1);
                                selClip.Intersect(base.TextRectangle);
                                if (selClip.Width > 0 && selClip.Height > 0) {
                                    render.SetClip(selClip);
                                    DrawSubLineRuns(true);
                                    render.SetClip(base.TextRectangle);
                                }
                            }
                        }
                    }
                }

                nY += c.LineHeight;
                nRet += c.LineHeight;
                if (nY >= base.TextRectangle.Bottom) break;
            }
            return nRet;
        }

        protected override void OnDrawHead(ISTTextBoxRender render, int nStartLineIndex) {
            var c = base.Core;
            if (nStartLineIndex >= c.TextManager.LineCount) return;
            TextLine line = c.TextManager[nStartLineIndex];
            var tag = _Tag(line);
            var temp = tag.ScreenLineOffset < c.Scroll.YValue ? c.Scroll.YValue - tag.ScreenLineOffset : 0;

            render.FillRectangle(base.HeadBackgroundColor, base.HeadRectangle);

            var clr_temp = Color.FromArgb(base.LineNumberColor.A / 3, base.LineNumberColor);
            int nY = base.HeadRectangle.Y;
            this.DrawHeadLine(render, line, this.GetSubLineStatus(tag.SubLines.Count, temp), nStartLineIndex + 1, base.LineNumberColor, nY);
            nY += c.LineHeight;
            for (int i = temp + 1; i < tag.SubLines.Count; i++) {
                if (nY >= base.HeadRectangle.Bottom) return;
                this.DrawHeadLine(render, line, this.GetSubLineStatus(tag.SubLines.Count, i), nStartLineIndex + 1, clr_temp, nY);
                nY += c.LineHeight;
            }

            for (int i = nStartLineIndex + 1; i < c.TextManager.LineCount; i++) {
                if (nY >= base.HeadRectangle.Bottom) return;
                line = c.TextManager[i];
                tag = _Tag(line);
                this.DrawHeadLine(render, line, this.GetSubLineStatus(tag.SubLines.Count, 0), i + 1, base.LineNumberColor, nY);
                nY += c.LineHeight;
                for (int j = 1; j < tag.SubLines.Count; j++) {
                    if (nY >= base.HeadRectangle.Bottom) return;
                    this.DrawHeadLine(render, line, this.GetSubLineStatus(tag.SubLines.Count, j), i + 1, clr_temp, nY);
                    nY += c.LineHeight;
                }
            }
        }

        private void DrawHeadLine(ISTTextBoxRender render, TextLine line, SubLineStatus status, int nShowNumber, Color clr, int nY) {
            var c = base.Core;
            Rectangle rect_linenumber = new Rectangle(base.HeadRectangle.X, nY, base.LineNumberWidth - c.TextBox.GetIntXSize(10), c.LineHeight);
            Rectangle rect_temp = new Rectangle(base.HeadRectangle.X + c.TextBox.GetIntXSize(5), nY, c.TextBox.GetIntXSize(1), c.LineHeight);
            if (this.ShowLineNumberGuide) {
                var clr_temp = this.LineNumberGuideColor;
                switch (status) {
                    case SubLineStatus.Start:
                        rect_temp.Y += c.LineHeight / 2;
                        rect_temp.Height -= rect_temp.Y - nY;
                        render.FillRectangle(clr_temp, rect_temp.Right, rect_temp.Y, rect_temp.Width * 3, rect_temp.Width);
                        break;
                    case SubLineStatus.End:
                        rect_temp.Height -= c.LineHeight / 2;
                        render.FillRectangle(clr_temp, rect_temp.Right, rect_temp.Bottom - rect_temp.Width, rect_temp.Width * 3, rect_temp.Width);
                        break;
                    case SubLineStatus.None:
                        rect_temp.Y += rect_temp.Width;
                        rect_temp.Height -= rect_temp.Width * 2;
                        rect_temp.Width = c.TextBox.GetIntXSize(4);
                        clr_temp = Color.FromArgb(clr_temp.A / 3, clr_temp);
                        break;
                }
                render.FillRectangle(clr_temp, rect_temp);
            }
            render.DrawString(nShowNumber.ToString(), c.TextBox.Font, clr, rect_linenumber, m_sf);
        }

        private SubLineStatus GetSubLineStatus(int nSubLineCount, int nIndex) {
            if (nSubLineCount == 1) {
                return SubLineStatus.None;
            } else if (nIndex == 0) {
                return SubLineStatus.Start;
            } else if (nIndex == nSubLineCount - 1) {
                return SubLineStatus.End;
            } else {
                return SubLineStatus.Middle;
            }
        }

        public override int GetLineIndexFromYScroll(int nYValue) {
            var m = base.Core.TextManager;
            int nLeft = 0, nRight = m.LineCount - 1, nMid = 0;
            while (nLeft <= nRight) {
                nMid = (nLeft + nRight) >> 1;
                var tag = _Tag(m[nMid]);
                if (nYValue < tag.ScreenLineOffset) {
                    nRight = nMid - 1;
                } else if (nYValue >= tag.ScreenLineOffset + tag.SubLines.Count) {
                    nLeft = nMid + 1;
                } else {
                    return nMid;
                }
            }
            if (nMid < 0) return 0;
            if (nMid >= m.LineCount) return m.LineCount - 1;
            return nMid;
        }

        public override void SetCaretPostion(int nCharIndex) {
            var c = this.Core;
            if (c == null) return;
            int nLineIndex = c.TextManager.GetLineIndexFromCharIndex(nCharIndex);
            var line = c.TextManager[nLineIndex];
            var tag = _Tag(line);
            if (tag.SubLines == null || tag.SubLines.Count == 0) {
                c.Caret.Line = line;
                c.Caret.IndexOfLine = nLineIndex;
                c.Caret.IndexOfChar = nCharIndex;
                this.SetCaretPostion(this.TextRectangle.X, this.TextRectangle.Y);
                return;
            }

            int nIndexInLine = nCharIndex - line.IndexOfFirstChar;
            int nSubLineIndex = 0;
            for (int i = 0; i < tag.SubLines.Count; i++) {
                var v = tag.SubLines[i];
                if (nIndexInLine >= v.Index && nIndexInLine < v.Index + v.Length) {
                    nSubLineIndex = i;
                    break;
                }
                if (i == tag.SubLines.Count - 1) {
                    nSubLineIndex = i;
                }
            }
            var subLine = tag.SubLines[nSubLineIndex];
            int relChar = Math.Max(0, Math.Min(subLine.Length, nIndexInLine - subLine.Index));
            int charX = GetSubLineOffset(subLine, relChar);

            c.Caret.Line = line;
            c.Caret.IndexOfLine = nLineIndex;
            c.Caret.IndexOfChar = nCharIndex;
            this.SetCaretPostion(this.TextRectangle.X + subLine.X + charX, this.TextRectangle.Y + (tag.ScreenLineOffset + nSubLineIndex - c.Scroll.YValue) * c.LineHeight);
        }

        protected override void OnDrawLineSelectionBackground(ISTTextBoxRender render, TextLine line, int nLineIndex, STTextBoxSelectionInfo selection, int nY) {
            var c = base.Core;
            if (string.IsNullOrEmpty(line.RawString)) return;
            var tag = _Tag(line);
            if (tag.SubLines == null) return;
            int temp = tag.ScreenLineOffset < c.Scroll.YValue ? c.Scroll.YValue - tag.ScreenLineOffset : 0;
            int lineStartChar = line.IndexOfFirstChar;

            for (int i = temp; i < tag.SubLines.Count; i++) {
                var subLine = tag.SubLines[i];
                int subStartChar = lineStartChar + subLine.Index;
                int subEndChar = subStartChar + subLine.Length;

                if (subStartChar >= selection.EndIndex) return;
                if (subEndChar <= selection.StartIndex) {
                    if (nY >= base.TextRectangle.Bottom) break;
                    nY += c.LineHeight;
                    continue;
                }

                int relStart = Math.Max(0, selection.StartIndex - subStartChar);
                int relEnd = Math.Min(subLine.Length, selection.EndIndex - subStartChar);

                int startX = GetSubLineOffset(subLine, relStart);
                int endX = GetSubLineOffset(subLine, relEnd);

                int nLeft = base.TextRectangle.X + subLine.X + startX;
                int nWidth = endX - startX;
                if (nWidth > 0) {
                    Color selColor = c.TextBox.SelectionColor.A == 0 ? Color.FromArgb(0, 120, 215) : c.TextBox.SelectionColor;
                    render.FillRectangle(selColor, nLeft, nY, nWidth, c.LineHeight + 1);
                }
                nY += c.LineHeight;
                if (nY >= base.TextRectangle.Bottom) break;
            }
        }

        public override bool ScrollToCaret() {
            var c = this.Core;
            bool flag = false;
            Point pt = c.Caret.Location;
            if (pt.Y < this.TextRectangle.Y) {
                c.Scroll.YValue += (pt.Y - this.TextRectangle.Y) / c.LineHeight;
                flag = true;
            } else if (pt.Y + c.LineHeight > this.TextRectangle.Bottom) {
                int nIncrement = (int)Math.Ceiling((pt.Y + c.LineHeight - this.TextRectangle.Bottom) / (float)c.LineHeight);
                c.Scroll.YValue += nIncrement;
                flag = true;
            }
            if (flag) {
                this.SetCaretPostion(c.Caret.IndexOfChar);
            }
            return flag;
        }

        public override void OnResize(EventArgs e) {
            base.OnResize(e);
            var c = base.Core;
            if (c == null) return;
            int nLineOffset = 0;
            c.ITextBoxRender.BeginPaint();
            foreach (TextLine line in c.TextManager) {
                var tag = _Tag(line);
                tag.ScreenLineOffset = nLineOffset;
                tag.SubLines = this.GetCache(line, c.ITextBoxRender);
                nLineOffset += tag.SubLines.Count;
            }
            c.ITextBoxRender.EndPaint();
            this.OnCalcScroll(c.Scroll);
        }

        public override void OnCalcScroll(STTextBoxScrollInfo scrollInfo) {
            var c = base.Core;
            if (c == null) return;
            var m = c.TextManager;
            if (m == null || m.LineCount == 0) return;
            var tag = _Tag(m[m.LineCount - 1]);
            int subCount = tag.SubLines != null ? tag.SubLines.Count : 0;
            c.Scroll.MaxYValue = tag.ScreenLineOffset + subCount - base.TextRectangle.Height / c.LineHeight + 2;
            if (c.Scroll.MaxYValue < 0) {
                c.Scroll.MaxYValue = 0;
            }
            if (c.Scroll.YValue > c.Scroll.MaxYValue) {
                c.Scroll.YValue = c.Scroll.MaxYValue;
            }
        }
    }
}
