using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ST.Library.UI.STTextBox;

namespace STTextBoxTest.Demos
{
    public partial class FrmQuickSelectionDemo : Form
    {
        private STTextBox m_tbx;
        private QuickTextView m_quickView;
        private NoWrapTextView m_standardView;
        private STTextBoxGDIRender m_gdiRender;
        private STTextBoxGDIPRender m_gdipRender;

        private ToolStripStatusLabel m_lblCaret;
        private ToolStripStatusLabel m_lblSelection;
        private ToolStripStatusLabel m_lblStats;
        private ToolStripComboBox m_cboSize;
        private ToolStripComboBox m_cboRenderer;
        private ToolStripComboBox m_cboView;
        private ToolStripComboBox m_cboTheme;

        public FrmQuickSelectionDemo() {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "QuickTextbox High-Performance Selection Demo (from QuickTextboxPrototype)";
            this.Font = new Font("Segoe UI", 9f);
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            SetupControls();
            PopulateDummyText(60, 120);

            // Select a sample range initially to highlight contrasting white text on accent blue
            m_tbx.Select();
            int startChar = 5 * 122 + 10;
            int endChar = 9 * 122 + 75;
            // Set selection on STTextBox
            SelectRange(startChar, endChar);
        }

        private void SetupControls() {
            // Main ToolStrip at top
            ToolStrip toolStrip = new ToolStrip();
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.RenderMode = ToolStripRenderMode.System;

            ToolStripButton btnRegen = new ToolStripButton("Regenerate Text");
            btnRegen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRegen.Font = new Font(toolStrip.Font, FontStyle.Bold);
            btnRegen.Click += (s, ev) => OnRegenerateClicked();
            toolStrip.Items.Add(btnRegen);

            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(new ToolStripLabel("Size:"));
            m_cboSize = new ToolStripComboBox();
            m_cboSize.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cboSize.Items.AddRange(new object[] {
                "60 lines x 120 chars (ASCII Prototype Default)",
                "150 lines x 120 chars (ASCII)",
                "300 lines x 140 chars (ASCII)",
                "1000 lines x 120 chars (ASCII)",
                "60 lines x 50 chars (Chinese / 中文)",
                "300 lines x 50 chars (Chinese / 中文)",
                "100 lines (Mixed English & Chinese / 中英混合)"
            });
            m_cboSize.SelectedIndex = 0;
            m_cboSize.SelectedIndexChanged += (s, ev) => OnRegenerateClicked();
            toolStrip.Items.Add(m_cboSize);

            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(new ToolStripLabel("Renderer:"));
            m_cboRenderer = new ToolStripComboBox();
            m_cboRenderer.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cboRenderer.Items.AddRange(new object[] {
                "GDI (Fast TextRenderer)",
                "GDI+ (Graphics.DrawString)"
            });
            m_cboRenderer.SelectedIndex = 0;
            m_cboRenderer.SelectedIndexChanged += (s, ev) => OnRendererChanged();
            toolStrip.Items.Add(m_cboRenderer);

            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(new ToolStripLabel("View:"));
            m_cboView = new ToolStripComboBox();
            m_cboView.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cboView.Items.AddRange(new object[] {
                "QuickTextView (Cached & Chunk Drawing)",
                "NoWrapTextView (Standard)"
            });
            m_cboView.SelectedIndex = 0;
            m_cboView.SelectedIndexChanged += (s, ev) => OnViewChanged();
            toolStrip.Items.Add(m_cboView);

            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(new ToolStripLabel("Theme:"));
            m_cboTheme = new ToolStripComboBox();
            m_cboTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            m_cboTheme.Items.AddRange(new object[] {
                "Windows Accent (Blue / White)",
                "Emerald (Green / White)",
                "Crimson (Red / White)",
                "Amber Gold (Gold / Black)",
                "Neon Cyan (Cyan / Black)"
            });
            m_cboTheme.SelectedIndex = 0;
            m_cboTheme.SelectedIndexChanged += (s, ev) => OnThemeChanged();
            toolStrip.Items.Add(m_cboTheme);

            this.Controls.Add(toolStrip);

            // Bottom StatusStrip
            StatusStrip statusStrip = new StatusStrip();
            m_lblCaret = new ToolStripStatusLabel("Caret: Line 1, Col 1");
            m_lblSelection = new ToolStripStatusLabel(" | Selection: None");
            m_lblStats = new ToolStripStatusLabel(" | Mode: High-Speed GDI + QuickTextView");
            m_lblStats.Spring = true;
            m_lblStats.TextAlign = ContentAlignment.MiddleRight;

            statusStrip.Items.AddRange(new ToolStripItem[] { m_lblCaret, m_lblSelection, m_lblStats });
            this.Controls.Add(statusStrip);

            // STTextBox in Center
            m_tbx = new STTextBox();
            m_tbx.Dock = DockStyle.Fill;
            m_tbx.Font = new Font("Consolas", 10.5f);
            m_tbx.SelectionColor = Color.FromArgb(0, 120, 215);
            m_tbx.SelectedTextColor = Color.White;

            m_quickView = new QuickTextView();
            m_standardView = new NoWrapTextView();

            m_gdiRender = new STTextBoxGDIRender();
            m_gdipRender = new STTextBoxGDIPRender();

            m_tbx.SetTextBoxRender(m_gdiRender);
            m_tbx.SetTextView(m_quickView);

            // Event updates
            m_tbx.MouseMove += (s, ev) => UpdateStatus();
            m_tbx.KeyDown += (s, ev) => UpdateStatus();
            m_tbx.KeyUp += (s, ev) => UpdateStatus();
            m_tbx.MouseUp += (s, ev) => UpdateStatus();

            this.Controls.Add(m_tbx);
            m_tbx.BringToFront();
        }

