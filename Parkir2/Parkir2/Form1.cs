using System;
using System.Windows.Forms;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.CvEnum;
using AForge.Imaging.Filters;
using Accord.Imaging;
using System.Collections.Generic;

namespace Parkir2
{
    public partial class ParkirFastDetect : Form
    {
        public ParkirFastDetect()
        {
            InitializeComponent();
        }
        FilterInfoCollection filter;
        VideoCaptureDevice device;
        private bool setz;
        private bool setH;
        int h, m, s;

        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("cars.xml");
        private void ParkirFastDetect_Load(object sender, EventArgs e)
        {
            filter = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in filter)
                cboDevice.Items.Add(device.Name);
            cboDevice.SelectedIndex = 0;
            device = new VideoCaptureDevice();
            device = new VideoCaptureDevice(filter[cboDevice.SelectedIndex].MonikerString);
            device.NewFrame += Device_NewFrame;

            Timer oTimer = new Timer(); 
            oTimer.Interval = (45* 60 * 1000);
            oTimer.Tick += new EventHandler(oTimer_Tick);

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            device = new VideoCaptureDevice(filter[cboDevice.SelectedIndex].MonikerString);
            device.NewFrame += Device_NewFrame;
            device.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            device.SignalToStop();
            device.NewFrame -= new NewFrameEventHandler(Device_NewFrame);
            device = null;
        }

        private void Device_NewFrame(Object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bit = (Bitmap)eventArgs.Frame.Clone();
            Image<Bgr, Byte> grayImage = bit.ToImage<Bgr, byte>();
                using (Graphics graphics = Graphics.FromImage(bit))
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                    graphics.DrawRectangle(pen, 1190, 740, 200, 250);
                    graphics.DrawRectangle(pen, 1420, 740, 200, 250);
                    }
                }
            pictureBox1.Image = bit;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string num = "123456789";
            int len = num.Length;
            string otp = string.Empty;
            int otpdigit = 5;
            string finaldigit;

            int getindex;
            for (int i = 0; i < otpdigit; i++)
            {
                do
                {
                    getindex = new Random().Next(0, len);
                    finaldigit = num.ToCharArray()[getindex].ToString();
                }
                while (otp.IndexOf(finaldigit) != -1);
                otp += finaldigit;
            }

            string filename = @"D:\capture\" + otp + ".jpg";
            Crop filter = new Crop(new Rectangle(1190, 740, 200, 250));
            Bitmap bitmap = new Bitmap(pictureBox1.Image);
            Bitmap newImage = filter.Apply(bitmap);
            System.Drawing.Imaging.ImageFormat imageFormat = null;
            imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
            newImage.Save(filename);
            pictureBox3.Image = newImage;
            setz = true;
            oTimer.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (setz == true)
            {
                using (Bitmap bitm = new Bitmap(pictureBox3.Image))
                using (Bitmap bitm2 = new Bitmap(pictureBox1.Image))
                {
                    Image<Bgr, byte> img = bitm.ToImage<Bgr, byte>();
                    Image<Bgr, byte> img2 = bitm2.ToImage<Bgr, byte>();

                    var gray = img.Convert<Gray, byte>();
                    var gray2 = img2.Convert<Gray, byte>();
                    int k = 2;
                    double uniquenessThreshold = 0.8;
                    Mat mask = new Mat();
                    Mat homography = null;
                    VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
                    VectorOfKeyPoint modelKeyPoints = new VectorOfKeyPoint();
                    VectorOfKeyPoint observedKeyPoints = new VectorOfKeyPoint();

                    FastFeatureDetector fast = new FastFeatureDetector(10, true);
                    Freak descriptor = new Freak();

                    Mat modelDescriptors = new Mat();
                    //detector.DetectAndCompute(gray, null, modelKeyPoints, modelDescriptors, false);
                    //var keypoint1 = fast.Detect(gray);
                    fast.DetectRaw(gray, modelKeyPoints, null);
                    descriptor.Compute(gray, modelKeyPoints, modelDescriptors);

                    Mat observedDescriptors = new Mat();
                    //detector.DetectAndCompute(gray2, null, observedKeyPoints, observedDescriptors, false);
                    //var keypoint2 = fast.Detect(gray2);
                    fast.DetectRaw(gray2, observedKeyPoints, null);
                    descriptor.Compute(gray2, observedKeyPoints, observedDescriptors);

                    //Features2DToolbox.DrawKeypoints(img, new VectorOfKeyPoint(keypoint1), modelDescriptors, new Bgr(255, 255, 0));
                    //Features2DToolbox.DrawKeypoints(img2, new VectorOfKeyPoint(keypoint2), observedDescriptors, new Bgr(255, 255, 0));

                    using (BFMatcher matcher = new BFMatcher(DistanceType.L2))
                    {
                        matcher.Add(modelDescriptors);

                        matcher.KnnMatch(observedDescriptors, matches, k, null);
                        mask= new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                        mask.SetTo(new MCvScalar(255));
                        Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                        int nonZeroCount = CvInvoke.CountNonZero(mask);
                        if (nonZeroCount >= 4)
                        {
                           nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, matches, mask,
                                1.5, 20);
                            if (nonZeroCount >= 4)
                                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, matches,
                                    mask, 2);
                        }
                        Mat result = new Mat();
                        Features2DToolbox.DrawMatches(gray, modelKeyPoints, img2, observedKeyPoints, matches, result,
                             new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);
                        
                        if (homography != null)
                        {
                            Rectangle rect = new Rectangle(System.Drawing.Point.Empty, gray.Size);
                            PointF[] pts = new PointF[]
                            {
                                new PointF(rect.Left, rect.Bottom),
                                new PointF(rect.Right, rect.Bottom),
                                new PointF(rect.Right, rect.Top),
                                new PointF(rect.Left, rect.Top)};
                            pts = CvInvoke.PerspectiveTransform(pts, homography);
                            System.Drawing.Point[] points = Array.ConvertAll<PointF, System.Drawing.Point>(pts, System.Drawing.Point.Round);

                            using(VectorOfPoint vp = new VectorOfPoint(points))
                            {
                                CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                            }
                        }
                        pictureBox2.Image = result.ToBitmap();
                    }
                }
            }
            //if (setz == false)
            //{
                
           // }
            if (setH == true)
            {
                Int32.TryParse(textBox1.Text, out int h);
                if (h <= 1671318060)
                {
                    textBox2.Text = "Rp. 2000";
                }
                else if (h <= 1671318120)
                {
                    textBox2.Text = "Rp.4.000";
                }
                else if (h <= 1671318180)
                {
                    textBox2.Text = "Rp.6.000";
                }
                else if (h <= 1671318240)
                {
                    textBox2.Text = "Rp.8.000";
                }
                else if (h <= 1671318300)
                {
                    textBox2.Text = "Rp.10.000";
                }
                else
                {
                    textBox2.Text = "Lebih Dari 5 Menit";
                }
            }
        }

        private void stopDetect_Click(object sender, EventArgs e)
        {
            oTimer.Stop();
            setH = true;
        }

        private void ParkirFastDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (device.IsRunning == true)
            {
                this.device.Stop();
            }
        }

        private void oTimer_Tick(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                s += 1;
                if (s == 60)
                {
                    s = 0;
                    m += 1;
                }
                if (m == 60)
                {
                    m = 0;
                    h += 1;
                }
                textBox1.Text = String.Format("{0}:{1}:{2}", h.ToString().PadLeft(2, '0'), m.ToString().PadLeft(2, '0'), s.ToString().PadLeft(2, '0'));
            }));

        }
    }
}
