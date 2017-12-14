using System;
using System.Threading;
using System.Windows.Forms;

namespace ForAllP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();
            textBox1.Clear();

            string[] arr = new string[] { "012", "123", "234", "345", "456", "567", "678", "789", "890", "901", "abc", "bcd", "cde", "def" };
            arr.ForAllP(
                body: (x, callback) => {
                    Thread.Sleep(rnd.Next(1000, 2000));
                    callback(50);
                    Thread.Sleep(rnd.Next(1500, 2000));
                    callback(100);
                },
                item_progress: (x, n, t, p) => {
                    textBox1.Text += string.Format("Item progress: {0} ({1}/{2}): {3:P2}", x.ToString(), n, t, p / 100) + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                },
                item_started: (x, n, t) => {
                    textBox1.Text += string.Format("Item started: {0} ({1}/{2})", x.ToString(), n, t) + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                },
                item_finished: (x, n, t) => {
                    textBox1.Text += string.Format("Item finished: {0} ({1}/{2})", x.ToString(), n, t) + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                },
                total_started: () => {
                    textBox1.Text += "Start." + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                },
                total_finished: () => {
                    Application.DoEvents();
                    textBox1.Text += "End." + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                }
            );
        }
    }

}
