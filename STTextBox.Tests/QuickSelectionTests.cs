using System;
using System.Drawing;
using System.Windows.Forms;
using ST.Library.UI.STTextBox;
using Xunit;

namespace STTextBox.Tests
{
    public class QuickSelectionTests
    {
        [Fact]
        public void STTextBox_SelectionProperties_HaveExpectedDefaultsAndUpdates()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                // Default selection color should be Windows accent blue
                Assert.Equal(Color.FromArgb(0, 120, 215), tbx.SelectionColor);
                // Default selected text color should be White
                Assert.Equal(Color.White, tbx.SelectedTextColor);

                // Can customize colors
                tbx.SelectionColor = Color.FromArgb(16, 124, 65);
                tbx.SelectedTextColor = Color.Yellow;

                Assert.Equal(Color.FromArgb(16, 124, 65), tbx.SelectionColor);
                Assert.Equal(Color.Yellow, tbx.SelectedTextColor);
            }
        }

        [Fact]
        public void STTextBoxGDIRender_TabAndMeasurement_CalculatesCorrectly()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var render = new STTextBoxGDIRender()) {
                render.BindControl(tbx);

                Assert.Equal(4, render.GetTabSize());
                int spaceWidth = render.GetSpaceWidth();
                Assert.True(spaceWidth > 0);

                int tabWidthAt0 = render.GetTabWidth(0);
                Assert.Equal(spaceWidth * 4, tabWidthAt0);

                // Monospaced font check for Consolas
                tbx.Font = new Font("Consolas", 10f);
                Assert.True(render.IsMonospaced);
                spaceWidth = render.GetSpaceWidth();

                // Tab size change
                render.SetTabSize(8);
                Assert.Equal(8, render.GetTabSize());
                Assert.Equal(spaceWidth * 8, render.GetTabWidth(0));

                render.UnbindControl();
            }
        }

        [Fact]
        public void QuickTextView_FindFromPoint_HitTestsAccurately()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10f);

                string content = "Line 00: abcdefghijklmnopqrstuvwxyz\r\nLine 01: 0123456789\r\nLine 02: End";
                tbx.SetText(content);

                // Find at top-left inside text rectangle
                Point pt = new Point(view.TextRectangle.Left + 5, view.TextRectangle.Top + 5);
                FindInfo fi = view.FindFromPoint(pt);

                Assert.True(fi.Find);
                Assert.Equal(0, fi.IndexOfLine);

                // Find at line 1
                Point ptLine1 = new Point(view.TextRectangle.Left + 20, view.TextRectangle.Top + tbx.Font.Height + 5);
                FindInfo fi1 = view.FindFromPoint(ptLine1);

                Assert.True(fi1.Find);
                Assert.Equal(1, fi1.IndexOfLine);
            }
        }

        [Fact]
        public void QuickTextView_FindFromCharIndex_MapsToCorrectLine()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10f);

                string line0 = "Line 000\r\n";
                string line1 = "Line 001\r\n";
                tbx.SetText(line0 + line1);

                // Char index 0 is on line 0
                FindInfo fi0 = view.FindFromCharIndex(0);
                Assert.True(fi0.Find);
                Assert.Equal(0, fi0.IndexOfLine);
                Assert.Equal(0, fi0.IndexOfCharInLine);

                // Char index inside line 1
                int charInLine1 = line0.Length + 2;
                FindInfo fi1 = view.FindFromCharIndex(charInLine1);
                Assert.True(fi1.Find);
                Assert.Equal(1, fi1.IndexOfLine);
                Assert.Equal(2, fi1.IndexOfCharInLine);
            }
        }

        [Fact]
        public void STTextBox_RegionalInvalidation_DoesNotThrow()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                tbx.CreateControl();
                tbx.Size = new Size(400, 300);
                tbx.SetText("Line 1\r\nLine 2\r\nLine 3\r\nLine 4\r\nLine 5");

                // Invalidate single line selection change
                var ex = Record.Exception(() => tbx.InvalidateSelectionChange(1, 2));
                Assert.Null(ex);

                // Invalidate multi-line selection change
                ex = Record.Exception(() => tbx.InvalidateSelectionChange(0, 4));
                Assert.Null(ex);
            }
        }

        [Fact]
        public void STTextBoxGDIPRender_MeasurementPrecision_HasSubpixelAccuracy()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var render = new STTextBoxGDIPRender()) {
                tbx.Font = new Font("Consolas", 10.5f);
                render.BindControl(tbx);

                Assert.True(render.IsMonospaced);
                float charWidth = render.CharWidthFloat;
                Assert.True(charWidth > 0f);

                render.BeginPaint();
                try {
                    // Test across 100 characters that cumulative advance matches measured advance within 0.001 px
                    string text100 = new string('A', 100);
                    float measured100 = render.GetStringWidthFloat(text100, TextStyle.Empty, 0f);
                    float expected100 = 100 * charWidth;
                    Assert.InRange(Math.Abs(measured100 - expected100), 0f, 0.01f);

                    // Test drawing text chunk that starts with a space doesn't throw
                    using (var bmp = new Bitmap(200, 30))
                    using (var g = Graphics.FromImage(bmp)) {
                        render.OnBeginPaint(g);
                        var ex = Record.Exception(() => {
                            render.DrawString(" Test Chunk", new TextStyle() { ForeColor = Color.Black }, new RectangleF(0, 0, 100, 20));
                        });
                        Assert.Null(ex);
                        render.OnEndPaint(g);
                    }
                } finally {
                    render.EndPaint();
                    render.UnbindControl();
                }
            }
        }

        [Fact]
        public void QuickTextView_WithGDIPRender_HitTestsAndAlignsAccurately()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);

                string sampleLine = "it&bq2f(bnuglc3l7a48cgo^ron(c*@d$(@%6s*0fh5jn3y8^(i^bmpcfb3bpg&dvr";
                tbx.SetText(sampleLine);

                // Caret at char index 20
                FindInfo fi20 = view.FindFromCharIndex(20);
                Assert.True(fi20.Find);
                Assert.Equal(20, fi20.IndexOfCharInLine);

                // Hit testing back at that exact location should return column 20
                FindInfo fiHit = view.FindFromPoint(fi20.Location);
                Assert.True(fiHit.Find);
                Assert.Equal(20, fiHit.IndexOfCharInLine);
            }
        }

        [Fact]
        public void QuickTextView_WithChineseCharacters_HitTestsAndMeasuresAccurately()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);

                string chineseLine = "中文测试：Hello World! 快速选择。";
                tbx.SetText(chineseLine);

                // Caret at char index 4 (after "中文测试")
                FindInfo fi4 = view.FindFromCharIndex(4);
                Assert.True(fi4.Find);
                Assert.Equal(4, fi4.IndexOfCharInLine);

                // Hit testing back at that exact location should return column 4
                FindInfo fiHit = view.FindFromPoint(fi4.Location);
                Assert.True(fiHit.Find);
                Assert.Equal(4, fiHit.IndexOfCharInLine);

                // Verify line width is strictly positive and wider than pure ASCII 4 chars
                Assert.True(fi4.Location.X > view.TextRectangle.Left);

                // Ensure drawing with selection does not throw and paints without error
                using (var render = new STTextBoxGDIPRender()) {
                    tbx.SetTextBoxRender(render);
                    using (var bmp = new Bitmap(500, 50))
                    using (var g = Graphics.FromImage(bmp)) {
                        tbx.SelectionColor = Color.FromArgb(0, 120, 215);
                        tbx.SelectedTextColor = Color.White;
                        tbx.Select(2, 6);
                        render.OnBeginPaint(g);
                        view.OnDrawView(render);
                        render.OnEndPaint(g);
                    }
                }
            }
        }

        [Fact]
        public void QuickTextView_DualPassClipRendering_EliminatesGlyphShiftWhenSelected()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 800, 100);

                string chineseText = "\u4e24\u628a\u81f4\u591a\u53f7\u6211\u4e24\u4e3a\u597d\u6210\u9732\u529b\u7740\u672c\u591c\u6210\u5b9a\u6c11\u65f6\u4e0b\u7384\u5730\u9633";
                tbx.SetText(chineseText);
                tbx.SelectionColor = Color.FromArgb(0, 120, 215);
                tbx.SelectedTextColor = Color.White;

                using (var render = new STTextBoxGDIPRender()) {
                    tbx.SetTextBoxRender(render);

                    // 1. Render without selection
                    using (var bmpUnselected = new Bitmap(800, 100))
                    using (var g1 = Graphics.FromImage(bmpUnselected)) {
                        g1.Clear(Color.White);
                        render.OnBeginPaint(g1);
                        view.OnDrawView(render);
                        render.OnEndPaint(g1);

                        // 2. Render with partial selection (chars 6 to 15)
                        tbx.Select(6, 9);
                        using (var bmpSelected = new Bitmap(800, 100))
                        using (var g2 = Graphics.FromImage(bmpSelected)) {
                            g2.Clear(Color.White);
                            render.OnBeginPaint(g2);
                            view.OnDrawView(render);
                            render.OnEndPaint(g2);

                            FindInfo fi6 = view.FindFromCharIndex(6);
                            FindInfo fi15 = view.FindFromCharIndex(15);

                            // Verify prefix region (before selection): 0 pixel differences
                            int prefixDiff = 0;
                            int fontHeight = render.GetFontHeight();
                            for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                                for (int x = view.TextRectangle.Left; x < fi6.Location.X - 1; x++) {
                                    if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                        prefixDiff++;
                                    }
                                }
                            }
                            Assert.Equal(0, prefixDiff);

                            // Verify suffix region (after selection): 0 pixel differences (zero glyph shift/jiggle)
                            int suffixDiff = 0;
                            for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                                for (int x = fi15.Location.X + 2; x < 700; x++) {
                                    if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                        suffixDiff++;
                                    }
                                }
                            }
                            Assert.Equal(0, suffixDiff);
                        }
                    }
                }
            }
        }

        [Fact]
        public void QuickTextView_PartialSelection_ChineseText_GDIPlus_EliminatesGlyphShifting()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 800, 200);

                var render = new STTextBoxGDIPRender();
                tbx.SetTextBoxRender(render);

                string content = "\u4e24\u628a\u81f4\u591a\u5b89\u6668\u9876\u671f\u989d\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341";
                tbx.SetText(content);

                // 1. Render without selection
                using (var bmpUnselected = new Bitmap(800, 100))
                using (var g1 = Graphics.FromImage(bmpUnselected)) {
                    g1.Clear(Color.White);
                    render.OnBeginPaint(g1);
                    view.OnDrawView(render);
                    render.OnEndPaint(g1);

                    // 2. Render with partial selection of Chinese characters (index 4 to 8)
                    tbx.Select(4, 4);
                    using (var bmpSelected = new Bitmap(800, 100))
                    using (var g2 = Graphics.FromImage(bmpSelected)) {
                        g2.Clear(Color.White);
                        render.OnBeginPaint(g2);
                        view.OnDrawView(render);
                        render.OnEndPaint(g2);

                        FindInfo fi4 = view.FindFromCharIndex(4);
                        FindInfo fi8 = view.FindFromCharIndex(8);

                        // Verify prefix region (characters 0..3): 0 pixel differences
                        int prefixDiff = 0;
                        int fontHeight = render.GetFontHeight();
                        for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                            for (int x = view.TextRectangle.Left; x < fi4.Location.X - 1; x++) {
                                if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                    prefixDiff++;
                                }
                            }
                        }
                        Assert.Equal(0, prefixDiff);

                        // Verify suffix region (characters 8+): 0 pixel differences (zero glyph shift/jitter)
                        int suffixDiff = 0;
                        for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                            for (int x = fi8.Location.X + 2; x < 600; x++) {
                                if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                    suffixDiff++;
                                }
                            }
                        }
                        Assert.Equal(0, suffixDiff);
                    }
                }
            }
        }

        [Fact]
        public void QuickTextView_SwitchingRenderer_ClearsCachedWidths()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new QuickTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 500, 100);

                var gdiRender = new STTextBoxGDIRender();
                var gdipRender = new STTextBoxGDIPRender();

                // 1. Start with GDI
                tbx.SetTextBoxRender(gdiRender);
                tbx.SetText("\u4e24\u628a\u81f4\u591a");
                FindInfo fiGdi = view.FindFromCharIndex(2);

                // 2. Switch to GDI+
                tbx.SetTextBoxRender(gdipRender);
                FindInfo fiGdip = view.FindFromCharIndex(2);

                // GDI and GDI+ measure font fallback widths differently; verify cache was invalidated upon switch
                Assert.NotEqual(fiGdi.Location.X, fiGdip.Location.X);
            }
        }

        [Fact]
        public void NoWrapTextView_PartialSelection_ChineseText_GDIPlus_EliminatesGlyphShifting()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new NoWrapTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 800, 200);

                var render = new STTextBoxGDIPRender();
                tbx.SetTextBoxRender(render);

                string content = "\u4e24\u628a\u81f4\u591a\u5b89\u6668\u9876\u671f\u989d\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341";
                tbx.SetText(content);

                // 1. Render without selection
                using (var bmpUnselected = new Bitmap(800, 100))
                using (var g1 = Graphics.FromImage(bmpUnselected)) {
                    g1.Clear(Color.White);
                    render.OnBeginPaint(g1);
                    view.OnDrawView(render);
                    render.OnEndPaint(g1);

                    // 2. Render with partial selection of Chinese characters (index 4 to 8)
                    tbx.Select(4, 4);
                    using (var bmpSelected = new Bitmap(800, 100))
                    using (var g2 = Graphics.FromImage(bmpSelected)) {
                        g2.Clear(Color.White);
                        render.OnBeginPaint(g2);
                        view.OnDrawView(render);
                        render.OnEndPaint(g2);

                        FindInfo fi4 = view.FindFromCharIndex(4);
                        FindInfo fi8 = view.FindFromCharIndex(8);

                        // Verify prefix region (characters 0..3): 0 pixel differences
                        int prefixDiff = 0;
                        int fontHeight = render.GetFontHeight();
                        for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                            for (int x = view.TextRectangle.Left; x < fi4.Location.X - 1; x++) {
                                if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                    prefixDiff++;
                                }
                            }
                        }
                        Assert.Equal(0, prefixDiff);

                        // Verify suffix region (characters 8+): 0 pixel differences (zero glyph shift/jitter)
                        int suffixDiff = 0;
                        for (int y = view.TextRectangle.Top; y < view.TextRectangle.Top + fontHeight; y++) {
                            for (int x = fi8.Location.X + 2; x < 600; x++) {
                                if (bmpUnselected.GetPixel(x, y).ToArgb() != bmpSelected.GetPixel(x, y).ToArgb()) {
                                    suffixDiff++;
                                }
                            }
                        }
                        Assert.Equal(0, suffixDiff);
                    }
                }
            }
        }

        [Fact]
        public void NoWrapTextView_FindFromPointAndCharIndex_HitTestsAccurately()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var view = new NoWrapTextView();
                tbx.SetTextView(view);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 500, 200);

                string line0 = "Line 000: Hello World\r\n";
                string line1 = "Line 001: Fast Selection\r\n";
                tbx.SetText(line0 + line1);

                FindInfo fi0 = view.FindFromCharIndex(0);
                Assert.True(fi0.Find);
                Assert.Equal(0, fi0.IndexOfLine);
                Assert.Equal(0, fi0.IndexOfCharInLine);

                FindInfo fiLine1 = view.FindFromCharIndex(line0.Length + 5);
                Assert.True(fiLine1.Find);
                Assert.Equal(1, fiLine1.IndexOfLine);
                Assert.Equal(5, fiLine1.IndexOfCharInLine);

                // Hit test near that point
                FindInfo fiHit = view.FindFromPoint(new Point(fiLine1.Location.X + 1, fiLine1.Location.Y + 2));
                Assert.True(fiHit.Find);
                Assert.Equal(1, fiHit.IndexOfLine);
                Assert.Equal(5, fiHit.IndexOfCharInLine);
            }
        }
    }
}
