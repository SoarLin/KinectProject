//using Microsoft.Samples.Kinect.KinectExplorer;

namespace WpfApplication1
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.Windows.Media;
    using System.Windows.Documents;
    using System.Windows.Input;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.KinectExplorer;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinectSensor;
        private byte[] colorData;
        private ImageFrame imageFrame;
        private WriteableBitmap outputImage;

        //Declare some global variables
        private short[] pixelData;
        private byte[] depthFrame32;
        
        //The bitmap that will contain the actual converted depth into an image
        private WriteableBitmap outputBitmap;
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        //Format of the last Depth Image. 
        //This changes when you first run the program or whenever you minimize the window 
        private DepthImageFormat lastImageFormat;

        //Identify each color layer on the R G B
        private const int RedIndex = 2;
        private const int GreenIndex = 1;
        private const int BlueIndex = 0;

        // color divisors for tinting depth pixels
        private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
        private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
        private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

        //#region ctor & Window events
        public MainWindow()
        {
            InitializeComponent();
           
            kinectSensor = KinectSensor.KinectSensors[0];
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.Start();
            kinectSensor.ColorFrameReady += ColorImageReady;
            kinectSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(DepthImageReady);
             
        }
        
        private void ColorImageReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
               if (imageFrame != null){
                    this.colorData = new byte[imageFrame.PixelDataLength];
                    imageFrame.CopyPixelDataTo(this.colorData);

                    this.outputImage = new WriteableBitmap(imageFrame.Width, imageFrame.Height,
                                       96, 96, PixelFormats.Bgr32, null);
                    this.kinectColorImage.Source = this.outputImage;

                    this.outputImage.WritePixels(new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                                    this.colorData, imageFrame.Width * Bgr32BytesPerPixel, 0);
                }else{ // imageFrame is null because the request did not arrive in time 
                }
            } //  using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
        }//private void ColorImageReady

        private void DepthImageReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame imageFrame = e.OpenDepthImageFrame())
            {
                if (imageFrame != null)
                {
                    bool NewFormat = this.lastImageFormat != imageFrame.Format;
                    if (NewFormat)
                    {
                        //Update the image to the new format
                        this.pixelData = new short[imageFrame.PixelDataLength];
                        this.depthFrame32 = new byte[imageFrame.Width * imageFrame.Height * Bgr32BytesPerPixel];

                        //Create the new Bitmap
                        this.outputBitmap = new WriteableBitmap(
                           imageFrame.Width,
                           imageFrame.Height,
                           96,  // DpiX
                           96,  // DpiY
                           PixelFormats.Bgr32,
                           null);

                        this.kinectDepthImage.Source = this.outputBitmap;
                    }
                    imageFrame.CopyPixelDataTo(this.pixelData);
                    byte[] convertedDepthBits = this.ConvertDepthFrame(this.pixelData, ((KinectSensor)sender).DepthStream);

                    this.outputBitmap.WritePixels(
                        new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                        convertedDepthBits,
                        imageFrame.Width * Bgr32BytesPerPixel,
                        0);

                    //Update the Format
                    this.lastImageFormat = imageFrame.Format;
                }
                else
                {
                    // imageFrame is null because the request did not arrive in time
                }
            }
        }

        /// <summary>
        /// ConvertDepthFrame:
        /// Converts the depth frame into its RGB version taking out all the player information and leaving only the depth.
        /// </summary>
        private byte[] ConvertDepthFrame(short[] depthFrame, DepthImageStream depthStream)
        {
            int tooNearDepth = depthStream.TooNearDepth;
            int tooFarDepth = depthStream.TooFarDepth;
            int unknownDepth = depthStream.UnknownDepth;
            //Run through the depth frame making the correlation between the two arrays
            for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < this.depthFrame32.Length; i16++, i32 += 4)
            {
                int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
                //We don't care about player's information here, so we are just going to rule it out by shifting the value.
                int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                //We are left with 13 bits of depth information that we need to convert into an 8 bit number for each pixel.
                //There are hundreds of ways to do this. This is just the simplest one.
                 byte intensity = (byte)(~(realDepth >> 4));

                if (player == 0 && realDepth == 0)
                {
                    // white 
                    this.depthFrame32[i32 + RedIndex] = 255;
                    this.depthFrame32[i32 + GreenIndex] = 255;
                    this.depthFrame32[i32 + BlueIndex] = 255;
                }
                else if (player == 0 && realDepth == tooFarDepth)
                {
                    // dark purple
                    this.depthFrame32[i32 + RedIndex] = 66;
                    this.depthFrame32[i32 + GreenIndex] = 0;
                    this.depthFrame32[i32 + BlueIndex] = 66;
                }
                else if (player == 0 && realDepth == unknownDepth)
                {
                    // dark brown
                    this.depthFrame32[i32 + RedIndex] = 66;
                    this.depthFrame32[i32 + GreenIndex] = 66;
                    this.depthFrame32[i32 + BlueIndex] = 33;
                }
                else
                {
                    if (player != 0)
                        Console.WriteLine("Player : "+player);
                    // tint the intensity by dividing by per-player values
                    this.depthFrame32[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
                    this.depthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
                    this.depthFrame32[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);
                }
            }
            //Now that we are done painting the pixels, we can return the byte array to be painted
            return this.depthFrame32;
        }
    }
}
