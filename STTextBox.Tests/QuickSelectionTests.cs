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
    }
}
