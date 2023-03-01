using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.OleDb;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;

using DirectShowLib;
using System.Threading;
using System.IO;
using Emgu.CV.Face;

namespace FaceControl
{
    public partial class Form3 : Form
    {
        #region Variables
        OleDbConnection con = new OleDbConnection($@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Directory.GetCurrentDirectory()}\people.mdb; Persist Security Info=False");
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private VideoCapture capture = null;
        private DsDevice[] webcams = null;
        private int selectedCameraId = 0;
        private bool facesDetection = false;
        private Image<Bgr, byte> currentFrame = null;
        private bool isTrained = false;
        EigenFaceRecognizer recognizer;
        List<string> PersonsNames = new List<string>();
        List<Mat> TrainedFaces = new List<Mat>();
        List<int> PersonsLabes = new List<int>();
        Mat frame = new Mat();
        #endregion
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            facesDetection = true;
            TrainImagesFromDir();
            isTrained = true;
            try
            {
                capture = new VideoCapture();
                capture.ImageGrabbed += Capture_ImageGrabbed;
                capture.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool TrainImagesFromDir()
        {
            int ImagesCount = 0;
            TrainedFaces.Clear();
            PersonsLabes.Clear();
            PersonsNames.Clear();
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\TrainedImages";
                string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    Debug.WriteLine("files" + file);
                    Image<Gray, byte> trainedImage = new Image<Gray, byte>(file).Resize(200, 200, Inter.Cubic);
                    CvInvoke.EqualizeHist(trainedImage, trainedImage);
                    TrainedFaces.Add(trainedImage.Mat);
                    PersonsLabes.Add(ImagesCount);
                    string name = file.Split('\\').Last().Split('_')[0];
                    PersonsNames.Add(name);
                    ImagesCount++;
                    Debug.WriteLine("Imagecount" + ImagesCount + "name " + name);
                    Debug.WriteLine("Trained faces" + TrainedFaces.ToString());
                }

                if (TrainedFaces.Count() > 0)
                {
                    recognizer = new EigenFaceRecognizer(ImagesCount, double.PositiveInfinity);
                    recognizer.Train(TrainedFaces.ToArray(), PersonsLabes.ToArray());

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                isTrained = false;
                MessageBox.Show("Ошибка: " + ex.Message);
                return false;
            }

        }
        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                capture.Retrieve(frame, 0);
                currentFrame = frame.ToImage<Bgr, byte>().Resize(pictureBox1.Width, pictureBox1.Height, Inter.Cubic);
                //Обнаружение лица
                if (facesDetection)
                {
                    Mat grayImage = new Mat();
                    CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                    CvInvoke.EqualizeHist(grayImage, grayImage);
                    Rectangle[] faces = classifier.DetectMultiScale(grayImage, 1.2, 3, Size.Empty, Size.Empty);
                    if (faces.Length > 0)
                    {
                        foreach (var face in faces)
                        {
                            CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Red).MCvScalar, 2);
                            Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
                            resultImage.ROI = face;
                            if (isTrained)
                            {
                                Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                                CvInvoke.EqualizeHist(grayFaceResult, grayFaceResult);
                                var result = recognizer.Predict(grayFaceResult);
                                pictureBox1.Image = grayFaceResult.Bitmap;
                                Debug.WriteLine("debug result label" + result.Label + "debug result dist" + result.Distance);
                                if (result.Label != -1 && result.Distance < 30000)
                                {
                                    CvInvoke.PutText(currentFrame, PersonsNames[result.Label], new Point(face.X - 2, face.Y - 2),
                                        FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                                    CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Green).MCvScalar, 2);
                                }
                                else
                                {
                                    CvInvoke.PutText(currentFrame, "Unknown", new Point(face.X - 2, face.Y - 2),
                                        FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                                    CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Red).MCvScalar, 2);

                                }
                            }
                        }
                    }
                }
                pictureBox1.Image = currentFrame.Bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
