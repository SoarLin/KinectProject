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
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.WpfViewers;

namespace SkeletonTest
{
    public enum ImageType
    {
        Color,
        Depth,
    }

    internal enum TrackingMode
    {
        DefaultSystemTracking,
        Closest1Player,
        Closest2Player,
        Sticky1Player,
        Sticky2Player,
        MostActive1Player,
        MostActive2Player
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const float ActivityFalloff = 0.98f;
        private readonly List<ActivityWatcher> recentActivity = new List<ActivityWatcher>();
        private readonly List<int> activeList = new List<int>();
        private List<KinectSkeleton> skeletonCanvases;
        private List<Dictionary<JointType, JointMapping>> jointMappings = new List<Dictionary<JointType, JointMapping>>();
        private Skeleton[] skeletonData;
        public SkeletonFrame skeletonFrame;
        KinectSensor kinectSensor;

        private byte[] colorData;
        public ImageFrame imageFrame;
        private WriteableBitmap outputImage;
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        public MainWindow()
        {
            InitializeComponent();
            this.ShowJoints = true;
            this.ShowBones = true;
            this.ShowCenter = true;

            kinectSensor = KinectSensor.KinectSensors[0];
            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.SkeletonStream.Enable();
            kinectSensor.Start();
            //kinectSensor.ColorFrameReady += ColorImageReady;
            kinectSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(KinectAllFramesReady);

        }

        private void WindowClosed(object sender, EventArgs e)
        {
            kinectSensor.Stop();
        }

        public bool ShowBones { get; set; }
        public bool ShowJoints { get; set; }
        public bool ShowCenter { get; set; }
        public ImageType ImageType { get; set; }
        internal TrackingMode TrackingMode { get; set; }

        public void HideAllSkeletons()
        {
            if (this.skeletonCanvases != null)
            {
                foreach (KinectSkeleton skeletonCanvas in this.skeletonCanvases)
                {
                    skeletonCanvas.Reset();
                }
            }
        }
        
        private void ColorImageReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    this.colorData = new byte[imageFrame.PixelDataLength];
                    imageFrame.CopyPixelDataTo(this.colorData);

                    this.outputImage = new WriteableBitmap(imageFrame.Width, imageFrame.Height,
                                       96, 96, PixelFormats.Bgr32, null);
                    this.kinectColorImage.Source = this.outputImage;

