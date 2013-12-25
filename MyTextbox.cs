using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace MyLibrary
{
    /// <summary>
    /// 自作テキストボックス
    /// ・ハイパーリンク
    /// ・
    /// </summary>
    public class MyTextbox : System.Windows.Forms.Control
    {
        #region Constants

        #endregion

        #region delegate
        public delegate void MyMouseEventHandler(object sender, MyMouseEventArgs e);
        public delegate void MyTextboxInfoEventHandler(object sender, MyTextboxInfoEventArgs e);
        #endregion

        #region Event
        public event MyMouseEventHandler MyMouseMove;
        public event MyMouseEventHandler MyMouseDown;
        public event MyTextboxInfoEventHandler MyTextboxInfoEvent;
        #endregion

        #region property
        /// <summary>
        /// 行数
        /// </summary>
        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }
        /// <summary>
        /// これが位置情報の主体。キャレットはあくまで現在地の目安。
        /// </summary>
        private Position _currentpos;
        /// <summary>
        /// 
        /// </summary>
        public Position CurrentPos
        {
            set
            {
                try
                {
                    MyLine line = list[value.Line];
                    if (value.Pos > line.Length)
                        value.Pos = line.Length;
                    else if (value.Pos < 0)
                        value.Pos = 0;
                    _currentpos = value;
                    Point point = Position2Point(_currentpos);
                    caret.SetPos(point.X, point.Y);
                    if (MyTextboxInfoEvent != null)
                    {
                        MyTextboxInfoEvent(this, SetInfoEventArgs());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            get
            {
                return _currentpos;
            }
        }
        /// <summary>
        /// 現在の行
        /// </summary>
        public MyLine CurrentLine
        {
            get
            {
                return list[CurrentPos.Line];
            }
        }
        #endregion

        #region variable
        /// <summary>
        /// 行のリスト
        /// </summary>
        List<MyLine> list;
        /// <summary>
        /// 改行コード
        /// </summary>
        MyReturnCode _returnCode;
        /// <summary>
        /// キャレット
        /// </summary>
        Caret caret;

        List<Task> history;
        #endregion

        #region override
        /// <summary>
        /// 文字列の設定/取得
        /// 外部との文字列のやりとりはこことInsert?
        /// </summary>
        public override string Text
        {
            set
            {
                list = String2MyLineList(value);
                Replace work = new Replace();
                history.Add(work);
            }
            get
            {
                string s = "";
                for (int i = 0; i < list.Count; i++)
                {
                    s += list[i].Text;
                }
                return s;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private List<MyLine> String2MyLineList(string str)
        {
            // オリジナルの改行コードを保持する必要がある。
            List<MyLine> tmp = new List<MyLine>();
            tmp.Add(new MyLine(str,new MyReturnCode(returnCode.NULL)));
            tmp = LineSplitter(new List<MyLine>(), tmp, new MyReturnCode(returnCode.CRLF));
            tmp = LineSplitter(new List<MyLine>(), tmp, new MyReturnCode(returnCode.CR));
            tmp = LineSplitter(new List<MyLine>(), tmp, new MyReturnCode(returnCode.LF));
            return tmp;
        }
        /// <summary>
        /// List&lt;MyLine&gt;の中のMyLine中にある文字列の改行コードを見つけて、MyLineを再帰的に分割する。
        /// </summary>
        /// <param name="ret">処理し終えた部分</param>
        /// <param name="tmp">未処理部分</param>
        /// <param name="returnCode"></param>
        /// <returns></returns>
        private List<MyLine> LineSplitter(List<MyLine> ret, List<MyLine> tmp, MyReturnCode returnCode)
        {
            // もう処理するものが無かったら、終わり
            if (tmp.Count == 0) return ret;
            else
            {
                // 未処理の配列の先頭行を取得し、未処理配列から消す。
                MyLine a = tmp[0];
                tmp.RemoveAt(0);

                // 文字列をreturnCodeで分割する。
                string[] arr = MyLibrary.MyStringTool.StringSplitter(a.TextWithoutReturnValue, returnCode.ToString());

                // 分割した文字列を1つずつ配列に変換する。
                for(int i = 0;i<arr.Length;++i)
                {
                    ret.Add(new MyLine(arr[i], returnCode));
                }
                // 処理中配列の最後の要素の改行コードはreturnCodeではなく、a.ReturnCodeである。
                ret[ret.Count - 1].ReturnCode = a.ReturnCode;

                return LineSplitter(ret, tmp, returnCode);
            }
        }
        /// <summary>
        /// WndProc
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            
            LogWriter logWriter = new LogWriter(@"C:\log\MyTextbox.txt");
            logWriter.Write(m.ToString());

            IntPtr hIMC;

            switch (m.Msg)
            {
                case NativeMethods.WM_CHAR:
                    hIMC = NativeMethods.ImmGetContext(this.Handle);
                    if (NativeMethods.ImmGetOpenStatus(hIMC) == 0)
                    {
                        char chr = Convert.ToChar(m.WParam.ToInt32() & 0xff);
                        Insert(chr.ToString());
//                        caret.SetPos(caret.GetPos().X + , caret.GetPos().Y);
                    }
                    NativeMethods.ImmReleaseContext(this.Handle, hIMC);
                    Invalidate();
                    break;
                case NativeMethods.WM_IME_STARTCOMPOSITION:
                    hIMC = NativeMethods.ImmGetContext(this.Handle);

                    // ImmSetCompositionWindowとImmSetCandidateWindowがしっかり機能しているのかがイマイチ分からない。
                    // 変換ウィンドウの位置を設定
                    NativeMethods.COMPOSITIONFORM cf = new NativeMethods.COMPOSITIONFORM();
                    cf.dwStyle = NativeMethods.CFS_POINT;
                    cf.ptCurrentPos = new Point(100, 0);
                    cf.rcArea = new Rectangle();                    
                    NativeMethods.ImmSetCompositionWindow(hIMC, ref cf);

                    // 候補文字ウィンドウの位置調整を行う
                    NativeMethods.CANDIDATEFORM lpCandidate = new NativeMethods.CANDIDATEFORM();
                    lpCandidate.dwIndex = 0;
                    lpCandidate.dwStyle = NativeMethods.CFS_CANDIDATEPOS;
                    lpCandidate.ptCurrentPos = new Point(10, 50);                    
                    NativeMethods.ImmSetCandidateWindow(hIMC, ref lpCandidate);

                    NativeMethods.ImmReleaseContext(this.Handle, hIMC);
                    break;
                case NativeMethods.WM_IME_COMPOSITION:
                    this.ImeComposition(m);
                    break;
                case NativeMethods.WM_IME_ENDCOMPOSITION:
                    
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    switch (m.WParam.ToInt32())
                    {
                        case NativeMethods.IMN_OPENCANDIDATE:
                            // 候補文字ウィンドウが表示された
                            this.SetCandidateWindowPos(m.HWnd);
                            break;
                        case NativeMethods.IMN_CLOSECANDIDATE:
                        case NativeMethods.IMN_CHANGECANDIDATE:
                        case NativeMethods.IMN_SETOPENSTATUS:
                            // 何もしない。
                            break;
                    }
                    break;

            }
            base.WndProc(ref m);
        }

        List<Task> deleteCandidate = new List<Task>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        private void ImeComposition(Message m)
        {
            for (int i = 0; i < deleteCandidate.Count; i++)
            {
                Delete(deleteCandidate[i].fromLine, deleteCandidate[i].fromPos, deleteCandidate[i].length);               
            }
            deleteCandidate.Clear();

            char[] buf;
            int len;
            if ((m.LParam.ToInt32() & NativeMethods.GCS_RESULTSTR) != 0)
            {
                // 文字列の変換状態が全て確定した
                // よって、確定文字の取得
                IntPtr hImc = NativeMethods.ImmGetContext(m.HWnd);

                // 必要なメモリサイズを取得
                len = NativeMethods.ImmGetCompositionString(hImc, NativeMethods.GCS_RESULTSTR, null, 0);
                buf = new char[len];

                NativeMethods.ImmGetCompositionString(hImc, NativeMethods.GCS_RESULTSTR, buf, (uint)len);

                string s = new string(buf);
                s = s.Replace("\0", "");

                Insert(s);
                Invalidate();
            }
            else if ((m.LParam.ToInt32() & NativeMethods.GCS_COMPSTR) != 0)
            {
                IntPtr hImc = NativeMethods.ImmGetContext(m.HWnd);

                // 必要なメモリサイズを取得
                len = NativeMethods.ImmGetCompositionString(hImc, NativeMethods.GCS_COMPSTR, null, 0);
                buf = new char[len];
                NativeMethods.ImmGetCompositionString(hImc, NativeMethods.GCS_COMPSTR, buf, (uint)len);
                string s = new string(buf);
                s = s.Replace("\0", "");
                Task work = Insert(s);
                deleteCandidate.Add(work);
            }
            Invalidate();
            return;
        }
        /// <summary>
        /// 候補文字ウィンドウの位置を設定
        /// </summary>
        /// <param name="hWnd"></param>
        private void SetCandidateWindowPos(IntPtr hWnd)
        {
            IntPtr hImc = NativeMethods.ImmGetContext(hWnd);
            if (hImc != null)
            {
                NativeMethods.CANDIDATEFORM cndFrm = new NativeMethods.CANDIDATEFORM();
                cndFrm.ptCurrentPos.X = caret.GetPos().X;
                cndFrm.ptCurrentPos.Y = caret.GetPos().Y;

                cndFrm.dwStyle = NativeMethods.CFS_CANDIDATEPOS;// 候補文字ウィンドウの位置を指定する。

                NativeMethods.ImmSetCandidateWindow(hImc, ref cndFrm);

                NativeMethods.ImmReleaseContext(hWnd, hImc);
            }


        }
        /// <summary>
        /// OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            float x = 0F;
            float y = 0F;
            for (int nLine = 0; nLine < list.Count; nLine++)
            {
                MyLine line = list[nLine];
                for (int nPos = 0; nPos < line.Length; nPos++)
                {
                    MyChar c = line[nPos];
                    e.Graphics.DrawString(c.ToString(), c.Font, c.Brush, x, y);
                    x += c.Width;
                }
                x = 0F;
                y += line.Height;
            }
            
            base.OnPaint(e);
        }
        /// <summary>
        /// OnGotFocus
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            this.caret.ShowCaret();
            this.BackColor = Color.LightBlue;
            base.OnGotFocus(e);
        }
        /// <summary>
        /// OnLostFocus
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            this.caret.HideCaret();
            this.BackColor = Color.White;
            base.OnLostFocus(e);
        }
        /// <summary>
        /// OnMouseDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (false && MyMouseDown != null)
            {
                MyMouseEventArgs myMouseEventArgs = new MyMouseEventArgs();
                myMouseEventArgs.X = Position2Point(CurrentPos).X;
                myMouseEventArgs.Y = Position2Point(CurrentPos).Y;
                myMouseEventArgs.nLine = CurrentPos.Line;
                myMouseEventArgs.nPos = CurrentPos.Pos;
                MyMouseDown(this, myMouseEventArgs);
            }
            // 文字ごとにサイズが異なることを考慮しないといけない。
            // 結構めんどうそうだ・・・まずは表示させるところからやるか・・・
            CurrentPos = Point2Position(new Point(e.X, e.Y));

            this.Focus();
            base.OnMouseDown(e);

            
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {

            if (MyMouseMove != null)
            {
                MyMouseEventArgs myMouseEventArgs = new MyMouseEventArgs();
                myMouseEventArgs.X = e.X;
                myMouseEventArgs.Y = e.Y;
                myMouseEventArgs.nLine = Point2Position(new Point(e.X, e.Y)).Line;
                myMouseEventArgs.nPos = Point2Position(new Point(e.X, e.Y)).Pos;
                MyMouseMove(this, myMouseEventArgs);
            }
            base.OnMouseMove(e);
        }
        /// <summary>
        /// 通常はこのコントロールで補足しないようなキーが押された場合にも発生するイベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            // すべてのキーが補足されるようにする。これをしないと矢印キーなどが補足されない。
            e.IsInputKey = true;
            base.OnPreviewKeyDown(e);
        }
        /// <summary>
        /// キーが押された時。特定のキーが押された時の動作を定義する。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    CurrentPos = new Position(CurrentPos.Line, CurrentPos.Pos + 1);
                    e.SuppressKeyPress = true;
//                    Invalidate();
                    break;
                case Keys.Left:
                    CurrentPos = new Position(CurrentPos.Line, CurrentPos.Pos - 1);
                    e.SuppressKeyPress = true;
//                    Invalidate();
                    break;
                case Keys.Enter:
                    Insert("\r\n");
                    
                    // Keyイベントをコントロールに渡さない場合はtrue。これをtrueにすると、同時にHandledもtrueとなる。
                    e.SuppressKeyPress = true;
                    // TODO 改行する処理
                    CurrentPos = new Position(CurrentPos.Line + 1, 0);
                    
//                    Invalidate();
                    break;
                case Keys.Back:
                    this.Delete(CurrentPos.Line, CurrentPos.Pos - 1, 1);
                    e.SuppressKeyPress = true;
                    // キャレットを一文字分後退。関数化すべき明らかに。現状、0点で実行すると-1になるバグあり
                    CurrentPos = new Position(CurrentPos.Line, CurrentPos.Pos -1);

//                    Invalidate();
                    break;
                case Keys.Tab:

                    break;
            }
            //base.OnKeyDown(e);
        }
        protected override void OnMove(EventArgs e)
        {
            vScrollBar.Size = new Size(vScrollBar.Width, this.Height - vScrollBar.Width);
            hScrollBar.Size = new Size(this.Width - hScrollBar.Height, hScrollBar.Height);
            base.OnMove(e);
        }
        #endregion


        #region ScrollBar
        /// <summary>
        /// 縦スクロールバー
        /// </summary>
        VScrollBar vScrollBar;
        /// <summary>
        /// 横スクロールバー
        /// </summary>
        HScrollBar hScrollBar;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public MyTextbox()
        {
            list = new List<MyLine>();
            MyLine firstLine = new MyLine();
            list.Add(firstLine);
            this.Font = MyTextboxDefaultValue.Font;
            this._returnCode = MyTextboxDefaultValue.ReturnCode;
            this.AllowDrop = true;
            this.DragDrop += new DragEventHandler(MyTextbox_DragDrop);
            caret = new Caret(this.Handle, 2, this.Font.Height);
            CurrentPos = new Position(0, 0);
            history = new List<Task>();
            vScrollBar = new VScrollBar();
            hScrollBar = new HScrollBar();

            vScrollBar.Dock = DockStyle.Right;
            hScrollBar.Dock = DockStyle.Bottom;
            this.Controls.Add(vScrollBar);
            this.Controls.Add(hScrollBar);
        }
        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MyTextbox()
        {

        }
        /// <summary>
        /// ピクセルから何行目何列かを取得する
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Position Point2Position(Point point)
        {
            int nLine = 0;
            int pos = 0;

            float testHeight = 0;
            for (int i = 0; i < list.Count; i++)
            {
                testHeight += list[i].Height;
                if (point.Y < testHeight)
                {
                    nLine = i;
                    break;
                }
            }

            MyLine line = list[nLine];
            if (point.X < 0)
                pos = 0;
            else if (point.X > line.Width)
                pos = line.Length;
            else
            {
                for (int i = 0; i < line.Length; i++)
                {
                    float testWidth = line.GetWidth(0, i);
                    if (point.X < (int)testWidth)
                    {
                        // 文字列のサイズの方が上回ったらそこがポジション
                        // 文字の真ん中だった場合とか細かいことはまだ考慮してない。
                        // とりあえずこんな感じだろうとテストしてみる。
                        pos = i;
                        MyChar c = line[i];
                        float t = c.Width / 2.0F;
                        float k = testWidth - t;
                        if (point.X > (int)k)
                        {
                            pos += 1;
                        }

                        break;
                    }
                }
            }
            using(System.IO.StreamWriter sw = new System.IO.StreamWriter(@"C:\log\point2position.txt",true))
            {

                string s = string.Format("X={0}, Y={1}, nLine={2}, pos={3}",point.X,point.Y,nLine,pos);
//                sw.WriteLine(s);
            }
            return new Position(nLine, pos);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Point Position2Point(Position position)
        {            
            float testHeight = 0F;
            for (int i = 0; i < position.Line; i++)
            {
                testHeight += list[i].Height;                
            }
            int y = (int)testHeight;

            MyLine line = list[position.Line];
            int x;
            if (position.Pos == 0)
                x = 0;
            else
                x = (int)line.GetWidth(0, position.Pos - 1);// 前の文字までの長さを取得
            return new Point(x, y);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyTextbox_DragDrop(object sender, DragEventArgs e)
        {
            
        }

        private MyTextboxInfoEventArgs SetInfoEventArgs()
        {
            MyTextboxInfoEventArgs args = null;

            try
            {
                args = new MyTextboxInfoEventArgs();
                args.nLine = CurrentPos.Line;
                args.nPos = CurrentPos.Pos;
                args.CurrentLineLength = list[CurrentPos.Line].Length;
            }
            catch(Exception)
            {

            }

            return args;
        }
        
        /// <summary>
        /// 新たに文字列を設定する。
        /// </summary>
        /// <param name="arr"></param>
        public void Set(string[] arr)
        {
            list = new List<MyLine>();
            for (int i = 0; i < arr.Length; i++)
            {
                this.Insert(i, 0, arr[i]);
            }
        }
        /// <summary>
        /// 現在の位置に文字列を挿入
        /// </summary>
        /// <param name="str"></param>
        public Task Insert(string str)
        {
            return Insert(CurrentPos.Line, CurrentPos.Pos, str);
        }
        /// <summary>
        /// 任意の位置に文字列を挿入
        /// </summary>
        /// <param name="nLine">Line number. 0 origin</param>
        /// <param name="pos"></param>
        /// <param name="str"></param>
        public Task Insert(int nLine, int pos, string str)
        {
            
            //nLineがまだ存在しない行番号だった場合に備えて、その行までの存在しない行を全て作成
            // 例えば、現在0-4行目が存在、8行目に文字を挿入する場合、
            // 5-8行目を作成する。
            for (int i = list.Count; i < nLine + 1; i++)
            {
                MyLine newLine = new MyLine();
                list.Add(newLine);
            }

            MyLine line = list[nLine];
            line.Insert(pos, str);
            CurrentPos = new Position(CurrentPos.Line, CurrentPos.Pos + str.Length);
            Task work = new Task();
            work.type = Task.Type.INSERT;
            work.fromLine = nLine;
            work.fromPos = pos;
            work.length = str.Length;
            work.str = str;
            history.Add(work);
            return work;
        }
        /// <summary>
        /// 文字列を削除
        /// </summary>
        /// <param name="nLine"></param>
        /// <param name="index">開始点</param>
        /// <param name="count">削除する長さ</param>
        public void Delete(int nLine, int index, int count)
        {
            MyLine line = list[nLine];
            line.Delete(index, count);
            Task work = new Task();
            work.type = Task.Type.DELETE;
            work.fromLine = nLine;
            work.fromPos = index;
            work.length = count;
            history.Add(work);

            CurrentPos = new Position(nLine,index);
        }

    }
    /// <summary>
    /// デフォルトの設定を保持するクラス。
    /// </summary>
    public static class MyTextboxDefaultValue
    {
        public static Font Font;
        public static Brush Brush;
        public static MyReturnCode ReturnCode;
        static MyTextboxDefaultValue()
        {
            Font = new Font("メイリオ", 10);
            Brush = Brushes.Black;
            ReturnCode = new MyReturnCode(returnCode.CRLF);
        }
    }
    public class Position
    {
        public int Line
        {
            get;
            set;
        }
        public int Pos
        {
            get;
            set;
        }
        public Position(int Line, int Pos)
        {
            this.Line = Line;
            this.Pos = Pos;
        }
    }
    /// <summary>
    /// 色々と便利なツール群
    /// </summary>
    public static class MyTextboxTools
    {
        
        /// <summary>
        /// 文字列内の全改行コードを指定されたものに変換する
        /// </summary>
        /// <returns></returns>
        public static string ReturnCodeConverter(string str, string returnCode)
        {
            string s = "";
            // 一旦全てを\nにする。こうしないと、\r\nが指定された時に多分ちょっと面倒
            s = str.Replace("\r\n", "\n");
            s = s.Replace("\r", "\n");

            // 指定されたものに。
            if (returnCode != "\n")
            {
                s = s.Replace("\n", returnCode);
            }
            return s;
        }



    }
    /// <summary>
    /// 1文字の情報を保持するクラス
    /// </summary>
    public class MyChar
    {
        
        
        
        private char _c;
        /// <summary>
        /// 保持する文字の取得または設定
        /// </summary>
        public char C
        {
            set
            {
                _c = value;
            }
            get
            {
                return _c;
            }
        }
        private Font _font;
        public Font Font
        {
            set
            {
                _font = value;
            }
            get
            {
                return _font;
            }
        }
        private Brush _brush;
        public Brush Brush
        {
            set
            {
                _brush = value;
            }
            get
            {
                return _brush;
            }
        }
        public float Height
        {
            get
            {
                Bitmap canvas = new Bitmap(10, 10);
                Graphics g = Graphics.FromImage(canvas);
                SizeF sizeF = g.MeasureString(this.C.ToString(), Font);
                return sizeF.Height;
            }
        }
        public float Width
        {
            get
            {
                Bitmap canvas = new Bitmap(10,10);
                Graphics g = Graphics.FromImage(canvas);
                SizeF sizeF = g.MeasureString(this.C.ToString(), Font);
                return sizeF.Width;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        public MyChar(char c)
        {
            this.C = c;
            this.Font = MyTextboxDefaultValue.Font;
            this.Brush = MyTextboxDefaultValue.Brush;
            if (C.ToString() == "き")
            {
//                Font = new Font(Font.Name, 20);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.C + "";
        }
    }
    /// <summary>
    /// 1行の情報を保持するクラス
    /// 
    /// 文字列中への改行コードの混入を許容すべきか否か。現在のところこれといった対策を取っていないため、一応許容する状態となっている。
    /// 改行コードの混入を禁止する場合にはMyTextbox.Textのsetの部分を変更する必要が出る。
    /// </summary>
    public class MyLine
    {
        /// <summary>
        /// 保持する文字のリスト
        /// </summary>
        List<MyChar> list;
        /// <summary>
        /// 改行文字
        /// </summary>
        public MyReturnCode ReturnCode;
        public MyChar this[int index]
        {
            get
            {
                return list[index];
            }
        }
        public MyLine()
        {
            Text = "";
            this.ReturnCode = new MyReturnCode();
        }

        public MyLine(string str, MyReturnCode myReturnCode)
        {
            // TODO: Complete member initialization
            this.Text = str;
            this.ReturnCode = myReturnCode;
        }
        /// <summary>
        /// 行の長さ
        /// </summary>
        public int Length
        {
            get
            {
                return list.Count;
            }
        }

        public float Width
        {
            get
            {
                return this.GetWidth(0, this.Length-1);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="str"></param>
        public void Insert(int pos, string str)
        {
            List<MyChar> myCharList = String2MyCharList(str);
            if (list.Count >= pos)
            {
                list.InsertRange(pos, myCharList);
            }
            else
            {
                list.AddRange(myCharList);
            }
        }

        public void Delete(int index, int count)
        {
            list.RemoveRange(index, count);
        }
        /// <summary>
        /// string型の文字列をList&lt;MyChar&gt;に変換する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public List<MyChar> String2MyCharList(string str)
        {
            List<MyChar> retVal = new List<MyChar>();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                MyChar myChar = new MyChar(c);
                retVal.Add(myChar);
            }
            return retVal;
        }
        /// <summary>
        /// MyLibrary.MyLineのような文字列を返すべき。今までの処理はthis.Textへ。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "MyLibrary.MyLine";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Text
        {
            set
            {
                list = String2MyCharList(value);
            }
            get
            {
                return TextWithoutReturnValue + this.ReturnCode.ToString();
            }            
        }
        /// <summary>
        /// 
        /// </summary>
        public string TextWithoutReturnValue
        {
            get
            {
                string s = "";
                for (int i = 0; i < list.Count; i++)
                {
                    s += list[i].C;
                }
                return s;
            }
        }
        /// <summary>
        /// 指定された範囲の文字幅を取得
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public float GetWidth(int from, int to)
        {
            float width = 0.0F;

            // 
            if (to >= list.Count)
                to = list.Count - 1;

            for (int i = from; i <= to; i++)
            {
                width += list[i].Width;
            }
            return width;
        }
        /// <summary>
        /// 
        /// </summary>
        public float Height
        {
            get
            {
                //一番大きい文字のHeightをこの行のHeightとする
                float max = 0F;
                for (int i = 0; i < this.Length; i++)
                {
                    if (max < list[i].Height)
                        max = list[i].Height;
                }
                return max;
            }
        }
    }
    /// <summary>
    /// 改行文字
    /// </summary>
    public static class ReturnCode_old
    {
        public const string CRLF= "\r\n";
        public const string LF = "\n";
        public const string CR = "\r";
    }
    public enum returnCode
    {
        CRLF,
        LF,
        CR,
        NULL,
    }
    /// <summary>
    /// あたかもこれ自体が文字列のように振る舞うようにしたい
    /// </summary>
    public class MyReturnCode
    {
        private returnCode _returnCode;     
        public MyReturnCode()
        {
            _returnCode = returnCode.NULL;
        }
        public MyReturnCode(returnCode returnCode)
        {
            _returnCode = returnCode;
        }
        public void Set(returnCode returnCode)
        {
            _returnCode = returnCode;
        }
        public returnCode Get()
        {
            return _returnCode;
        }
        public override string ToString()
        {
            string ret = string.Empty;
            switch (_returnCode)
            {
                case returnCode.CR:
                    ret = "\r";
                    break;
                case returnCode.CRLF:
                    ret = "\r\n";
                    break;
                case returnCode.LF:
                    ret = "\n";
                    break;
                default:
                    ret = "";
                    break;
            }
            return ret;
        }
    }
    /// <summary>
    /// キャレット
    /// </summary>
    public class Caret
    {
        IntPtr handle;
        int width;
        int height;
        int x;
        int y;
        bool isShow;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Caret(IntPtr Handle, int width, int height)
        {
            this.handle = Handle;
            this.width = width;
            this.height = height;
            this.isShow = false;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetPos(int x, int y)
        {
            this.x = x;
            this.y = y;
            MyLibrary.NativeMethods.SetCaretPos(this.x, this.y);
        }
        /// <summary>
        /// デバッグ以外ではたぶんいらない
        /// </summary>
        /// <returns></returns>
        public Point GetPos()
        {
            return new Point(x, y);
        }
        /// <summary>
        /// 
        /// </summary>
        public void ShowCaret()
        {
            if (!isShow)
            {
                MyLibrary.NativeMethods.CreateCaret(handle, IntPtr.Zero, this.width, this.height);
                MyLibrary.NativeMethods.ShowCaret(this.handle);
            }
        }

        public void HideCaret()
        {
            if (isShow)
            {                
                MyLibrary.NativeMethods.HideCaret(this.handle);
                MyLibrary.NativeMethods.DestroyCaret();
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class MyMouseEventArgs : EventArgs
    {
        public int X;
        public int Y;
        public int nLine;
        public int nPos;
    }
    /// <summary>
    /// 
    /// </summary>
    public class MyTextboxInfoEventArgs : EventArgs
    {
        public int nLine;
        public int nPos;
        public int CurrentLineLength;
    }

}
