using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ST.Library.UI.STTextBox
{
    public class STTextBoxScrollInfo
    {
        private int _DisplayTime;
        public int DisplayTime {
            get { return _DisplayTime; }
            set {
                if (value < 1) {
                    throw new ArgumentException("The value must be more than 1");
                }
                _DisplayTime = value;
            }
        }

        internal int CountDown { get; set; }
        public int XValue { get; internal set; }
        public int YValue { get; internal set; }
        public int MaxXValue { get; set; }
        public int MaxYValue { get; set; }
        public int XIncrement { get; set; }
        public int XOffset { get { return XValue * XIncrement; } }
        public int Size { get; internal set; }
        public ScrollBarType HoverScrollBar { get; internal set; }
        public ScrollBarType DownScrollBar { get; internal set; }
        public Rectangle VThumbRect { get; internal set; }
        public Rectangle HThumbRect { get; internal set; }
        public Rectangle VBackRect { get; internal set; }
        public Rectangle HBackRect { get; internal set; }

        public enum ScrollBarType { None, V, H }

        public STTextBoxScrollInfo() {
            this._DisplayTime = 3;
            this.Size = 10;
            this.XIncrement = 10;
        }
    }
}