        private void OnRegenerateClicked() {
            int lines = 60;
            int cols = 120;
            int mode = m_cboSize.SelectedIndex;
            switch (mode) {
                case 0: lines = 60; cols = 120; break;
                case 1: lines = 150; cols = 120; break;
                case 2: lines = 300; cols = 140; break;
                case 3: lines = 1000; cols = 120; break;
                case 4: lines = 60; cols = 50; break;
                case 5: lines = 300; cols = 50; break;
                case 6: lines = 100; cols = 80; break;
            }
            if (mode >= 4) {
                PopulateChineseText(lines, cols, mode == 6);
            } else {
                PopulateDummyText(lines, cols);
            }
            UpdateStatus();
        }

        private void OnRendererChanged() {
            if (m_cboRenderer.SelectedIndex == 0) {
                m_tbx.SetTextBoxRender(m_gdiRender);
            } else {
                m_tbx.SetTextBoxRender(m_gdipRender);
            }
            m_tbx.Invalidate();
            UpdateStatus();
        }

        private void OnViewChanged() {
            if (m_cboView.SelectedIndex == 0) {
                m_tbx.SetTextView(m_quickView);
            } else {
                m_tbx.SetTextView(m_standardView);
            }
            m_tbx.Invalidate();
            UpdateStatus();
        }

        private void OnThemeChanged() {
            switch (m_cboTheme.SelectedIndex) {
                case 0: // Windows Blue + White
                    m_tbx.SelectionColor = Color.FromArgb(0, 120, 215);
                    m_tbx.SelectedTextColor = Color.White;
                    break;
                case 1: // Emerald Green + White
                    m_tbx.SelectionColor = Color.FromArgb(16, 124, 65);
                    m_tbx.SelectedTextColor = Color.White;
                    break;
                case 2: // Crimson Red + White
                    m_tbx.SelectionColor = Color.FromArgb(196, 43, 28);
                    m_tbx.SelectedTextColor = Color.White;
                    break;
                case 3: // Amber Gold + Black
                    m_tbx.SelectionColor = Color.FromArgb(255, 185, 0);
                    m_tbx.SelectedTextColor = Color.Black;
                    break;
                case 4: // Neon Cyan + Black
                    m_tbx.SelectionColor = Color.FromArgb(0, 204, 255);
                    m_tbx.SelectedTextColor = Color.Black;
                    break;
            }
            m_tbx.Invalidate();
        }

