using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MediaPlayer
{
    class VisualManager
    {
        private Canvas mCanvas;
        private List<VisualItem> mItems = new List<VisualItem>();
        private Rect mLayout;
        private bool mLayoutChange;
        private System.Threading.Timer mTimer;
        private int mUpdateInterval;
        private Point? mCursor;

        private List<VisualItem> mActiveItems = new List<VisualItem>();

        Path mPath = new Path();
        PathGeometry mPathGeometry = new PathGeometry();
        PathFigure mPathFigure = new PathFigure();
        LineSegment mLineSegment = new LineSegment();
        ArcSegment mArcSegment = new ArcSegment();
        int mRadius = 50;
        int mAngle = 50;

        private void InitActivationCircle()
        {
            Canvas.SetLeft(mPath, mCanvas.ActualWidth / 2);
            Canvas.SetTop(mPath, mCanvas.ActualHeight / 2);
            mLineSegment.IsStroked = true;
            mLineSegment.Point = new Point(mRadius, 0);
            mArcSegment.SweepDirection = SweepDirection.Clockwise;
            //mArcSegment.IsLargeArc = mAngle >= 180.0;
            //mArcSegment.Point = new Point(Math.Cos(mAngle * Math.PI / 180) * mRadius, Math.Sin(mAngle * Math.PI / 180) * mRadius);
            mArcSegment.Size = new Size(mRadius, mRadius);
            mPathFigure.StartPoint = new Point(0, 0);
            mPathFigure.IsClosed = true;
            mPathFigure.Segments.Add(mLineSegment);
            mPathFigure.Segments.Add(mArcSegment);
            mPathGeometry.Figures.Add(mPathFigure);
            mPath.Fill = Brushes.Red;
            mPath.Stroke = Brushes.Red;
            mPath.Visibility = Visibility.Hidden;
            mPath.Data = mPathGeometry;
            mCanvas.Children.Add(mPath);
        }

        private void DrawActivation()
        {
//            mPathFigure.StartPoint = new Point(0, 0);
//            mPathFigure.IsClosed = true;
            mAngle = (mAngle+10)%360;

//            mLineSegment.Point = new Point(mRadius, 0);
//            mLineSegment.IsStroked = true;

//            mArcSegment.IsLargeArc = mAngle >= 180.0;
//            mArcSegment.Point = new Point(Math.Cos(mAngle * Math.PI / 180) * mRadius, Math.Sin(mAngle * Math.PI / 180) * mRadius);
//            mArcSegment.Size = new Size(mRadius, mRadius);

            mArcSegment.IsLargeArc = mAngle >= 180.0;
//            mArcSegment.Point.X = Math.Cos(mAngle * Math.PI / 180) * mRadius;
 //           mArcSegment.Point.Y = Math.Sin(mAngle * Math.PI / 180) * mRadius;
            mArcSegment.Point = new Point(Math.Cos(mAngle * Math.PI / 180) * mRadius, Math.Sin(mAngle * Math.PI / 180) * mRadius);
            
        }

        private void DoEnter(object sender, EventArgs e)
        {
        }

        private void DoLeave(object sender, EventArgs e)
        {
        }

        private void DoPreActive(object sender, EventArgs e)
        {
            mActiveItems.Add(sender as VisualItem);
        }

        private void DoActivate(object sender, EventArgs e)
        {
            mActiveItems.Remove(sender as VisualItem);
        }

        private void ResetItems()
        {
            foreach (VisualItem item in mItems)
            {
                item.Reset();
            }
        }

        private void LayoutItems()
        {
            foreach (VisualItem item in mItems)
            {
                item.Layout(mLayout);
            }
            mLayoutChange = false;
        }

        private void TimerCallback(object state)
        {
            if (mLayoutChange)
            {
                LayoutItems();
            }

            lock (mItems)
            {
                long msec = DateTime.Now.Ticks / 10000;
                foreach (VisualItem item in mItems)
                {
                    item.Update(mCursor, msec);
                }
                //mCursor = null;
            }
        }

        public VisualManager(Canvas canvas)
        {
            mCanvas = canvas;
            mLayout = new Rect(new Point(0, 0), new Size(800, 600));
            mLayoutChange = false;
            UpdateInterval = 100;
            mTimer = new System.Threading.Timer(TimerCallback);
            InitActivationCircle();
        }

        public void SetLocation(double x, double y)
        {
            mLayout.X = x;
            mLayout.Y = y;
            mLayoutChange = true;
        }

        public void SetSize(double w, double h)
        {
            mLayout.Width = w;
            mLayout.Height = h;
            mLayoutChange = true;
        }

        public void SetSize(Size size)
        {
            SetSize(size.Width, size.Height);
        }

        public void Add(VisualItem item)
        {
            item.Layout(mLayout);
            item.Reset();
            item.OnEnter += DoEnter;
            item.OnLeave += DoLeave;
            item.OnPreActivate += DoPreActivate;
            item.OnActivate += DoActivate;
            lock (mItems)
            {
                mItems.Add(item);
            }
        }

        public int UpdateInterval
        {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                mUpdateInterval = value;
            }

            get
            {
                return mUpdateInterval;
            }
        }

        public void Start()
        {
            mActivation = 0;
            ResetItems();
            mTimer.Change(0, mUpdateInterval);
        }

        public void Stop()
        {
            mTimer.Change(-1, mUpdateInterval);
        }

        public void UpdateCursor(Point cursor)
        {
            mCursor = cursor;
        }
    }
}
