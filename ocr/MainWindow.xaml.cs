using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace DemoOCR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] langarr = { "Vietnamese", "English", "Korean", "French", "Japanese", "Russian", "Spanish"};
        string[] langarrmini = { "vie", "eng", "kor", "fra", "jpn", "rus", "spa" };
        string langnow = "eng";
        Thread thread_onload;
        Thread thread_ocr;
        Thread thread_xulyocr;
        string textscaned;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _close(object sender, RoutedEventArgs e)
        {

            System.Environment.Exit(1);
        }

        private void _minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void OnLoad()
        {
            while (true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    int indexarr = Array.IndexOf(langarr, _ocrlang.Text);
                    langnow = langarrmini[indexarr];
                });
                Thread.Sleep(100);
            }
        }
        public partial class SnippingTool : Form
        {
            public static System.Drawing.Image Snip()
            {
                var rc = Screen.PrimaryScreen.Bounds;
                using (Bitmap bmp = new Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (Graphics gr = Graphics.FromImage(bmp))
                        gr.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    using (var snipper = new SnippingTool(bmp))
                    {
                        if (snipper.ShowDialog() == DialogResult.OK)
                        {
                            return snipper.Image;
                        }
                    }
                    return null;
                }
            }

            public SnippingTool(System.Drawing.Image screenShot)
            {
                this.BackgroundImage = screenShot;
                this.ShowInTaskbar = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.DoubleBuffered = true;
            }
            public System.Drawing.Image Image { get; set; }

            private System.Drawing.Rectangle rcSelect = new System.Drawing.Rectangle();
            private System.Drawing.Point pntStart;

            protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
            {
                // Start the snip on mouse down
                if (e.Button != MouseButtons.Left) return;
                pntStart = e.Location;
                rcSelect = new System.Drawing.Rectangle(e.Location, new System.Drawing.Size(0, 0));
                this.Invalidate();
            }
            protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
            {
                // Modify the selection on mouse move
                if (e.Button != MouseButtons.Left) return;

                int x1 = Math.Min(e.X, pntStart.X);
                int y1 = Math.Min(e.Y, pntStart.Y);
                int x2 = Math.Max(e.X, pntStart.X);
                int y2 = Math.Max(e.Y, pntStart.Y);
                rcSelect = new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1);
                this.Invalidate();
            }
            protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
            {
                // Complete the snip on mouse-up
                if (rcSelect.Width <= 0 || rcSelect.Height <= 0) return;
                Image = new Bitmap(rcSelect.Width, rcSelect.Height);
                using (Graphics gr = Graphics.FromImage(Image))
                {
                    gr.DrawImage(this.BackgroundImage, new System.Drawing.Rectangle(0, 0, Image.Width, Image.Height),
                        rcSelect, GraphicsUnit.Pixel);
                }
                DialogResult = DialogResult.OK;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                // Draw the current selection
                using (System.Drawing.Brush br = new SolidBrush(System.Drawing.Color.FromArgb(30, System.Drawing.Color.White)))
                {
                    int x1 = rcSelect.X; int x2 = rcSelect.X + rcSelect.Width;
                    int y1 = rcSelect.Y; int y2 = rcSelect.Y + rcSelect.Height;
                    e.Graphics.FillRectangle(br, new System.Drawing.Rectangle(0, 0, x1, this.Height));
                    e.Graphics.FillRectangle(br, new System.Drawing.Rectangle(x2, 0, this.Width - x2, this.Height));
                    e.Graphics.FillRectangle(br, new System.Drawing.Rectangle(x1, 0, x2 - x1, y1));
                    e.Graphics.FillRectangle(br, new System.Drawing.Rectangle(x1, y2, x2 - x1, this.Height - y2));
                }
                using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Red, 3))
                {
                    e.Graphics.DrawRectangle(pen, rcSelect);
                }
            }
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
 
                if (keyData == Keys.Escape) this.DialogResult = DialogResult.Cancel;
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        private void threadxulyocr()
        {
            {
                this.Dispatcher.Invoke(() =>
                {
                    _process.Visibility = System.Windows.Visibility.Visible;
                    _trangthai.Content = "Đang chạy...";
                });

                
                thread_ocr = new Thread(() => OCR(Directory.GetCurrentDirectory() + "\\scan.jpg", langnow));
                thread_ocr.IsBackground = true;
                thread_ocr.Start();
                while (thread_ocr.IsAlive)
                {
                    Thread.Sleep(50);
                }
                this.Dispatcher.Invoke(() =>
                {
                    _process.Visibility = System.Windows.Visibility.Hidden;
                    _trangthai.Content = "Hoàn thành!";
                    _text.Text = textscaned;
                    if (_googlecheck.IsChecked == true)
                    {
                        System.Diagnostics.Process.Start("https://www.google.com/search?q=" + textscaned);
                    }
                    
                    
                    System.Windows.Clipboard.SetText(textscaned);
                });
            }
            
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var bmp = SnippingTool.Snip();
            if (bmp != null)
            {
                if (System.IO.File.Exists(Directory.GetCurrentDirectory() + "\\scan.jpg"))
                    System.IO.File.Delete(Directory.GetCurrentDirectory() + "\\scan.jpg");
                bmp.Save(Directory.GetCurrentDirectory() + "\\scan.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                this.Show();
                thread_xulyocr = new Thread(() => threadxulyocr());
                thread_xulyocr.IsBackground = true;
                thread_xulyocr.Start();
            }
        }
        private void OCR(string path, string lang = "vie")
        {
            if (lang=="vie")
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "Capture2Text\\Capture2Text_CLI.exe",
                        Arguments = @"-i """+ Directory.GetCurrentDirectory() + @"\\scan.jpg""" + " -l " + @"""" + "Vietnamese" + @"""" + @" -o """+ Directory.GetCurrentDirectory() + @"\\data\\kq.txt""",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
                string kqorc = File.ReadAllText("data\\kq.txt", Encoding.UTF8);
                textscaned = kqorc;
            }
            this.Dispatcher.Invoke(() =>
            {
                if (_latexcheck.IsChecked == true)
                {
                    string filename = @"cli.py";
                    Process p = new Process();
                    p.StartInfo = new ProcessStartInfo(@"python.exe", filename){
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    p.Start();
                    p.WaitForExit();
                    string kqorc = File.ReadAllText("data\\kq.txt", Encoding.UTF8);
                    textscaned = kqorc;
                }
            });
            //{
            //    var process = Process.Start("\"E:\\LaTeX-OCR-main\\pix2tex\\cli.py\"");
            //    process.WaitForExit();
            //}

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
                _process.Visibility = System.Windows.Visibility.Hidden;
                this.Activate();
                thread_onload = new Thread(() => OnLoad());
                thread_onload.IsBackground = true;
                thread_onload.Start();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void _googlecheck_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _latexcheck_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
