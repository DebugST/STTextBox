using System;
using System.Collections.Generic;

namespace ST.Library.UI.STTextBox
{
    /// <summary>
    /// High-performance Unicode grapheme cluster splitter powered by GraphemeSplitterNET.
    /// Compliant with Unicode Standard Annex #29, featuring full Unicode coverage,
    /// O(1) table lookups, and zero allocations for cursor and index iteration.
    /// </summary>
    public class GraphemeSplitter : TextBoundary
    {
        public const int Other = 0;
        public const int CR = 1;
        public const int LF = 2;
        public const int Control = 3;
        public const int Extend = 4;
        public const int Regional_Indicator = 5;
        public const int SpacingMark = 6;
        public const int L = 7;
        public const int V = 8;
        public const int T = 9;
        public const int LV = 10;
        public const int LVT = 11;
        public const int Prepend = 12;
        public const int E_Base = 13;
        public const int E_Modifier = 14;
        public const int ZWJ = 15;
        public const int Glue_After_Zwj = 16;
        public const int E_Base_GAZ = 17;
        public const int Extended_Pictographic = 18;

        private readonly GraphemeSplitterNET.GraphemeSplitter m_splitter;

        /// <summary>
        /// Underlying GraphemeSplitterNET engine.
        /// </summary>
        public GraphemeSplitterNET.GraphemeSplitter InnerSplitter {
            get { return m_splitter; }
        }

        public GraphemeSplitter() {
            m_splitter = new GraphemeSplitterNET.GraphemeSplitter();
        }

        public GraphemeSplitter(GraphemeSplitterNET.GraphemeSplitter splitter) {
            if (splitter == null) throw new ArgumentNullException("splitter");
            m_splitter = splitter;
        }

        public override List<string> Split(string strText) {
            return m_splitter.Split(strText);
        }

        public override int GetCount(string strText) {
            return m_splitter.GetCount(strText);
        }

        public override void Each(string strText, EachVoidCallBack cb) {
            if (cb == null) return;
            m_splitter.Each(strText, (str, nStart, nLen) => cb(str, nStart, nLen));
        }

        public override void Each(string strText, int nIndex, EachVoidCallBack cb) {
            if (cb == null) return;
            m_splitter.Each(strText, nIndex, (str, nStart, nLen) => cb(str, nStart, nLen));
        }

        public override void Each(string strText, EachBoolCallBack cb) {
            if (cb == null) return;
            m_splitter.Each(strText, (str, nStart, nLen) => cb(str, nStart, nLen));
        }

        public override void Each(string strText, int nIndex, EachBoolCallBack cb) {
            if (cb == null) return;
            m_splitter.Each(strText, nIndex, (str, nStart, nLen) => cb(str, nStart, nLen));
        }

        /// <summary>
        /// Gets a list of grapheme cluster start indices without allocating strings.
        /// </summary>
        public List<int> GetBreaks(string strText) {
            return m_splitter.GetBreaks(strText);
        }

        /// <summary>
        /// Appends grapheme cluster start indices into an existing list (zero allocation when reused).
        /// </summary>
        public void GetBreaks(string strText, List<int> boundaries) {
            m_splitter.GetBreaks(strText, boundaries);
        }

        /// <summary>
        /// Finds the index of the next grapheme cluster boundary after startIndex.
        /// </summary>
        public int NextBreak(string strText, int startIndex) {
            return m_splitter.NextBreak(strText, startIndex);
        }

        // Backward compatibility cache stubs (no longer needed because lookup is already O(1) via compact two-stage table)
        public static void CreateArrayCache() { }
        public static void CreateDictionaryCache() { }
        public static void ClearCache() { }

        protected override bool ShouldBreak(int nRightBreakType, List<int> lstHistoryBreakType) {
            return true;
        }

        protected override int GetBreakProperty(int nCodePoint) {
            return m_splitter.GetGraphemeBreakProperty(nCodePoint);
        }
    }
}
