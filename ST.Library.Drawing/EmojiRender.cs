using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.STTextBox;
using ST.Library.Drawing.SvgRender;

using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ST.Library.Drawing
{
    public class EmojiRender : IEmojiRender, IDisposable
    {
        private struct SvgSpan
        {
            public readonly int Offset;
            public readonly int Length;
            public SvgSpan(int offset, int length) {
                this.Offset = offset;
                this.Length = length;
            }
        }

        private const string FALLBACK_NONE_XML = "<svg viewBox='0,0,20,20'><rect stroke='red' fill='yellow' x='4' y='2' width='12' height='16'/><path stroke='red' fill='none' d='M8,8 v-2 h4 v4 h-2 v2'/><line stroke='red' x1='8' y1='14' x2='12' y2='14'/></svg>";
        private const int MAX_CACHE_SIZE = 128;

        private FileStream m_file_stream;
        private readonly object m_stream_lock = new object();
        private Dictionary<string, SvgSpan> m_dic_spans = new Dictionary<string, SvgSpan>();
        private Dictionary<string, CacheInfo> m_dic_cache = new Dictionary<string, CacheInfo>();
        private LinkedList<string> m_cache_lru = new LinkedList<string>();

        private struct CacheInfo
        {
            public int Size;
            public Image Image;
            public Image SelectedImage;
        }

        private ImageAttributes m_img_attr_normal;
        private ImageAttributes m_img_attr_selected;

        public EmojiRender(string strFile) {
            ColorMatrix m = new ColorMatrix(new float[][]{
                    new float[]{1, 0, 0, 0, 0},
                    new float[]{0, 1, 0, 0, 0},
                    new float[]{0, 0, 1, 0, 0},
                    new float[]{0, 0, 0, 1, 0},
                    new float[]{0, 0, 0, 0, 1},
                });
            m_img_attr_normal = new ImageAttributes();
            m_img_attr_normal.SetColorMatrix(m);
            m[3, 3] = 0.5F;
            m_img_attr_selected = new ImageAttributes();
            m_img_attr_selected.SetColorMatrix(m);
            this.InitSvgFromPackage(strFile);
        }

        public bool IsEmoji(string strChar) {
            return this.GetEmojiString(strChar) != null;
        }

        private string GetEmojiString(string strText) {
            if (string.IsNullOrEmpty(strText)) return null;
            if (strText == "none") return "none";
            if (m_dic_spans.ContainsKey(strText)) {
                return strText;
            }
            // http://www.unicode.org/charts/PDF/UFE00.pdf
            // FE00 - FE0F is the Variation Selectors
            // FE0E -> text variation selector
            // FE0F -> emoji variation selector
            if (strText.IndexOf('\uFE0F') == -1) {
                if (m_dic_spans.ContainsKey(strText + "\uFE0F")) {
                    return strText + "\uFE0F";
                }
                return null;
            }
            strText = strText.Replace("\uFE0F", "");
            if (m_dic_spans.ContainsKey(strText)) {
                return strText;
            }
            return null;
        }

        public void DrawEmoji(ISTTextBoxRender render, string strChar, int nX, int nY, int nWidth, bool bSelected) {
            strChar = this.GetEmojiString(strChar);
            CacheInfo ci;
            if (string.IsNullOrEmpty(strChar)) {
                strChar = "none";
            }
            if (m_dic_cache.TryGetValue(strChar, out ci)) {
                if (ci.Size != nWidth) {
                    if (ci.Image != null) ci.Image.Dispose();
                    if (ci.SelectedImage != null) ci.SelectedImage.Dispose();
                    ci.Size = nWidth;
                    ci.Image = this.GetEmojiImage(strChar, nWidth);
                    ci.SelectedImage = this.SetAlpha((Bitmap)ci.Image.Clone());
                    m_dic_cache[strChar] = ci;
                }
            } else {
                if (m_dic_cache.Count >= MAX_CACHE_SIZE && m_cache_lru.First != null) {
                    string oldest = m_cache_lru.First.Value;
                    m_cache_lru.RemoveFirst();
                    if (m_dic_cache.TryGetValue(oldest, out CacheInfo oldCi)) {
                        if (oldCi.Image != null) oldCi.Image.Dispose();
                        if (oldCi.SelectedImage != null) oldCi.SelectedImage.Dispose();
                        m_dic_cache.Remove(oldest);
                    }
                }
                ci.Size = nWidth;
                ci.Image = this.GetEmojiImage(strChar, nWidth);
                ci.SelectedImage = this.SetAlpha((Bitmap)ci.Image.Clone());
                m_dic_cache[strChar] = ci;
                m_cache_lru.AddLast(strChar);
            }
            Rectangle rect = new Rectangle(nX, nY, nWidth, nWidth);
            render.DrawImage(bSelected ? ci.SelectedImage : ci.Image, rect);
        }

        public Image GetEmojiImage(string strChar, int nSize) {
            strChar = this.GetEmojiString(strChar);
            if (strChar == null && strChar != "none") {
                return null;
            }
            string xml = null;
            if (strChar == "none") {
                xml = FALLBACK_NONE_XML;
            } else if (m_dic_spans.TryGetValue(strChar, out SvgSpan span)) {
                lock (m_stream_lock) {
                    if (m_file_stream != null) {
                        m_file_stream.Seek(span.Offset, SeekOrigin.Begin);
                        byte[] buf = new byte[span.Length];
                        int read = m_file_stream.Read(buf, 0, span.Length);
                        xml = Encoding.UTF8.GetString(buf, 0, read);
                    }
                }
            }
            if (string.IsNullOrEmpty(xml)) {
                xml = FALLBACK_NONE_XML;
            }

            Bitmap bmp = new Bitmap(nSize, nSize);
            using (Graphics g = Graphics.FromImage(bmp)) {
                using (SvgDocument svg = SvgDocument.FromXml(xml)) {
                    svg.Draw(g, new RectangleF(0, 0, nSize, nSize));
                }
            }
            return bmp;
        }

        public Image SetAlpha(Bitmap bmp) {
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            byte[] byClr = new byte[bmpData.Stride * bmpData.Height];
            Marshal.Copy(bmpData.Scan0, byClr, 0, byClr.Length);
            for (int x = 0; x < bmp.Width; x++) {
                for (int y = 0; y < bmp.Height; y++) {
                    int nIndex = y * bmpData.Stride + x * 4 + 3;
                    byClr[nIndex] = (byte)(byClr[nIndex] >> 1);
                }
            }
            Marshal.Copy(byClr, 0, bmpData.Scan0, byClr.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private int InitSvgFromPackage(string strFile) {
            if (m_file_stream != null) {
                m_file_stream.Dispose();
                m_file_stream = null;
            }
            m_file_stream = new FileStream(strFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            m_dic_spans.Clear();

            byte[] buffer = new byte[65536];
            long filePos = 0;

            int bomRead = m_file_stream.Read(buffer, 0, 3);
            if (bomRead == 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) {
                filePos = 3;
            } else {
                m_file_stream.Seek(0, SeekOrigin.Begin);
                filePos = 0;
            }

            List<byte> lineBytes = new List<byte>(4096);
            long lineStartPos = filePos;
            int commaIndexInLine = -1;

            int bytesRead;
            while ((bytesRead = m_file_stream.Read(buffer, 0, buffer.Length)) > 0) {
                for (int i = 0; i < bytesRead; i++) {
                    byte b = buffer[i];
                    long currentBytePos = filePos + i;
                    if (b == (byte)'\r' || b == (byte)'\n') {
                        if (lineBytes.Count > 0 && commaIndexInLine >= 0) {
                            string hex = Encoding.ASCII.GetString(lineBytes.ToArray(), 0, commaIndexInLine);
                            int xmlOffset = (int)(lineStartPos + commaIndexInLine + 1);
                            int xmlLen = lineBytes.Count - (commaIndexInLine + 1);
                            string key = this.GetString(hex);
                            m_dic_spans[key] = new SvgSpan(xmlOffset, xmlLen);
                        }
                        lineBytes.Clear();
                        commaIndexInLine = -1;
                        lineStartPos = currentBytePos + 1;
                    } else {
                        if (b == (byte)',' && commaIndexInLine == -1) {
                            commaIndexInLine = lineBytes.Count;
                        }
                        lineBytes.Add(b);
                    }
                }
                filePos += bytesRead;
            }
            if (lineBytes.Count > 0 && commaIndexInLine >= 0) {
                string hex = Encoding.ASCII.GetString(lineBytes.ToArray(), 0, commaIndexInLine);
                int xmlOffset = (int)(lineStartPos + commaIndexInLine + 1);
                int xmlLen = lineBytes.Count - (commaIndexInLine + 1);
                string key = this.GetString(hex);
                m_dic_spans[key] = new SvgSpan(xmlOffset, xmlLen);
            }
            return m_dic_spans.Count;
        }

        public string GetString(string strText) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strText.Length; i += 4) {
                sb.Append((char)(Convert.ToInt32(strText.Substring(i, 4), 16)));
            }
            return sb.ToString();
        }
        /// <summary>
        /// Package all svg files to one file
        /// </summary>
        /// <param name="strSvgFilesPath">The svg path</param>
        /// <param name="strFileOut">The out file</param>
        /// <returns>Svg count</returns>
        public static int PackageSvgFiles(string strSvgFilesPath, string strFileOut) {
            int nCounter = 0;
            Regex reg_start = new Regex(@"^\s+|\s+$", RegexOptions.Multiline);
            using (StreamWriter writer = new StreamWriter(strFileOut, false, Encoding.UTF8)) {
                foreach (var v in Directory.GetFiles(strSvgFilesPath, "*.svg")) {
                    string strName = Path.GetFileNameWithoutExtension(v);
                    string strText = EmojiRender.FileNameToUnicode(strName);
                    string strXml = File.ReadAllText(v).Trim();
                    strXml = reg_start.Replace(strXml, "").Replace("\r", "").Replace("\n", "");
                    writer.WriteLine(strText + "," + strXml);
                    nCounter++;
                }
            }
            return nCounter;
        }

        private static string FileNameToUnicode(string strName) {
            //((strText[nIndex] & 0x03FF) << 10) + (strText[nIndex + 1] & 0x03FF) + 0x10000;
            strName.ToLower();
            if (strName[0] == 'u') {
                strName = strName.Substring(1);
            }
            string strRet = string.Empty;
            foreach (var v in strName.Split('-', '_')) {
                int number = Convert.ToInt32(v, 16);
                if (number < 0x10000) {
                    strRet += number.ToString("X4");
                } else {
                    number &= 0xFFFF;
                    int nH = (number >> 10) | 0xD800;       //D800-DBFF -> hight
                    int nL = (number & 0x03FF) | 0xDC00;    //DC00-DEFF -> low
                    strRet += nH.ToString("X4");
                    strRet += nL.ToString("X4");
                }
            }
            return strRet;
        }

        public void Dispose() {
            if (m_file_stream != null) {
                m_file_stream.Dispose();
                m_file_stream = null;
            }
            if (m_dic_cache != null) {
                foreach (var ci in m_dic_cache.Values) {
                    if (ci.Image != null) ci.Image.Dispose();
                    if (ci.SelectedImage != null) ci.SelectedImage.Dispose();
                }
                m_dic_cache.Clear();
            }
            if (m_cache_lru != null) {
                m_cache_lru.Clear();
            }
            if (m_dic_spans != null) {
                m_dic_spans.Clear();
            }
            if (m_img_attr_normal != null) {
                m_img_attr_normal.Dispose();
                m_img_attr_normal = null;
            }
            if (m_img_attr_selected != null) {
                m_img_attr_selected.Dispose();
                m_img_attr_selected = null;
            }
        }
    }
}
