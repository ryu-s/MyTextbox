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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.myTextbox1.MyMouseMove += new MyTextbox.MyMouseEventHandler(myTextbox1_MyMouseMove);
            this.myTextbox1.MyMouseDown += new MyTextbox.MyMouseEventHandler(myTextbox1_MyMouseDown);
            this.myTextbox1.MyTextboxInfoEvent += new MyTextbox.MyTextboxInfoEventHandler(myTextbox1_MyTextboxInfoEvent);
            this.myTextbox1.MyTaskEvent += myTextbox1_MyTaskEvent;
        }

        void myTextbox1_MyTaskEvent(object sender, TaskEventArgs e)
        {
            myDefaultTextbox1.Text = e.task.ToString() + Environment.NewLine + myDefaultTextbox1.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.myTextbox1.Text = "テスト\nえ\r\nこれ\rおお";
            this.textBox1.Text = this.myTextbox1.Text;
            this.myTextbox1.Focus();
            this.myTextbox1.Select();

            return;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.myTextbox1.Text = this.textBox1.Text;
        }
        private void myTextbox1_MyTextboxInfoEvent(object sender, MyTextboxInfoEventArgs e)
        {
            string s = string.Format("nLine={0}\nPos={1}\nCurrentLineLength={1}", e.nLine, e.nPos,e.CurrentLineLength);
            label3.Text = s;
            Invalidate();
        }
        private void myTextbox1_MyMouseMove(object sender, MyMouseEventArgs e)
        {
            string s = string.Format("X={0}\nY={1}\nLine={2}\nPos={3}", e.X, e.Y, e.nLine, e.nPos);
            label1.Text = s;
            Invalidate();
        }
        private void myTextbox1_MyMouseDown(object sender, MyMouseEventArgs e)
        {
            string s = string.Format("X={0}\nY={1}\nLine={2}\nPos={3}", e.X, e.Y, e.nLine, e.nPos);
            label2.Text = s;
            Invalidate();
        }
    }
}
