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
                (item, callback_perc, callback_log) => {
                    callback_log("starting...");
                    Thread.Sleep(rnd.Next(1000, 2000));
                    callback_perc(50);
                    callback_log("in the middle!");
                    Thread.Sleep(rnd.Next(1500, 2000));
                    callback_perc(100);
                    callback_log("finished.");
                },
                item_progress: (x, n, t, p) => {
                    textBox1.Text += string.Format("Item progress: {0} ({1}/{2}): {3:P2}", x.ToString(), n, t, p / 100) + Environment.NewLine;
                    Refresh();
                    Application.DoEvents();
                },
                item_log: (x, n, t, l) => {
                    textBox1.Text += string.Format("Item log: {0} ({1}/{2}): {3}", x.ToString(), n, t, l) + Environment.NewLine;
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
