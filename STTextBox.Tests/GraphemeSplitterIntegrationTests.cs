using System;
using System.Collections.Generic;
using ST.Library.UI.STTextBox;
using Xunit;

namespace STTextBox.Tests
{
    public class GraphemeSplitterIntegrationTests
    {
        [Fact]
        public void Split_StandardClusters_SplitsCorrectly()
        {
            ITextBoundary splitter = new GraphemeSplitter();

            var clusters = splitter.Split("a\r\nb\u0301c");
            Assert.Equal(new[] { "a", "\r\n", "b\u0301", "c" }, clusters);
            Assert.Equal(4, splitter.GetCount("a\r\nb\u0301c"));
        }

        [Fact]
        public void Split_ComplexEmojiAndFlags_SplitsCorrectly()
        {
            ITextBoundary splitter = new GraphemeSplitter();

            // Family emoji with ZWJ: Man + ZWJ + Woman + ZWJ + Girl = 1 grapheme cluster
            string family = "\U0001F468\u200D\U0001F469\u200D\U0001F467";
            Assert.Single(splitter.Split(family));
            Assert.Equal(1, splitter.GetCount(family));

            // US flag: 2 Regional Indicators = 1 grapheme cluster
            string flag = "\U0001F1FA\U0001F1F8";
            Assert.Single(splitter.Split(flag));
        }

        [Fact]
        public void Split_IndicConjuncts_DoesNotBreak()
        {
            ITextBoundary splitter = new GraphemeSplitter();

            // Devanagari ksha (KA + VIRAMA + SSA) = 1 grapheme cluster (GB9c)
            string ksha = "\u0915\u094D\u0937";
            Assert.Single(splitter.Split(ksha));
            Assert.Equal(1, splitter.GetCount(ksha));
        }

        [Fact]
        public void Each_WithStartIndex_ReportsIndicesRelativeToOriginalString()
        {
            ITextBoundary splitter = new GraphemeSplitter();
            string text = "Line: \U0001F468\u200D\U0001F469 end";

            // Find cluster starting at index 6 (where the family emoji begins)
            var reported = new List<(int start, int len)>();
            splitter.Each(text, 6, (str, nStart, nLen) => {
                reported.Add((nStart, nLen));
            });

            Assert.Equal(5, reported.Count);
            // 1st is the emoji at index 6, length 5
            Assert.Equal(6, reported[0].start);
            Assert.Equal(5, reported[0].len);
            Assert.Equal("\U0001F468\u200D\U0001F469", text.Substring(reported[0].start, reported[0].len));

            // 2nd is space at index 11, length 1
            Assert.Equal(11, reported[1].start);
            Assert.Equal(1, reported[1].len);
        }

        [Fact]
        public void Each_BoolCallback_StopsEarlyWhenRequested()
        {
            ITextBoundary splitter = new GraphemeSplitter();
            int count = 0;
            splitter.Each("abcdefg", 0, (str, nStart, nLen) => {
                count++;
                return count < 3; // Stop after 3
            });

            Assert.Equal(3, count);
        }

        [Fact]
        public void GraphemeSplitter_ExposesNewZeroAllocMethods()
        {
            var splitter = new GraphemeSplitter();
            string text = "Hello \U0001F600 World";

            var breaks = splitter.GetBreaks(text);
            var reused = new List<int>();
            splitter.GetBreaks(text, reused);

            Assert.Equal(breaks, reused);
            Assert.Equal(0, breaks[0]);

            int next = 0;
            var fromNext = new List<int>();
            while (next < text.Length) {
                fromNext.Add(next);
                next = splitter.NextBreak(text, next);
            }

            Assert.Equal(breaks, fromNext);
        }
    }
}
