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
    public partial class Form1 : Form
    {
        #region Variables
        OleDbConnection con = new OleDbConnection($@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Directory.GetCurrentDirectory()}\people.mdb; Persist Security Info=False");
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private VideoCapture capture = null;
        private DsDevice[] webcams = null;
        private int selectedCameraId = 0;
        private bool facesDetection = false;
        private bool enableSaveImage = false;
        private Image<Bgr, byte> currentFrame = null;
        private bool isTrained = false;
        EigenFaceRecognizer recognizer;
        List<string> PersonsNames = new List<string>();
        List<Mat> TrainedFaces = new List<Mat>();
        List<int> PersonsLabes = new List<int>();
        Mat frame = new Mat();
        #endregion
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            webcams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for(int i = 0; i < webcams.Length; i++)
            {
                toolStripComboBox1.Items.Add(webcams[i].Name);
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedCameraId = toolStripComboBox1.SelectedIndex;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (webcams.Length == 0)
                {
                    throw new Exception("Нет доступных камер!");
                }
                else if (toolStripComboBox1.SelectedItem == null)
                {
                    throw new Exception("Необходимо выбрать камеру!");
                }
                else if (capture != null)
                {
                    capture.Start();
                }
                else
                {
                    capture = new VideoCapture(selectedCameraId);
                    capture.ImageGrabbed += Capture_ImageGrabbed;
                    capture.Start();

                }
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
                    Debug.WriteLine("Imagecount"+ImagesCount + "name " + name);
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
                            if (enableSaveImage)
                            {
                                string path = Directory.GetCurrentDirectory() +@"\TrainedImages"+ @"\" +toolStripTextBox1.Text;
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);
                                Task.Factory.StartNew(() => {
                                    for(int i =0; i<10; i++)
                                    {
                                        resultImage.Resize(300, 300, Inter.Cubic).Save(path + @"\" + toolStripTextBox1.Text + "_" + DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss") + ".jpg");
                                        Thread.Sleep(500);
                                    }
                                });
                                string sql = $"INSERT INTO student (ID, name, file_path) VALUES('{Guid.NewGuid().ToString()}','{toolStripTextBox1.Text}','{path}')";
                                con.Open();
                                OleDbCommand command = new OleDbCommand(sql, con);
                                command.ExecuteNonQuery();
                                con.Close();
                                MessageBox.Show("Успешно добавлено"," ", MessageBoxButtons.OK);
                            }
                            enableSaveImage = false;
                            if (isTrained)
                            {
                                Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                                CvInvoke.EqualizeHist(grayFaceResult, grayFaceResult);
                                var result = recognizer.Predict(grayFaceResult);
                                pictureBox1.Image = grayFaceResult.Bitmap;
                                Debug.WriteLine("debug result label"+result.Label +"debug result dist"+ result.Distance);
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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (capture != null)
                {
                    capture.Pause();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                if (capture != null)
                {
                    capture.Pause();
                    capture.Dispose();
                    capture = null;
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                    selectedCameraId = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            try
            {
                facesDetection = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                enableSaveImage = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            isTrained = true;
        }
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            TrainImagesFromDir();
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5();
            form5.Show();
        }
    }
}
