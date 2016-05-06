using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FaceTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            grid.MouseMove += Grid_MouseMove;
            grid.MouseDown += Grid_MouseDown; ;
            grid.MouseUp += Grid_MouseUp;
        }

        private string[] img = { ".jpg", ".bmp", ".png" };
        private List<Face> list;
        private int index = 0;
        private EllipsePoint ep;

        private async void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = fbd.SelectedPath;
                await loadFiles();
                setImage();
            }
        }

        private async void txtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.End)
            {
                await loadFiles();
                setImage();
            }
        }

        private Task loadFiles()
        {
            var di = new DirectoryInfo(txtPath.Text);
            if (di.Exists)
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var groups = di.GetFiles().GroupBy(f => f.Extension);
                        if (groups.Count() >= 2)
                        {
                            var pg = groups.First(g => g.Key.ToLower() == ".5pt");
                            var ig = groups.First(g => img.Contains(g.Key.ToLower()));
                            list = pg.Zip(ig, (p, i) => new Face()
                            {
                                ImageFile = i.FullName,
                                PointFile = p.FullName
                            }).ToList();
                        }
                    }
                    catch { }
                });
            }
            return null;
        }

        private void setImage()
        {
            if (list?.Count > 0)
            {
                if (index < 0)
                    index = list.Count - 1;
                else if (index > list.Count - 1)
                    index = 0;

                txtIndex.Text = index.ToString();
                image.Source = list[index].Image;
                canvas.Children.Clear();
                foreach (var item in list[index].EPoints)
                    canvas.Children.Add(item.Ellipse);
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            index--;
            setImage();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            index++;
            setImage();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Ellipse)
            {
                ep = list[index].EPoints.First(p => p.Ellipse == e.OriginalSource);
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ep != null)
            {
                list[index].Save();
                ep = null;
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (ep != null)
            {
                var p = e.GetPosition(canvas);
                ep.X = (int)p.X;
                ep.Y = (int)p.Y;
            }
        }
    }

    public class Face
    {
        public string ImageFile { get; set; }
        public string PointFile { get; set; }

        private List<EllipsePoint> _EPoints;
        public List<EllipsePoint> EPoints
        {
            get
            {
                if (_EPoints == null)
                {
                    _EPoints = File.ReadAllLines(PointFile)
                         .Select(l =>
                         {
                             var xy = l.Split('\t');
                             var rp = new EllipsePoint(double.Parse(xy[0]), double.Parse(xy[1]));
                             return rp;
                         }).ToList();
                }
                return _EPoints;
            }
        }

        private ImageSource _Image;

        public ImageSource Image
        {
            get
            {
                if (_Image == null)
                    _Image = new BitmapImage(new Uri(ImageFile));
                return _Image;
            }
        }

        public void Save()
        {
            File.WriteAllText(PointFile,
                string.Join(Environment.NewLine, EPoints.Select(p => $"{p.X}\t{p.Y}")));
        }
    }

    public class EllipsePoint
    {
        private const int r = 5;
        public Ellipse Ellipse { get; set; }
        public int X
        {
            get { return (int)Canvas.GetLeft(Ellipse) + r; }
            set { Canvas.SetLeft(Ellipse, value - r); }
        }
        public int Y
        {
            get { return (int)Canvas.GetTop(Ellipse) + r; }
            set { Canvas.SetTop(Ellipse, value - r); }
        }

        public EllipsePoint(double x, double y)
        {
            Ellipse = new Ellipse()
            {
                Fill = Brushes.Blue,
                Width = r * 2,
                Height = r * 2
            };
            X = (int)x;
            Y = (int)y;
        }
    }
}