                    this.outputImage.WritePixels(new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                                    this.colorData, imageFrame.Width * Bgr32BytesPerPixel, 0);
                }
                else
                { // imageFrame is null because the request did not arrive in time 
                }
            } //  using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
        }//private void ColorImageReady
        
        private void KinectAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Have we already been "shut down" by the user of this viewer, 
            // or has the SkeletonStream been disabled since this event was posted?
            //if ((this.Kinect == null) || !((KinectSensor)sender).SkeletonStream.IsEnabled)
            if ( (kinectSensor==null) || !kinectSensor.IsRunning || !((KinectSensor)sender).SkeletonStream.IsEnabled )
            {
                return;
            }

            bool haveSkeletonData = false;

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if (this.skeletonCanvases == null)
                    {
                        this.CreateListOfSkeletonCanvases();
                    }

                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        //this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        this.skeletonData = new Skeleton[kinectSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    haveSkeletonData = true;
                }
            }

            if (haveSkeletonData)
            {
                using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
                {
                    if (depthImageFrame != null)
                    {
                        int trackedSkeletons = 0;

                        foreach (Skeleton skeleton in this.skeletonData)
                        {
                            Dictionary<JointType, JointMapping> jointMapping = this.jointMappings[trackedSkeletons];
                            jointMapping.Clear();

                            KinectSkeleton skeletonCanvas = this.skeletonCanvases[trackedSkeletons++];
                            skeletonCanvas.ShowBones = this.ShowBones;
                            skeletonCanvas.ShowJoints = this.ShowJoints;
                            skeletonCanvas.ShowCenter = this.ShowCenter;

                            // Transform the data into the correct space
                            // For each joint, we determine the exact X/Y coordinates for the target view
                            foreach (Joint joint in skeleton.Joints)
                            {
                                Point mappedPoint = this.GetPosition2DLocation(depthImageFrame, joint.Position);
                                jointMapping[joint.JointType] = new JointMapping
                                {
                                    Joint = joint,
                                    MappedPoint = mappedPoint
                                };
                            }

                            // Look up the center point
                            Point centerPoint = this.GetPosition2DLocation(depthImageFrame, skeleton.Position);

                            // Scale the skeleton thickness
                            // 1.0 is the desired size at 640 width
                            double scale = this.RenderSize.Width / 640;

                            skeletonCanvas.RefreshSkeleton(skeleton, jointMapping, centerPoint, scale);
                        }

                        if (ImageType == ImageType.Depth)
                        {
                            this.ChooseTrackedSkeletons(this.skeletonData);
                        }
                    }
                }

                using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
                {
                    if (imageFrame != null)
                    {
                        this.colorData = new byte[imageFrame.PixelDataLength];
                        imageFrame.CopyPixelDataTo(this.colorData);
                        
                        this.outputImage = new WriteableBitmap(imageFrame.Width, imageFrame.Height,
                                           96, 96, PixelFormats.Bgr32, null);
                        this.kinectColorImage.Source = this.outputImage;
                        
                        this.outputImage.WritePixels(new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                                this.colorData, imageFrame.Width * Bgr32BytesPerPixel, 0);
                        /**/
                    }
                    else
                    { // imageFrame is null because the request did not arrive in time 
                    }
                }
            }
        }

        private Point GetPosition2DLocation(DepthImageFrame depthFrame, SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = depthFrame.MapFromSkeletonPoint(skeletonPoint);

            switch (ImageType)
            {
                case ImageType.Color:
                    ColorImagePoint colorPoint = depthFrame.MapToColorImagePoint(depthPoint.X, depthPoint.Y, kinectSensor.ColorStream.Format);

                    // map back to skeleton.Width & skeleton.Height
                    return new Point(
                        (int)(this.RenderSize.Width * colorPoint.X / kinectSensor.ColorStream.FrameWidth),
                        (int)(this.RenderSize.Height * colorPoint.Y / kinectSensor.ColorStream.FrameHeight));
                case ImageType.Depth:
                    return new Point(
                        (int)(this.RenderSize.Width * depthPoint.X / depthFrame.Width),
                        (int)(this.RenderSize.Height * depthPoint.Y / depthFrame.Height));
                default:
                    throw new ArgumentOutOfRangeException("ImageType was a not expected value: " + ImageType.ToString());
            }
        }

        private void CreateListOfSkeletonCanvases()
        {
            this.skeletonCanvases = new List<KinectSkeleton>
                {
                    this.skeletonCanvas1,
                    this.skeletonCanvas2,
                    this.skeletonCanvas3,
                    this.skeletonCanvas4,
                    this.skeletonCanvas5,
                    this.skeletonCanvas6
                };

            this.skeletonCanvases.ForEach(s => this.jointMappings.Add(new Dictionary<JointType, JointMapping>()));
        }

        // NOTE: The ChooseTrackedSkeletons part of the KinectSkeletonViewer would be useful
        // separate from the SkeletonViewer.
        private void ChooseTrackedSkeletons(IEnumerable<Skeleton> skeletonDataValue)
        {
            switch (TrackingMode)
            {
                case TrackingMode.Closest1Player:
                    this.ChooseClosestSkeletons(skeletonDataValue, 1);
                    break;
                case TrackingMode.Closest2Player:
                    this.ChooseClosestSkeletons(skeletonDataValue, 2);
                    break;
                case TrackingMode.Sticky1Player:
                    this.ChooseOldestSkeletons(skeletonDataValue, 1);
                    break;
                case TrackingMode.Sticky2Player:
                    this.ChooseOldestSkeletons(skeletonDataValue, 2);
                    break;
                case TrackingMode.MostActive1Player:
                    this.ChooseMostActiveSkeletons(skeletonDataValue, 1);
                    break;
                case TrackingMode.MostActive2Player:
                    this.ChooseMostActiveSkeletons(skeletonDataValue, 2);
                    break;
            }
        }

        private void ChooseClosestSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            SortedList<float, int> depthSorted = new SortedList<float, int>();

            foreach (Skeleton s in skeletonDataValue)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    float valueZ = s.Position.Z;
                    while (depthSorted.ContainsKey(valueZ))
                    {
                        valueZ += 0.0001f;
                    }

                    depthSorted.Add(valueZ, s.TrackingId);
                }
            }

            this.ChooseSkeletonsFromList(depthSorted.Values, count);
        }

        private void ChooseOldestSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            List<int> newList = new List<int>();

            foreach (Skeleton s in skeletonDataValue)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    newList.Add(s.TrackingId);
                }
            }

            // Remove all elements from the active list that are not currently present
            this.activeList.RemoveAll(k => !newList.Contains(k));

            // Add all elements that aren't already in the activeList
            this.activeList.AddRange(newList.FindAll(k => !this.activeList.Contains(k)));

            this.ChooseSkeletonsFromList(this.activeList, count);
        }

        private void ChooseMostActiveSkeletons(IEnumerable<Skeleton> skeletonDataValue, int count)
        {
            foreach (ActivityWatcher watcher in this.recentActivity)
            {
                watcher.NewPass();
            }

            foreach (Skeleton s in skeletonDataValue)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    ActivityWatcher watcher = this.recentActivity.Find(w => w.TrackingId == s.TrackingId);
                    if (watcher != null)
                    {
                        watcher.Update(s);
                    }
                    else
                    {
                        this.recentActivity.Add(new ActivityWatcher(s));
                    }
                }
            }

            // Remove any skeletons that are gone
            this.recentActivity.RemoveAll(aw => !aw.Updated);

            this.recentActivity.Sort();
            this.ChooseSkeletonsFromList(this.recentActivity.ConvertAll(f => f.TrackingId), count);
        }

        private void ChooseSkeletonsFromList(IList<int> list, int max)
        {
            if (kinectSensor.SkeletonStream.IsEnabled)
            {
                int argCount = Math.Min(list.Count, max);

                if (argCount == 0)
                {
                    kinectSensor.SkeletonStream.ChooseSkeletons();
                }

                if (argCount == 1)
                {
                    kinectSensor.SkeletonStream.ChooseSkeletons(list[0]);
                }

                if (argCount >= 2)
                {
                    kinectSensor.SkeletonStream.ChooseSkeletons(list[0], list[1]);
                }
            }
        }

        private class ActivityWatcher : IComparable<ActivityWatcher>
        {
            private float activityLevel;
            private SkeletonPoint previousPosition;
            private SkeletonPoint previousDelta;

            internal ActivityWatcher(Skeleton s)
            {
                this.activityLevel = 0.0f;
                this.TrackingId = s.TrackingId;
                this.Updated = true;
                this.previousPosition = s.Position;
                this.previousDelta = new SkeletonPoint();
            }

            internal int TrackingId { get; private set; }

            internal bool Updated { get; private set; }

            public int CompareTo(ActivityWatcher other)
            {
                // Use the existing CompareTo on float, but reverse the arguments,
                // since we wish to have larger activityLevels sort ahead of smaller values.
                return other.activityLevel.CompareTo(this.activityLevel);
            }

            internal void NewPass()
            {
                this.Updated = false;
            }

            internal void Update(Skeleton s)
            {
                SkeletonPoint newPosition = s.Position;
                SkeletonPoint newDelta = new SkeletonPoint
                {
                    X = newPosition.X - this.previousPosition.X,
                    Y = newPosition.Y - this.previousPosition.Y,
                    Z = newPosition.Z - this.previousPosition.Z
                };

                SkeletonPoint deltaV = new SkeletonPoint
                {
                    X = newDelta.X - this.previousDelta.X,
                    Y = newDelta.Y - this.previousDelta.Y,
                    Z = newDelta.Z - this.previousDelta.Z
                };

                this.previousPosition = newPosition;
                this.previousDelta = newDelta;

                float deltaVLengthSquared = (deltaV.X * deltaV.X) + (deltaV.Y * deltaV.Y) + (deltaV.Z * deltaV.Z);
                float deltaVLength = (float)Math.Sqrt(deltaVLengthSquared);

                this.activityLevel = this.activityLevel * ActivityFalloff;
                this.activityLevel += deltaVLength;

                this.Updated = true;
            }
        }
    }
}
