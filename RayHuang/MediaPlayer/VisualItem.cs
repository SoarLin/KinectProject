using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MediaPlayer
{
    class VisualItem
    {
        private UIElement mUI;
        private Point mLocation;
        private Size mSize;
        private Rect mBound;
        private bool mIsHit;
        private bool mIsActive;
        private bool mIsVisible;
        private long mBirth;
        private long mAge;

        private double Layout(double pos, double original, double length)
        {
            double result = (pos >= 0) ? original + pos : original + length + pos;
            return Math.Max(Math.Min(result, original + length), original);
        }

        private double Layout(double pos, double offset, double original, double length)
        {
            if (offset == 0)
            {
                return original + length;
            }
            else if (offset < 0)
            {
                return Layout(offset, original, length);
            }
            else if (pos >= 0)
            {
                return Layout(pos + offset, original, length);
            }
            else
            {
                double result = Layout(pos, original, length);
                return Math.Min(result + offset, original + length);
            }
        }

        private void ShowUI()
        {
            mUI.Visibility = Visibility.Visible;
        }

        private void HideUI()
        {
            mUI.Visibility = Visibility.Hidden;
        }

        private void SetVisibility(bool visible)
        {
            if (visible)
            {
                mIsVisible = true;
                mUI.Dispatcher.Invoke(new Action(ShowUI));
            }
            else
            {
                mIsVisible = false;
                if (AutoHide)
                {
                    mUI.Dispatcher.Invoke(new Action(HideUI));
                }
            }
        }

        private void SendEvent(EventHandler del)
        {
            if( del != null )
            {
                del(this, new EventArgs());
            }
        }

        private void CheckActive()
        {
            if (!mIsActive && ActivateTime >= 0)
            {
                if ((mAge - mBirth) >= ActivateTime)
                {
                    mIsActive = true;
                    SendEvent(OnActivate);
                }
                else
                {
                    SendEvent(OnPreActivate);
                }
            }
        }

        private void UpdateHitStatus(bool hit, long time)
        {
            if (mIsHit && hit)
            {
                mAge = time;
            }
            if (!mIsHit && hit)
            {
                mAge = time;
                mBirth = time;
                SendEvent(OnEnter);
            }
            else if (mIsHit && !hit)
            {
                mAge = time;
                mIsActive = false;
                SendEvent(OnLeave);
            }

            if (hit)
            {
                CheckActive();
            }

            mIsHit = hit;
        }

        private void UpdateVisibility(long time)
        {
            if (mIsHit && !mIsVisible)
            {
                SetVisibility(true);
            }
            else if (mIsVisible && !mIsHit && (time - mAge) >= VisibleTime)
            {
                SetVisibility(false);
            }
        }

        public VisualItem(UIElement ui, Point pt, Size size)
        {
            mUI = ui;
            mLocation = pt;
            mSize = size;
            mBound = new Rect(pt, size);
            VisibleTime = 5000;
            ActivateTime = 5000;
            Reset(false);
        }

        public VisualItem(UIElement ui, Point pt, double w, double h)
            : this(ui, pt, new Size(w, h))
        {
        }

        public VisualItem(UIElement ui, double x, double y, double w, double h)
            : this(ui, new Point(x, y), new Size(w, h))
        {
        }

        public void Layout(Rect layout)
        {
            double x1 = Layout(mLocation.X, layout.X, layout.Width);
            double x2 = Layout(mLocation.X, mSize.Width, layout.X, layout.Width);
            double y1 = Layout(mLocation.Y, layout.Y, layout.Height);
            double y2 = Layout(mLocation.Y, mSize.Height, layout.Y, layout.Height);

            mBound.X = Math.Min(x1, x2);
            mBound.Width = Math.Abs(x1 - x2);
            mBound.Y = Math.Min(y1, y2);
            mBound.Height = Math.Abs(y1 - y2);

            if (OnLayout != null)
            {
                OnLayout(this, new EventArgs());
            }
        }

        public void Reset(bool hide = true)
        {
            if (hide)
            {
                SetVisibility(false);
            }
            mBirth = -1;
            mAge = -1;
            mIsHit = false;
            mIsActive = false;
        }

        public void Update(Point? cursor, long time)
        {
            bool hit = cursor.HasValue && mBound.Contains(cursor.Value);
            UpdateHitStatus(hit, time);
            UpdateVisibility(time);
        }

        public Rect GetBound()
        {
            return mBound;
        }

        public UIElement GetUI()
        {
            return mUI;
        }

        public int VisibleTime { set; get; }
        public int ActivateTime { set; get; }
        public bool AutoHide { set; get; }

        public event EventHandler OnEnter;
        public event EventHandler OnLeave;
        public event EventHandler OnPreActivate;
        public event EventHandler OnActivate;
        public event EventHandler OnLayout;
    }
}
