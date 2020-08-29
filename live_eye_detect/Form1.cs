using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Video.FFMPEG;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;

namespace live_eye_detect
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevicesList;
        private IVideoSource videoSource;

        public Form1()
        {
            InitializeComponent();
            // get list of video devices
            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
                comboBox1.Items.Add(videoDevice.Name);
            if (comboBox1.Items.Count > 0)            
                comboBox1.SelectedIndex = 0;
            else
                MessageBox.Show("No video sources found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // stop the camera on window close
            this.Closing += Form1_Closing;
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            // signal to stop
            if (videoSource != null && videoSource.IsRunning)
                videoSource.SignalToStop();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            csv.AppendLine("X;Y;Size;\n");
        }
        

        private void button1_Click(object sender, EventArgs e)
        {
            videoSource = new VideoCaptureDevice(videoDevicesList[comboBox1.SelectedIndex].MonikerString);
            videoSource.NewFrame += new NewFrameEventHandler(Device_NewFrame);  //camera_NewFrame simán camera   --- Device_NewFrame szemfelismeréssel camera
            videoSource.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //videoSource.NewFrame += new NewFrameEventHandler(Device_NewFrame);
            videoSource.SignalToStop();
            if (videoSource != null && videoSource.IsRunning && pic.Image != null)
            {
                pic.Image.Dispose();
            }
        }

        private void camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            pic.Image = bitmap;
        }

        public StringBuilder csv = new StringBuilder();
        public void csv_file(int[] x)
        {
            for (int i = 0; i < x.Length; i++)
            {
                csv.Append(x[i] + ";");
            }
            csv.Append("\n");

            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + textBox2.Text+".csv", csv.ToString());
        }


        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascades\\haarcascade_eye.xml");
        private void Device_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            Image<Bgr, byte> grayImage = new Image<Bgr, byte>(bitmap);
            Rectangle[] rectangles = cascadeClassifier.DetectMultiScale(grayImage, 3.5, 3);
            foreach (Rectangle rectangle in rectangles)
            {
                string result = String.Concat(rectangle);
                //Console.WriteLine(result);  {X=671,Y=254,Width=70,Height=70}
                csv_file(eyes_coordinate(result));


                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        graphics.DrawEllipse(pen, rectangle);
                    }
                }
            }
            pic.Image = bitmap;
        }


        private int[] eyes_coordinate(string x)
        {
            int[] coordinate = new int[3];
            string num = "";
            for (int i = 1; i < x.Length; i++)
            {
                if (x[i - 1] == 'X' && x[i] == '=')
                {
                    num = "";
                    for (int o = i + 1; o < x.Length; o++)
                    {
                        if (x[o] != ',')
                        {
                            num += x[o];
                        }
                        else
                        {
                            break;
                        }
                    }
                    coordinate[0] = Int32.Parse(num);
                }
                if (x[i - 1] == 'Y' && x[i] == '=')
                {
                    num = "";
                    for (int o = i + 1; o < x.Length; o++)
                    {
                        if (x[o] != ',')
                        {
                            num += x[o];
                        }
                        else
                        {
                            break;
                        }
                    }
                    coordinate[1] = Int32.Parse(num);
                }
                if (i >= 6) {
                    if (x[i - 5] == 'W' && x[i] == '=')
                    {
                        num = "";
                        for (int o = i + 1; o < x.Length; o++)
                        {
                            if (x[o] != ',')
                            {
                                num += x[o];
                            }
                            else
                            {
                                break;
                            }
                        }
                        coordinate[2] = Int32.Parse(num);
                    }
                }
            }
            return coordinate;
        }


    }
}
