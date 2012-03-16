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
//using System.Windows.Forms;
using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using System.Threading;
using System.IO;
using Microsoft.Speech.AudioFormat;
using System.Diagnostics;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace KinectPowerPointControl
{
    public enum ImageType
    {
        Color,
        Depth,
    }

    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        SpeechRecognitionEngine speechRecognizer;

        DispatcherTimer readyTimer;

        byte[] colorBytes;
        Skeleton[] skeletons;

        private KinectSkeleton skeletonCanvas;// = new KinectSkeleton();
        private Dictionary<JointType, JointMapping> jointMapping = new Dictionary<JointType, JointMapping>();
        
        bool isCirclesVisible = true;

        bool isForwardGestureActive = false;
        bool isBackGestureActive = false;
        SolidColorBrush activeBrush = new SolidColorBrush(Colors.Green);
        SolidColorBrush inactiveBrush = new SolidColorBrush(Colors.Red);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr Parent, IntPtr ChildAfter, string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        static extern IntPtr PostMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);
     
        const int WM_COMMAND = 0x111;
        const int GW_HWNDNEXT = 2;
        const int MIN_ALL = 419;
        const int MIN_ALL_UNDO = 416;
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        const int WM_CHAR = 0x0102;

        public MainWindow()
        {
            InitializeComponent();
            this.ShowJoints = true;
            this.ShowBones = true;
            this.ShowCenter = true;
            //Runtime initialization is handled when the window is opened. When the window
            //is closed, the runtime MUST be unitialized.
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            //Handle the content obtained from the video camera, once received.

            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            #region Find PowerPoint & Send F5
            try
            {
                IntPtr ppt = FindWindow("PP11FrameClass", null);
                IntPtr ppt_slide = FindWindowRecursive(ppt, "paneClassDC", null);
                //IntPtr ppt_screen = FindWindow("screenClass", null);
                //SetForegroundWindow(ppt_slide);
                //System.Threading.Thread.Sleep(500);
//                PostMessage(ppt_slide, WM_KEYDOWN, (IntPtr)0x74, IntPtr.Zero);
                //System.Threading.Thread.Sleep(1000);
                //SendMessage(ppt_slide, WM_CHAR, (IntPtr)0x74, IntPtr.Zero);
                //System.Threading.Thread.Sleep(500);
                //PostMessage(ppt_slide, WM_KEYUP, (IntPtr)0x74, IntPtr.Zero);
                //System.Threading.Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            #endregion Find PowerPoint & Send F5
        }

        public bool ShowBones { get; set; }
        public bool ShowJoints { get; set; }
        public bool ShowCenter { get; set; }
        public ImageType ImageType { get; set; }
       
        #region Trace KeyDown 
        /*
        protected override void OnKeyDown(KeyEventArgs keyEvent)
        {
            // Gets the key code 
            string key = "Key: " + keyEvent.Key.ToString();

            // Gets the key data; recognizes combination of keys 
            string Type = "Type: " + keyEvent.GetType();
            MessageBox.Show(key + ", " + Type);
        }*/
        #endregion Trace KeyDown

        private IntPtr FindWindowRecursive(IntPtr hParent, string szClass, string szCaption)
        {
            IntPtr hResult = FindWindowEx(hParent, IntPtr.Zero, szClass, szCaption);
            //SendMessage(hParent, WM_KEYDOWN, (IntPtr)0x74, IntPtr.Zero);
            //SendMessage(hParent, WM_CHAR, (IntPtr)0x74, IntPtr.Zero);
            //SendMessage(hParent, WM_KEYUP, (IntPtr)0x74, IntPtr.Zero);
            if (hResult != IntPtr.Zero)
                return hResult; // found it

            // enumerate all childs and if found one that has childs go in
            IntPtr hChild = FindWindowEx(hParent, IntPtr.Zero, null, null); // first child
            while (hChild != IntPtr.Zero)
            {
                // let's enumerate
                hResult = FindWindowRecursive(hChild, szClass, szCaption);
                if (hResult != IntPtr.Zero)
                    return hResult;  // found it
                hChild = GetWindow(hChild, GW_HWNDNEXT);
            }
            return IntPtr.Zero; // no childs, so no window was found
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.KinectSensors.FirstOrDefault();

            if (sensor == null)
            {
                MessageBox.Show("This application requires a Kinect sensor.");
                this.Close();
            }
            
            sensor.Start();

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);

            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.SkeletonStream.Enable();
            //sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            sensor.ElevationAngle = 0;

            Application.Current.Exit += new ExitEventHandler(Current_Exit);

            InitializeSpeechRecognition();
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (speechRecognizer != null)
            {
                speechRecognizer.RecognizeAsyncCancel();
                speechRecognizer.RecognizeAsyncStop();
            }
            if (sensor != null)
            {
                sensor.AudioSource.Stop();
                sensor.Stop();
                sensor.Dispose();
                sensor = null;
            }
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            sensor.Stop();
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C)
            {
                ToggleCircles();
            }
        }

        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (sensor == null || !sensor.IsRunning || !((KinectSensor)sender).SkeletonStream.IsEnabled)
                return;
            using (var image = e.OpenColorImageFrame())
            {
                if (image == null)
                    return;

                if (colorBytes == null ||
                    colorBytes.Length != image.PixelDataLength)
                {
                    colorBytes = new byte[image.PixelDataLength];
                }

                image.CopyPixelDataTo(colorBytes);

                //You could use PixelFormats.Bgr32 below to ignore the alpha,
                //or if you need to set the alpha you would loop through the bytes 
                //as in this loop below
                int length = colorBytes.Length;
                for (int i = 0; i < length; i += 4)
                {
                    colorBytes[i + 3] = 255;
                }

                BitmapSource source = BitmapSource.Create(image.Width,
                    image.Height,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    colorBytes,
                    image.Width * image.BytesPerPixel);
                videoImage.Source = source;
            }
        }

        //void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (sensor == null || !sensor.IsRunning || !((KinectSensor)sender).SkeletonStream.IsEnabled)
                return;
            Skeleton closestSkeleton;
            #region Find Skeleton
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;

                if (skeletons == null ||
                    skeletons.Length != skeletonFrame.SkeletonArrayLength)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                }

                skeletonFrame.CopySkeletonDataTo(skeletons);

                closestSkeleton = (from s in skeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked &&
                                                  s.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked
                                            select s).OrderBy(s => s.Joints[JointType.Head].Position.Z)
                                                    .FirstOrDefault();

                if (closestSkeleton == null)
                    return;

                #region default paint
                var head = closestSkeleton.Joints[JointType.Head];
                var rightHand = closestSkeleton.Joints[JointType.HandRight];
                var leftHand = closestSkeleton.Joints[JointType.HandLeft];

                if (head.TrackingState != JointTrackingState.Tracked ||
                    rightHand.TrackingState != JointTrackingState.Tracked ||
                    leftHand.TrackingState != JointTrackingState.Tracked)
                {
                    //Don't have a good read on the joints so we cannot process gestures
                    return;
                }

                SetEllipsePosition(ellipseHead, head, false);
                SetEllipsePosition(ellipseLeftHand, leftHand, isBackGestureActive);
                SetEllipsePosition(ellipseRightHand, rightHand, isForwardGestureActive);

                ProcessForwardBackGesture(head, rightHand, leftHand);
                #endregion default paint
            }
            #endregion Find Skeleton

            if (closestSkeleton != null)
            {
                using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
                {
                    #region depth_image
                    skeletonCanvas = this.skeletonCanvas1;                   
                    if (depthImageFrame != null)
                    {
                        skeletonCanvas.ShowBones = true;//this.ShowBones;
                        skeletonCanvas.ShowJoints = true;//this.ShowJoints;
                        skeletonCanvas.ShowCenter = true;//this.ShowCenter;

                        // Transform the data into the correct space
                        // For each joint, we determine the exact X/Y coordinates for the target view
                        foreach (Joint joint in closestSkeleton.Joints)
                        {
                            Point mappedPoint = this.GetPosition2DLocation(depthImageFrame, joint.Position);
                            jointMapping[joint.JointType] = new JointMapping
                            {
                                Joint = joint,
                                MappedPoint = mappedPoint
                            };
                            if (joint.TrackingState != 0)
                            {
                                //MessageBox.Show("Find someone's Skeleton!");
                                Console.WriteLine("Find someone's Skeleton!");
                            }
                        }
                        Point centerPoint = this.GetPosition2DLocation(depthImageFrame, closestSkeleton.Position);

                        // Scale the skeleton thickness
                        // 1.0 is the desired size at 640 width
                        double scale = this.RenderSize.Width / 640;

                        skeletonCanvas.RefreshSkeleton(closestSkeleton, jointMapping, centerPoint, scale);
                    }

                    #endregion depth_image
                }
            }
        }

        private Point GetPosition2DLocation(DepthImageFrame depthFrame, SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = depthFrame.MapFromSkeletonPoint(skeletonPoint);
            return new Point(
                        (int)(this.RenderSize.Width * depthPoint.X / depthFrame.Width),
                        (int)(this.RenderSize.Height * depthPoint.Y / depthFrame.Height));
            /*
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
            */
        }


        private void ProcessForwardBackGesture(Joint head, Joint rightHand, Joint leftHand)
        {
            if (rightHand.Position.X > head.Position.X + 0.45)
            {
                if (!isBackGestureActive && !isForwardGestureActive)
                {
                    isForwardGestureActive = true;
                    System.Windows.Forms.SendKeys.SendWait("{Right}");
                }
            }
            else
            {
                isForwardGestureActive = false;
            }

            if (leftHand.Position.X < head.Position.X - 0.45)
            {
                if (!isBackGestureActive && !isForwardGestureActive)
                {
                    isBackGestureActive = true;
                    System.Windows.Forms.SendKeys.SendWait("{Left}");
                }
            }
            else
            {
                isBackGestureActive = false;
            }
        }

        //This method is used to position the ellipses on the canvas
        //according to correct movements of the tracked joints.
        private void SetEllipsePosition(Ellipse ellipse, Joint joint, bool isHighlighted)
        {
            var point = sensor.MapSkeletonPointToColor(joint.Position, sensor.ColorStream.Format);

            if (isHighlighted)
            {
                ellipse.Width = 60;
                ellipse.Height = 60;
                ellipse.Fill = activeBrush;
            }
            else
            {
                ellipse.Width = 20;
                ellipse.Height = 20;
                ellipse.Fill = inactiveBrush;
            }

            Canvas.SetLeft(ellipse, (point.X - ellipse.ActualWidth / 2)/2);
            Canvas.SetTop(ellipse, (point.Y - ellipse.ActualHeight / 2)/2);
        }

        void ToggleCircles()
        {
            if (isCirclesVisible)
                HideCircles();
            else
                ShowCircles();
        }

        void HideCircles()
        {
            isCirclesVisible = false;
            ellipseHead.Visibility = System.Windows.Visibility.Collapsed;
            ellipseLeftHand.Visibility = System.Windows.Visibility.Collapsed;
            ellipseRightHand.Visibility = System.Windows.Visibility.Collapsed;
        }

        void ShowCircles()
        {
            isCirclesVisible = true;
            ellipseHead.Visibility = System.Windows.Visibility.Visible;
            ellipseLeftHand.Visibility = System.Windows.Visibility.Visible;
            ellipseRightHand.Visibility = System.Windows.Visibility.Visible;
        }

        #region Speech Recognition Methods

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        private void InitializeSpeechRecognition()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            if (ri == null)
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
Ensure you have the Microsoft Speech SDK installed.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                speechRecognizer = new SpeechRecognitionEngine(ri.Id);
            }
            catch
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
Ensure you have the Microsoft Speech SDK installed and configured.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            var phrases = new Choices();
            phrases.Add("computer show window");
            phrases.Add("computer hide window");
            phrases.Add("computer show circles");
            phrases.Add("computer hide circles");

            var gb = new GrammarBuilder();
            //Specify the culture to match the recognizer in case we are running in a different culture.                                 
            gb.Culture = ri.Culture;
            gb.Append(phrases);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);

            speechRecognizer.LoadGrammar(g);
            speechRecognizer.SpeechRecognized += SreSpeechRecognized;
            speechRecognizer.SpeechHypothesized += SreSpeechHypothesized;
            speechRecognizer.SpeechRecognitionRejected += SreSpeechRecognitionRejected;

            this.readyTimer = new DispatcherTimer();
            this.readyTimer.Tick += this.ReadyTimerTick;
            this.readyTimer.Interval = new TimeSpan(0, 0, 4);
            this.readyTimer.Start();

        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            this.StartSpeechRecognition();
            this.readyTimer.Stop();
            this.readyTimer = null;
        }

        private void StartSpeechRecognition()
        {
            if (sensor == null || speechRecognizer == null)
                return;

            var audioSource = this.sensor.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = audioSource.Start();
                
            speechRecognizer.SetInputToAudioStream(
                    kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            
        }

        void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Trace.WriteLine("\nSpeech Rejected, confidence: " + e.Result.Confidence);
        }

        void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Trace.Write("\rSpeech Hypothesized: \t{0}", e.Result.Text);
        }

        void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //This first release of the Kinect language pack doesn't have a reliable confidence model, so 
            //we don't use e.Result.Confidence here.
            if (e.Result.Confidence < 0.70)
            {
                Trace.WriteLine("\nSpeech Rejected filtered, confidence: " + e.Result.Confidence);
                return;
            }

            Trace.WriteLine("\nSpeech Recognized, confidence: " + e.Result.Confidence + ": \t{0}", e.Result.Text);

            if (e.Result.Text == "computer show window")
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        this.Topmost = true;
                        this.WindowState = System.Windows.WindowState.Normal;
                    });
            }
            else if (e.Result.Text == "computer hide window")
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.Topmost = false;
                    this.WindowState = System.Windows.WindowState.Minimized;
                });
            }
            else if (e.Result.Text == "computer hide circles")
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.HideCircles();
                });
            }
            else if (e.Result.Text == "computer show circles")
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.ShowCircles();
                });
            }
        }
        
        #endregion

    }
}
