using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Drawing;

namespace FaceIdentification
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebCam webcam;
        //string personGroupId = "mynewncrgroup";
        string personGroupId = "mylatestncrgroup";

        public MainWindow()
        {
            InitializeComponent();
            webcam = new WebCam();
            webcam.InitializeWebCam(ref captureImage);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var faceServiceClient = new FaceServiceClient("Your Subscription Key");
            // Create an empty person group
            bool groupExists = false;
            try
            {

                Title = String.Format("Request: Group {0} will be used to build a person database. Checking whether the group exists.", personGroupId);
                await faceServiceClient.GetPersonGroupAsync(personGroupId);
                groupExists = true;
                Title = String.Format("Response: Group {0} exists.", personGroupId);

            }
            catch (FaceAPIException ex)
            {
                if (ex.ErrorCode != "PersonGroupNotFound")
                {
                    Title = String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                    return;
                }
                else
                {
                    Title = String.Format("Response: Group {0} did not exist previously.", personGroupId);
                }
            }

            if (groupExists)
            {
                Title = String.Format("Success..... Now your Group  {0} ready to use.", personGroupId);
                webcam.Start();
                return;
            }
            else
            {
                Console.WriteLine("Group did not exist. First you need to create a group");
            }
        }

        private async void faceIdentifyBtn_Click(object sender, RoutedEventArgs e)
        {
            faceIdentifyBtn.IsEnabled = false;
            testImage.Source = captureImage.Source;
            Helper.SaveImageCapture((BitmapSource)captureImage.Source);

            string getDirectory = Directory.GetCurrentDirectory();
            string filePath = getDirectory + "\\test1.jpg";

            System.Drawing.Image image1 = System.Drawing.Image.FromFile(filePath);
            //pictureBox1.Image = image1;
            


            var faceServiceClient = new FaceServiceClient("Your Subscription Key");
            try
            {
                Title = String.Format("Request: Training group \"{0}\"", personGroupId);
                await faceServiceClient.TrainPersonGroupAsync(personGroupId);

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    await Task.Delay(1000);
                    trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);
                    Title = String.Format("Response: {0}. Group \"{1}\" training process is {2}", "Success", personGroupId, trainingStatus.Status);
                    if (trainingStatus.Status.ToString() != "running")
                    {
                        break;
                    }


                }
            }
            catch (FaceAPIException ex)
            {

                Title = String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                faceIdentifyBtn.IsEnabled = true;
            }

            Title = "Identifing....";

            using (Stream s = File.OpenRead(filePath))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var faceRects = faces.Select(face => face.FaceRectangle);
                FaceRectangle[] faceRect = faceRects.ToArray();
                Title = String.Format("Detection Finished. {0} face(s) detected", faceRect.Length);
                await Task.Delay(1000);
                try
                {
                    var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);

                    foreach (var identifyResult in results)
                    {
                        Title = String.Format("Result of face: {0}", identifyResult.FaceId);

                        if (identifyResult.Candidates.Length == 0)
                        {
                            Title = String.Format("No one identified");
                        }
                        else
                        {
                            // Get top 1 among all candidates returned
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                            Title = String.Format("Identified as {0}", person.Name);
                        }
                    }
                }
                catch (FaceAPIException ex)
                {
                    Title = String.Format("Failed...Try Again.");
                    Console.WriteLine("Error : ", ex.Message);
                    image1.Dispose();
                    File.Delete(filePath);
                    GC.Collect();
                    //await Task.Delay(2000);
                    faceIdentifyBtn.IsEnabled = true;
                    return;
                }
            }
            image1.Dispose();
            File.Delete(filePath);
            GC.Collect();
            await Task.Delay(2000);
            faceIdentifyBtn.IsEnabled = true;
        }
    }
}
