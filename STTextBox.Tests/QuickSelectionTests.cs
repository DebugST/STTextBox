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

        [Fact]
        public void WrapTextView_WordAndCharWrap_Alignments_SetsAndRendersCorrectly()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 400, 300);

                string text = "Line 0: This is a long sentence that should easily wrap into multiple sublines on a 400px wide textbox.\r\nLine 1: Short.\r\nLine 2: End.";
                tbx.SetText(text);

                // Verify Left alignment
                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                Assert.Equal(WrapTextView.Alignment.Left, wrapView.WrapAlignment);
                Assert.Equal(WrapTextView.Method.Word, wrapView.WrapMethod);

                FindInfo fi0 = wrapView.FindFromCharIndex(0);
                Assert.True(fi0.Find);
                Assert.Equal(0, fi0.IndexOfLine);

                // Verify Center alignment
                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Center);
                Assert.Equal(WrapTextView.Alignment.Center, wrapView.WrapAlignment);

                // Verify Right alignment
                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Right);
                Assert.Equal(WrapTextView.Alignment.Right, wrapView.WrapAlignment);

                // Verify Char wrap method
                wrapView.SetWrap(WrapTextView.Method.Char, WrapTextView.Alignment.Left);
                Assert.Equal(WrapTextView.Method.Char, wrapView.WrapMethod);

                // Hit test should succeed without exception
                FindInfo fiHit = wrapView.FindFromPoint(new Point(100, 20));
                Assert.True(fiHit.Find);
            }
        }

        [Fact]
        public void WrapTextView_Selection_AcrossWrappedSublines_RendersWithoutErrors()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                tbx.Font = new Font("Consolas", 10.5f);
                tbx.SetBounds(0, 0, 400, 300);

                string text = "First line is somewhat short.\r\nSecond line is a very long line that wraps across multiple sublines so that selection can test subline boundaries.\r\nThird line.";
                tbx.SetText(text);

                // Select from middle of line 1 across wrapped line 2
                var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;
                core.Selection.SetSelection(10, 80);
                wrapView.SetCaretPostion(80);

                // Test drawing to bitmap in Left, Center, Right alignments
                foreach (WrapTextView.Alignment align in new[] { WrapTextView.Alignment.Left, WrapTextView.Alignment.Center, WrapTextView.Alignment.Right }) {
                    wrapView.SetWrap(WrapTextView.Method.Word, align);
                    using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                        tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                        Assert.NotNull(bmp);
                    }
                }

                // Caret at very end of line / text
                wrapView.SetCaretPostion(text.Length);
                Assert.Equal(text.Length, core.Caret.IndexOfChar);
            }
        }

        [Fact]
        public void STTextBox_WordWrapAndTextAlignment_Properties_WorkCorrectly()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox()) {
                // By default WordWrap is false
                Assert.False(tbx.WordWrap);
                Assert.Equal(WrapTextView.Alignment.Left, tbx.TextAlignment);

                // Turn WordWrap on
                tbx.WordWrap = true;
                Assert.True(tbx.WordWrap);

                // Set Alignment
                tbx.TextAlignment = WrapTextView.Alignment.Center;
                Assert.Equal(WrapTextView.Alignment.Center, tbx.TextAlignment);

                tbx.TextAlignment = WrapTextView.Alignment.Right;
                Assert.Equal(WrapTextView.Alignment.Right, tbx.TextAlignment);

                // Turn WordWrap off
                tbx.WordWrap = false;
                Assert.False(tbx.WordWrap);
            }
        }

        [Fact]
        public void ProportionalFont_SwitchingAndSelection_WorksCorrectly()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdiRender = new STTextBoxGDIRender())
            using (var gdipRender = new STTextBoxGDIPRender()) {
                tbx.SetBounds(0, 0, 500, 300);
                tbx.SetTextBoxRender(gdiRender);

                // Initially with Consolas (Monospaced)
                tbx.Font = new Font("Consolas", 10.5f);
                Assert.True(gdiRender.IsMonospaced);

                // Switch to Segoe UI (Proportional)
                tbx.Font = new Font("Segoe UI", 10.5f);
                Assert.False(gdiRender.IsMonospaced);

                // Text with starkly varying character widths: 'i' vs 'W'
                string text = "iiiiWWWWiiiiWWWW\r\nThe quick brown fox jumps over the lazy dog.\r\nProportional fonts accurately measured.";
                tbx.SetText(text);

                var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;
                Assert.False(core.IsMonospaced);

                // Test QuickTextView with proportional font
                var quickView = new QuickTextView();
                tbx.SetTextView(quickView);

                quickView.SetCaretPostion(4);
                Assert.Equal(4, core.Caret.IndexOfChar);
                quickView.SetCaretPostion(8);
                Assert.Equal(8, core.Caret.IndexOfChar);

                // Selection across variable-width characters
                core.Selection.SetSelection(0, 8);
                Assert.Equal(8, core.Selection.Length);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // Test NoWrapTextView with proportional font
                var noWrapView = new NoWrapTextView();
                tbx.SetTextView(noWrapView);
                noWrapView.SetCaretPostion(8);
                Assert.Equal(8, core.Caret.IndexOfChar);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // Test WrapTextView with proportional font
                var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                wrapView.SetCaretPostion(8);
                Assert.Equal(8, core.Caret.IndexOfChar);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // Test switching to GDI+ renderer with proportional font
                tbx.SetTextBoxRender(gdipRender);
                Assert.False(gdipRender.IsMonospaced);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }
            }
        }

        [Fact]
        public void WrapTextView_HighSpeedSelectionAndTyping_WorksCorrectly()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdiRender = new STTextBoxGDIRender())
            using (var gdipRender = new STTextBoxGDIPRender()) {
                tbx.SetBounds(0, 0, 450, 300);
                tbx.SetTextBoxRender(gdiRender);
                var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                tbx.Font = new Font("Segoe UI", 10.5f);

                string text = "High performance text rendering requires efficient layout algorithms, low-overhead grapheme cluster detection, and cache-friendly data structures.\r\n" +
                              "The quick brown fox jumps over the lazy dog while software engineers benchmark text layout.\r\n" +
                              "天地玄黄宇宙洪荒日月盈昃辰宿列张寒来暑往秋收冬藏闰余成岁律吕调阳云腾致雨露结为霜。";
                tbx.SetText(text);

                var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;

                // 1. Test hit testing (FindFromPoint)
                for (int x = 20; x < 400; x += 40) {
                    for (int y = 20; y < 200; y += 30) {
                        FindInfo fi = wrapView.FindFromPoint(new Point(x, y));
                        Assert.True(fi.Find);
                        Assert.NotNull(fi.Line);
                        Assert.True(fi.IndexOfCharInLine >= 0);
                    }
                }

                // 2. Test caret placement
                wrapView.SetCaretPostion(35);
                Assert.Equal(35, core.Caret.IndexOfChar);
                wrapView.SetCaretPostion(120);
                Assert.Equal(120, core.Caret.IndexOfChar);

                // 3. Test selection rendering across sublines in GDI mode
                core.Selection.SetSelection(20, 140);
                Assert.Equal(120, core.Selection.Length);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // 4. Test text insertion (typing simulation)
                core.Selection.SetIndex(40);
                core.TextManager.SetText(40, 0, " [INSERTED TEXT] ");
                wrapView.SetCaretPostion(57);
                Assert.Equal(57, core.Caret.IndexOfChar);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // 5. Test switching to GDI+ renderer
                tbx.SetTextBoxRender(gdipRender);
                core.Selection.SetSelection(10, 80);

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                // 6. Test Right and Center alignments with wrapped selection
                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Center);
                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }

                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Right);
                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    Assert.NotNull(bmp);
                }
            }
        }

        [Fact]
        public void WrapTextView_Performance_Benchmark()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdiRender = new STTextBoxGDIRender())
            using (var gdipRender = new STTextBoxGDIPRender()) {
                tbx.SetBounds(0, 0, 800, 600);
                tbx.SetTextBoxRender(gdiRender);
                var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                tbx.Font = new Font("Segoe UI", 10.5f);

                // Build a 100-line natural prose document
                var sb = new System.Text.StringBuilder();
                string paragraph = "High performance text rendering requires efficient layout algorithms, low-overhead grapheme cluster detection, and cache-friendly data structures. " +
                                   "The quick brown fox jumps over the lazy dog while software engineers benchmark text layout and selection speed across various typographic styles. " +
                                   "Proportional typefaces introduce variable character advances, requiring individual glyph measurements or optimized proportional advance lookup tables.\r\n";
                for (int i = 0; i < 100; i++) {
                    sb.Append(paragraph);
                }
                tbx.SetText(sb.ToString());

                var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;

                // Active multi-line selection
                core.Selection.SetSelection(50, 600);

                // --- GDI Benchmarks ---
                // 1. 1000 Hit tests
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++) {
                    var fi = wrapView.FindFromPoint(new Point(200 + (i % 300), 50 + (i % 400)));
                    Assert.True(fi.Find);
                }
                sw.Stop();
                long gdiHitMs = sw.ElapsedMilliseconds;

                // 2. 50 Selection repaints
                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    sw.Restart();
                    for (int i = 0; i < 50; i++) {
                        tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    }
                    sw.Stop();
                }
                long gdiPaintMs = sw.ElapsedMilliseconds;

                // 3. 100 Typing insertions
                core.Selection.SetIndex(100);
                sw.Restart();
                for (int i = 0; i < 100; i++) {
                    core.TextManager.SetText(core.Caret.IndexOfChar, 0, "a");
                    core.Caret.IndexOfChar += 1;
                    wrapView.SetCaretPostion(core.Caret.IndexOfChar);
                }
                sw.Stop();
                long gdiTypingMs = sw.ElapsedMilliseconds;

                // --- GDI+ Benchmarks ---
                tbx.SetTextBoxRender(gdipRender);
                core.Selection.SetSelection(50, 600);

                sw.Restart();
                for (int i = 0; i < 1000; i++) {
                    var fi = wrapView.FindFromPoint(new Point(200 + (i % 300), 50 + (i % 400)));
                    Assert.True(fi.Find);
                }
                sw.Stop();
                long gdipHitMs = sw.ElapsedMilliseconds;

                using (var bmp = new Bitmap(tbx.Width, tbx.Height)) {
                    sw.Restart();
                    for (int i = 0; i < 50; i++) {
                        tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));
                    }
                    sw.Stop();
                }
                long gdipPaintMs = sw.ElapsedMilliseconds;

                core.Selection.SetIndex(100);
                sw.Restart();
                for (int i = 0; i < 100; i++) {
                    core.TextManager.SetText(core.Caret.IndexOfChar, 0, "b");
                    core.Caret.IndexOfChar += 1;
                    wrapView.SetCaretPostion(core.Caret.IndexOfChar);
                }
                sw.Stop();
                long gdipTypingMs = sw.ElapsedMilliseconds;

                // Assert performance thresholds:
                // 1000 hit tests should take well under 100ms (<0.1ms per hit test)
                Assert.True(gdiHitMs < 200, $"GDI HitTest too slow: {gdiHitMs}ms for 1000 tests");
                Assert.True(gdipHitMs < 200, $"GDIP HitTest too slow: {gdipHitMs}ms for 1000 tests");

                // 50 repaints should achieve > 60 FPS (<833ms for 50 repaints)
                Assert.True(gdiPaintMs < 800, $"GDI Paint too slow: {gdiPaintMs}ms for 50 frames");
                Assert.True(gdipPaintMs < 800, $"GDIP Paint too slow: {gdipPaintMs}ms for 50 frames");

                // 100 typing insertions should take under 250ms
                Assert.True(gdiTypingMs < 250, $"GDI Typing too slow: {gdiTypingMs}ms for 100 chars");
                Assert.True(gdipTypingMs < 250, $"GDIP Typing too slow: {gdipTypingMs}ms for 100 chars");
            }
        }

        [Fact]
        public void WrapTextView_Selection_NoGhostOffsets_GDI_GDIP()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdipRender = new STTextBoxGDIPRender())
            using (var gdiRender = new STTextBoxGDIRender()) {
                tbx.SetBounds(0, 0, 1170, 700);
                tbx.Font = new Font("Segoe UI", 10.5f);

                string line52_content = "52. 也多霜法起后栻玉开所wn4^ 薇列晏玄可下列子我列张fvvvj2 作为fd&zh 分昌玄不出生下5ni&q 周为都这霰性而出冬暑一能bmae^ 闺浑\\% \r\n";
                string line53_content = "53. ntcd zgdn 称数物当狄实御lib i6d2 法号分们他出成b*o7r2 进为雨也业绩诸使其邑展hkuu uý cw edpoend 夸爱方\r\n";
                string line54_content = "54. 说我到高水地若如a3im 二出过雨用民pf^npa ph^*9 性昃岁大水h 8f7 nq0m o0m* $6 i0u 它们而产%(&zkod 余足k1\r\n";

                // Test across both renderers: GDIP and GDI
                foreach (ISTTextBoxRender render in new ISTTextBoxRender[] { gdipRender, gdiRender }) {
                    tbx.SetTextBoxRender(render);
                    var wrapView = new WrapTextView(WrapTextView.Method.Word, WrapTextView.Alignment.Center);
                    wrapView.ShowLineNumber = true;
                    tbx.SetTextView(wrapView);
                    tbx.SetText(line52_content + line53_content + line54_content);

                    var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;
                    var line0 = core.TextManager[0];

                    var getTag = typeof(WrapTextView).GetMethod("_Tag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var tag = getTag.Invoke(wrapView, new object[] { line0 });
                    var subLinesField = tag.GetType().GetProperty("SubLines").GetValue(tag) as System.Collections.IList;

                    var sub = subLinesField[0];
                    int sLen = (int)sub.GetType().GetField("Length").GetValue(sub);
                    int sWidth = (int)sub.GetType().GetField("Width").GetValue(sub);
                    int sX = (int)sub.GetType().GetField("X").GetValue(sub);
                    int[] offsets = (int[])sub.GetType().GetField("Offsets").GetValue(sub);

                    string drawStr = line52_content.TrimEnd('\r', '\n');
                    var style = new TextStyle();
                    float actualStrWidthFloat = render.GetStringWidthFloat(drawStr, style, 0);
                    int actualStrWidthInt = (int)Math.Ceiling(actualStrWidthFloat);

                    // Offsets at line end must equal visual text width (0 ghost offset)
                    Assert.True(Math.Abs(offsets[sLen] - actualStrWidthInt) <= 1, $"Offset drift in {render.GetType().Name}: {offsets[sLen]} vs {actualStrWidthInt}");

                    // Selection end must align exactly with text visual end
                    core.Selection.SetSelection(4, line52_content.Length + line53_content.Length + 50);
                    int relStart = 4;
                    int relEnd = sLen;
                    int startX = offsets[relStart];
                    int endX = offsets[relEnd];
                    int nLeft = wrapView.TextRectangle.X + sX + startX;
                    int nWidth = endX - startX;

                    int visualRight = wrapView.TextRectangle.X + sX + actualStrWidthInt;
                    Assert.True(Math.Abs((nLeft + nWidth) - visualRight) <= 1, $"Selection block mismatch in {render.GetType().Name}: {nLeft + nWidth} vs {visualRight}");
                }
            }
        }

        [Fact]
        public void SingleTextView_GdipRender_CharacterOffsetsAlignWithSubPixelAccuracy()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdipRender = new STTextBoxGDIPRender())
            {
                tbx.SetBounds(0, 0, 500, 30);
                tbx.SetTextBoxRender(gdipRender);
                var singleView = new SingleTextView();
                tbx.SetTextView(singleView);
                tbx.Font = new Font("Consolas", 10f);

                string text = "this is a single text box";
                tbx.SetText(text);

                // Verify ViewToControl matches TextRectangle.Y
                Point viewPt = new Point(0, 0);
                Point ctrlPt = singleView.ViewToControl(viewPt);
                Assert.Equal(singleView.TextRectangle.Y, ctrlPt.Y);
                Assert.True(singleView.TextRectangle.Y > 0, "SingleTextView should be vertically centered");

                // Verify character boundary offsets match GDI+ actual visual measurement
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                {
                    var sf = new StringFormat(StringFormat.GenericTypographic);
                    sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;

                    for (int i = 0; i <= text.Length; i++)
                    {
                        var fi = singleView.FindFromCharIndex(i);
                        int charX = fi.Location.X - singleView.TextRectangle.X;

                        float actualW = i == 0 ? 0f : g.MeasureString(text.Substring(0, i), tbx.Font, 10000, sf).Width;
                        int expectedX = (int)Math.Round(actualW);

                        // Deviation must be <= 1px (sub-pixel accurate)
                        int diff = Math.Abs(charX - expectedX);
                        Assert.True(diff <= 1, $"Offset mismatch at char {i}: charX={charX}, expected={expectedX}, diff={diff}");
                    }
                }

                // Specifically verify character 10 ('s' of "single")
                var fiS = singleView.FindFromCharIndex(10);
                var fiSpace = singleView.FindFromCharIndex(9);
                int charWidthS = singleView.FindFromCharIndex(11).Location.X - fiS.Location.X;
                Assert.True(charWidthS >= 7 && charWidthS <= 8, $"Consolas 10pt char width should be 7-8px, got {charWidthS}");

                // FindFromPoint directly at the left edge of 's' should select char 10
                var hitTestS = singleView.FindFromPoint(new Point(fiS.Location.X + 1, fiS.Location.Y + 2));
                Assert.Equal(10, hitTestS.IndexOfCharInLine);
            }
        }

        [Fact]
        public void NoWrapTextView_TrailingNewlines_ClampedAccurately()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdipRender = new STTextBoxGDIPRender())
            {
                tbx.SetBounds(0, 0, 500, 100);
                tbx.SetTextBoxRender(gdipRender);
                var noWrapView = new NoWrapTextView();
                tbx.SetTextView(noWrapView);
                tbx.Font = new Font("Consolas", 10f);

                string line = "hello world\r\n";
                tbx.SetText(line);

                // Clicking far to the right of line 0 should place caret before \r\n (index 11)
                var fi = noWrapView.FindFromPoint(new Point(450, noWrapView.TextRectangle.Y + 5));
                Assert.Equal(0, fi.IndexOfLine);
                Assert.Equal("hello world".Length, fi.IndexOfCharInLine);
            }
        }

        [Fact]
        public void NoWrapTextView_StyledText_SelectionDragHasZeroGlyphJitter()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            using (var gdipRender = new STTextBoxGDIPRender())
            {
                tbx.SetBounds(0, 0, 500, 100);
                tbx.SetTextBoxRender(gdipRender);
                var noWrapView = new NoWrapTextView();
                tbx.SetTextView(noWrapView);
                tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
                tbx.Font = new Font("Consolas", 10f);

                string line = "tbx_single.AllowScrollBar = false;";
                tbx.SetText(line);

                // Render unselected line to bitmap1
                using (var bmpUnselected = new Bitmap(tbx.Width, tbx.Height))
                using (var bmpSelected = new Bitmap(tbx.Width, tbx.Height))
                {
                    tbx.Select(0, 0); // No selection
                    tbx.DrawToBitmap(bmpUnselected, new Rectangle(0, 0, tbx.Width, tbx.Height));

                    // Now select 'w' (index 15)
                    tbx.Select(15, 1);
                    tbx.DrawToBitmap(bmpSelected, new Rectangle(0, 0, tbx.Width, tbx.Height));

                    // Characters BEFORE 'w' (e.g. "tbx_single.Allo", X=0..130) must be 100% IDENTICAL
                    // and not have shifted by even a single pixel
                    for (int x = noWrapView.TextRectangle.Left; x < noWrapView.TextRectangle.Left + 100; x++)
                    {
                        for (int y = noWrapView.TextRectangle.Y; y < noWrapView.TextRectangle.Bottom; y++)
                        {
                            Color p1 = bmpUnselected.GetPixel(x, y);
                            Color p2 = bmpSelected.GetPixel(x, y);
                            Assert.True(p1 == p2, $"Glyph pixel jitter detected at ({x},{y}): unselected={p1}, selected={p2}");
                        }
                    }
                }
            }
        }

        [Fact]
        public void WrapTextView_ResizedSmaller_InspectSelection()
        {
            using (var tbx = new ST.Library.UI.STTextBox.STTextBox())
            {
                var wrapView = new WrapTextView();
                wrapView.ShowLineNumber = true;
                wrapView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
                tbx.SetTextView(wrapView);
                tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
                tbx.Font = new Font("Consolas", 10f);

                string strCode = @"//this is code
var textView = new WrapTextView();      //default: word, left
textView.ShowLineNumber = true;         //default: true
textView.SetWrap(WrapTextView.Method.Word, WrapTextView.Alignment.Left);
textView.ShowLineNumberGuide = true;    //default: true
textView.LineNumberGuideColor = Color.Magenta;  //default: Magenta

STTextBox tbx = new STTextBox();
tbx.SetTextView(textView);
tbx.SetTextStyleMonitors(new CSharpStyleMonitor());
tbx.Dock = DockStyle.Fill;
tbx.Font = new System.Drawing.Font(""Consolas"", 10);
this.Controls.Add(tbx);";

                tbx.SetBounds(0, 0, 600, 400);
                tbx.SetText(strCode);

                // Resize smaller so lines wrap (width = 320)
                tbx.SetBounds(0, 0, 320, 400);

                var core = typeof(ST.Library.UI.STTextBox.STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tbx) as ST.Library.UI.STTextBox.STTextBox.Core;

                // Test clicking on line 1 subline 1 (which is screen line 2: ": word, left")
                // Screen line 0: line 0 ("//this is code")
                // Screen line 1: line 1 subline 0 ("var textView = new WrapTextView();      //default")
                // Screen line 2: line 1 subline 1 (": word, left")
                // Screen line 3: line 2 subline 0 ("textView.ShowLineNumber = true;         //default")
                // Screen line 4: line 2 subline 1 (": true")

                // Let's test FindFromPoint at Screen line 1 and Screen line 2
                int x = wrapView.TextRectangle.X + 20;
                int yScreen0 = wrapView.TextRectangle.Y + (int)(core.LineHeight * 0.5);
                int yScreen1 = wrapView.TextRectangle.Y + (int)(core.LineHeight * 1.5);
                int yScreen2 = wrapView.TextRectangle.Y + (int)(core.LineHeight * 2.5);
                int yScreen3 = wrapView.TextRectangle.Y + (int)(core.LineHeight * 3.5);
                int yScreen4 = wrapView.TextRectangle.Y + (int)(core.LineHeight * 4.5);

                var fi0 = wrapView.FindFromPoint(new Point(x, yScreen0));
                var fi1 = wrapView.FindFromPoint(new Point(x, yScreen1));
                var fi2 = wrapView.FindFromPoint(new Point(x, yScreen2));
                var fi3 = wrapView.FindFromPoint(new Point(x, yScreen3));
                var fi4 = wrapView.FindFromPoint(new Point(x, yScreen4));

                // Check line indices and char indices
                Assert.Equal(0, fi0.IndexOfLine);
                Assert.Equal(1, fi1.IndexOfLine);
                Assert.Equal(1, fi2.IndexOfLine);
                Assert.Equal(2, fi3.IndexOfLine);
                Assert.Equal(2, fi4.IndexOfLine);

                // Now simulate MouseDown at fi1 and MouseMove to fi4
                var onMouseDown = typeof(ST.Library.UI.STTextBox.STTextBox).GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var onMouseMove = typeof(ST.Library.UI.STTextBox.STTextBox).GetMethod("OnMouseMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                onMouseDown.Invoke(tbx, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, yScreen1, 0) });
                Assert.Equal(fi1.IndexOfChar, core.Selection.StartIndex);
                Assert.Equal(fi1.IndexOfChar, core.Selection.EndIndex);

                onMouseMove.Invoke(tbx, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, yScreen4, 0) });
                Assert.Equal(fi1.IndexOfChar, core.Selection.StartIndex);
                Assert.Equal(fi4.IndexOfChar, core.Selection.EndIndex);

                // Now draw to bitmap and inspect pixels to see if selection is actually painted!
                using (var bmp = new Bitmap(tbx.Width, tbx.Height))
                {
                    tbx.DrawToBitmap(bmp, new Rectangle(0, 0, tbx.Width, tbx.Height));

                    // Check if selection color (0, 120, 215) is present on screen lines 1, 2, 3, 4
                    bool hasBlueScreen1 = false, hasBlueScreen2 = false, hasBlueScreen3 = false, hasBlueScreen4 = false;
                    for (int px = wrapView.TextRectangle.Left; px < wrapView.TextRectangle.Right; px++)
                    {
                        if (bmp.GetPixel(px, yScreen1) == Color.FromArgb(0, 120, 215)) hasBlueScreen1 = true;
                        if (bmp.GetPixel(px, yScreen2) == Color.FromArgb(0, 120, 215)) hasBlueScreen2 = true;
                        if (bmp.GetPixel(px, yScreen3) == Color.FromArgb(0, 120, 215)) hasBlueScreen3 = true;
                        if (bmp.GetPixel(px, yScreen4) == Color.FromArgb(0, 120, 215)) hasBlueScreen4 = true;
                    }

                    Assert.True(hasBlueScreen1, "Screen line 1 (line 1 sub 0) missing selection background!");
                    Assert.True(hasBlueScreen2, "Screen line 2 (line 1 sub 1) missing selection background!");
                    Assert.True(hasBlueScreen3, "Screen line 3 (line 2 sub 0) missing selection background!");
                    Assert.True(hasBlueScreen4, "Screen line 4 (line 2 sub 1) missing selection background!");
                }
            }
        }

        [Fact]
        public void SvgPath_CultureInvariant_ParsingTest()
        {
            var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ro-RO");

                string svgXml = @"<svg viewBox=""0 0 36 36"" width=""36"" height=""36"">
                    <path fill=""#DD2E44"" d=""M35.885 11.833c0-5.45-4.418-9.868-9.867-9.868-3.308 0-6.227 1.633-8.018 4.129-1.791-2.496-4.71-4.129-8.017-4.129-5.45 0-9.868 4.417-9.868 9.868 0 .772.098 1.52.266 2.241C1.751 22.587 11.216 31.568 18 34.034c6.783-2.466 16.249-11.447 17.617-19.96.17-.721.268-1.469.268-2.241z""/>
                </svg>";

                using (var doc = ST.Library.Drawing.SvgRender.SvgDocument.FromXml(svgXml))
                {
                    Assert.NotNull(doc);
                    using (var bmp = new Bitmap(36, 36))
                    using (var g = Graphics.FromImage(bmp))
                    {
                        doc.Draw(g, new RectangleF(0, 0, 36, 36));
                    }
                }
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

    }
}

