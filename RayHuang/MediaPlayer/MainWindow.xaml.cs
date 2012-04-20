using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VisualManager mVisualManager;

        public void OnItemLayout(object sender, EventArgs e)
        {
            textBlockTarget.Dispatcher.Invoke(new Action(() =>
            {
                VisualItem item = sender as VisualItem;
                Rect bound = item.GetBound();
                textBlockTarget.Text = string.Format("{0} {1} {2} {3}", bound.X, bound.Y, bound.Width, bound.Height);
            }));
        }

        public MainWindow()
        {
            InitializeComponent();

            mVisualManager = new VisualManager(canvasOverlay);

            mVisualManager.Add(new VisualItem(canvasExit, -100, 0, 100, 100));
            
            mVisualManager.Add(new VisualItem(canvasSource,0, 0, 150, 80));
            mVisualManager.Add(new VisualItem(canvasPlay, 200, 100, 120, 80));
            mVisualManager.Add(new VisualItem(canvasPause, 100, 100, 120, 80));
            mVisualManager.Add(new VisualItem(canvasVolume, -50, 150, 50, 0));
            mVisualManager.Add(new VisualItem(canvasSeek, 50, -55, 0, 0));
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            mVisualManager.Start();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            mVisualManager.Stop();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            mVisualManager.SetSize(canvasMain.RenderSize);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            textBlockWindow.Text = string.Format("{0} {1}", Left, Top);
            mVisualManager.SetLocation(0, 0);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point cursor = e.GetPosition(canvasMain);
            mVisualManager.UpdateCursor(cursor);
            textBlockCursor.Text = string.Format("{0} {1}", cursor.X, cursor.Y);
        }
    }
}