        private void PopulateDummyText(int lines, int lineLength) {
            StringBuilder s = new StringBuilder();
            Random r = new Random(42); // fixed seed for reproducible pleasant text matrix
            string char_set = "abcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            for (int i = 0; i < lines; i++) {
                char[] line_chars = new char[lineLength];
                for (int j = 0; j < lineLength; j++) {
                    int rand_index = r.Next(char_set.Length);
                    line_chars[j] = char_set[rand_index];
                }
                s.AppendLine(new string(line_chars));
            }
            m_tbx.SetText(s.ToString());
        }

        private void PopulateChineseText(int lines, int lineLength, bool mixed) {
            StringBuilder s = new StringBuilder();
            Random r = new Random(42);
            string cjkChars = "的一是在不了有和人这中大为上个国我以要他时来用们生到作地于出就分对成会可主发年动同工也能下过子说产种面而方后多定行学法所民得经十三之进着等部度家电力里如水化高自二理起小物现实加量都两体制机当使点从业本去把性好应开它天地玄黄宇宙洪荒日月盈昃辰宿列张寒来暑往秋收冬藏闰余成岁律吕调阳云腾致雨露结为霜金生丽水玉出昆冈剑号巨阙珠称夜光";
            string asciiChars = "abcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*() ";
            for (int i = 0; i < lines; i++) {
                if (mixed) {
                    StringBuilder line = new StringBuilder();
                    line.Append(string.Format("{0}. ", i + 1));
                    while (line.Length < lineLength) {
                        if (r.Next(2) == 0) {
                            int phraseLen = r.Next(2, 6);
                            for (int p = 0; p < phraseLen && line.Length < lineLength; p++) {
                                line.Append(cjkChars[r.Next(cjkChars.Length)]);
                            }
                        } else {
                            int wordLen = r.Next(3, 8);
                            for (int w = 0; w < wordLen && line.Length < lineLength; w++) {
                                line.Append(asciiChars[r.Next(asciiChars.Length)]);
                            }
                            line.Append(' ');
                        }
                    }
                    s.AppendLine(line.ToString());
                } else {
                    char[] line_chars = new char[lineLength];
                    for (int j = 0; j < lineLength; j++) {
                        line_chars[j] = cjkChars[r.Next(cjkChars.Length)];
                    }
                    s.AppendLine(new string(line_chars));
                }
            }
            m_tbx.SetText(s.ToString());
        }

        private void SelectRange(int start, int end) {
            // Helper to set selection on STTextBox
            var c = typeof(STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(m_tbx) as STTextBox.Core;
            if (c != null) {
                c.Selection.SetSelection(start, end);
                c.Caret.IndexOfChar = end;
                c.ITextView.SetCaretPostion(end);
                m_tbx.Invalidate();
            }
        }

        private void UpdateStatus() {
            var c = typeof(STTextBox).GetField("m_core", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(m_tbx) as STTextBox.Core;
            if (c != null) {
                m_lblCaret.Text = string.Format("Caret: Line {0}, Offset {1}", c.Caret.IndexOfLine + 1, c.Caret.IndexOfChar);
                if (c.Selection.IsEmptySelection) {
                    m_lblSelection.Text = " | Selection: None";
                } else {
                    m_lblSelection.Text = string.Format(" | Selection: [{0}..{1}] ({2} chars)", c.Selection.StartIndex, c.Selection.EndIndex, c.Selection.Length);
                }
                string rendererName = (m_cboRenderer.SelectedIndex == 0) ? "GDI TextRenderer" : "GDI+";
                string viewName = (m_cboView.SelectedIndex == 0) ? "QuickTextView" : "NoWrapTextView";
                m_lblStats.Text = string.Format("Mode: {0} + {1} | Total Lines: {2}", rendererName, viewName, c.TextManager.LineCount);
            }
        }
    }
}
