using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Zivid;
using Duration = Zivid.NET.Duration;
using System.IO;
using System.Threading;
using HandEye = Zivid.NET.HandEye;
using CookComputing.XmlRpc;
using System.Diagnostics;

namespace GC_PicknPlace
{
    public partial class Form1 : Form
    {
        Zivid.NET.Application zivid = new Zivid.NET.Application();
        
        Zivid.NET.CloudVisualizer visualizer = new Zivid.NET.CloudVisualizer();

        Zivid.NET.CloudVisualizerForm visualizerForm = new Zivid.NET.CloudVisualizerForm();

        IZivid proxy;

        HDev halconTest;

        /*
        public static void AddEnvPath(params string[] paths)
        {
            
            // PC에 설정되어 있는 환경 변수를 가져온다.
            var envPaths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).ToList();
            // 중복 환경 변수가 없으면 list에 넣는다.
            envPaths.InsertRange(0, paths.Where(x => x.Length > 0 && !envPaths.Contains(x)).ToArray());
            // 환경 변수를 다시 설정한다.
            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), envPaths), EnvironmentVariableTarget.Process);
        }*/

        /*public void initPipe()
        {
            NamedPipeServerStream server = new NamedPipeServerStream("CSServer", PipeDirection.InOut);
            server.WaitForConnection();
            MemoryStream stream;
            while (true)
            {
                try
                {
                    if (!server.IsConnected) { 
                        server.WaitForConnection();
                        continue;
                    }

                    stream = new MemoryStream();
            

               
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write("print \"hello\"");
                        server.Write(stream.ToArray(), 0, stream.ToArray().Length);
                    }
                    stream.Close();

                    stream = new MemoryStream();
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int length = reader.ReadInt32();
                        byte[] buffer = new byte[length];
                        reader.Read(buffer, 0, length);
                        Console.WriteLine(buffer);
                    }
                }
                catch(Exception ex)
                {
                    //Console.WriteLine(ex);
                }
                
            }


            
            server.Disconnect();
            server.Close();

        }*/

        [XmlRpcUrl("http://127.0.0.1:55556/RPC2")]
        public interface IZivid : IXmlRpcProxy
        {
            [XmlRpcMethod("msg")]
            string zividfunc(string msg);
        }
        

        public Form1()
        {
            
            InitializeComponent();
            //initPipe();

            proxy = XmlRpcProxyGen.Create<IZivid>();
            halconTest = HDev.Instance();
            Console.WriteLine("Setting up visualization");
            zivid.DefaultComputeDevice = visualizer.ComputeDevice;

            /*
            var PYTHON_HOME = Environment.ExpandEnvironmentVariables(@"C:\ProgramData\Anaconda3\envs\zivid\");
            // 환경 변수 설정
            AddEnvPath(PYTHON_HOME, Path.Combine(PYTHON_HOME, @"Library\bin"));
            // Python 홈 설정.
            PythonEngine.PythonHome = PYTHON_HOME;
            // 모듈 패키지 패스 설정.
            PythonEngine.PythonPath = string.Join(
                Path.PathSeparator.ToString(),
                new string[] {
                    PythonEngine.PythonPath,// pip하면 설치되는 패키지 폴더.
                    Path.Combine(PYTHON_HOME, @"Lib\site-packages"),// 개인 패키지 폴더
                    @"C:\Users\ABC\Documents\Visual Studio 2015\Projects\GC_PicknPlace_0.1\GC_PicknPlace\bin\x64\Debug\PyProj"
                }
            );
            // Python 엔진 초기화
            PythonEngine.Initialize();





            Console.WriteLine("Setting up visualization");
            zivid.DefaultComputeDevice = visualizer.ComputeDevice;
            
            using (Py.GIL())
            {
                dynamic np = Py.Import("numpy");
                dynamic cv2 = Py.Import("cv2");
                //dynamic test = Py.Import("create_zdf_pose");

                Console.WriteLine(np.cos(np.pi * 2));
                
                //dynamic filestorage = cv2.FileStorage(file_name, cv2.FILE_STORAGE_READ);
                //PyObject test = PythonEngine.ImportModule("PicknPlace");
                
                //if (null != test)
                //{
                //    Console.WriteLine("...");
                //    PyObject tests = test.InvokeMethod("go");
                //}


                dynamic sin = np.sin;
                Console.WriteLine(sin(5));
                
                double c = np.cos(5) + sin(5);
                Console.WriteLine(c);

                dynamic a = np.array(new List<float> { 1, 2, 3 });
                Console.WriteLine(a.dtype);

                dynamic b = np.array(new List<float> { 6, 5, 4 }, dtype: np.int32);
                Console.WriteLine(b.dtype);

                Console.WriteLine(a * b);
                //Console.ReadKey();
            }
            PythonEngine.Shutdown();
            */
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("Connecting to the camera");
                var camera = zivid.ConnectCamera();

                Console.WriteLine("Recording HDR source images");
                var frames = new List<Zivid.NET.Frame>();
                camera.UpdateSettings(s =>
                {
                    s.Iris = 10;
                    s.ExposureTime = Duration.FromMicroseconds(10000);
                    s.Brightness = 1;
                    s.Gain = 1;
                    s.Bidirectional = false;
                    s.Filters.Contrast.Enabled = true;
                    s.Filters.Contrast.Threshold = 5;
                    s.Filters.Gaussian.Enabled = true;
                    s.Filters.Gaussian.Sigma = 1.5;
                    s.Filters.Outlier.Enabled = true;
                    s.Filters.Outlier.Threshold = 5;
                    s.Filters.Reflection.Enabled = true;
                    s.Filters.Saturated.Enabled = true;
                    s.BlueBalance = 1.081;
                    s.RedBalance = 1.709;
                });
                frames.Add(camera.Capture());
                Console.WriteLine("Frame 1 " + camera.Settings.ToString());

                camera.UpdateSettings(s =>
                {
                    s.Iris = 20;
                    s.ExposureTime = Duration.FromMicroseconds(20000);
                    s.Brightness = 0.5;
                    s.Gain = 2;
                    s.Bidirectional = false;
                    s.Filters.Contrast.Enabled = true;
                    s.Filters.Contrast.Threshold = 5;
                    s.Filters.Gaussian.Enabled = true;
                    s.Filters.Gaussian.Sigma = 1.5;
                    s.Filters.Outlier.Enabled = true;
                    s.Filters.Outlier.Threshold = 5;
                    s.Filters.Reflection.Enabled = true;
                    s.Filters.Saturated.Enabled = true;
                    s.BlueBalance = 1.081;
                    s.RedBalance = 1.709;
                });
                frames.Add(camera.Capture());
                Console.WriteLine("Frame 2 " + camera.Settings.ToString());

                camera.UpdateSettings(s =>
                {
                    s.Iris = 30;
                    s.ExposureTime = Duration.FromMicroseconds(33000);
                    s.Brightness = 1;
                    s.Gain = 1;
                    s.Bidirectional = true;
                    s.Filters.Contrast.Enabled = true;
                    s.Filters.Contrast.Threshold = 5;
                    s.Filters.Gaussian.Enabled = true;
                    s.Filters.Gaussian.Sigma = 1.5;
                    s.Filters.Outlier.Enabled = true;
                    s.Filters.Outlier.Threshold = 5;
                    s.Filters.Reflection.Enabled = true;
                    s.Filters.Saturated.Enabled = true;
                    s.BlueBalance = 1.081;
                    s.RedBalance = 1.709;
                });
                frames.Add(camera.Capture());
                Console.WriteLine("Frame 3 " + camera.Settings.ToString());

                Console.WriteLine("Creating the HDR frame");
                var hdrFrame = Zivid.NET.HDR.CombineFrames(frames);

                Console.WriteLine("Saving the frames");
                //frames[0].Save("10.zdf");
                //frames[1].Save("20.zdf");
                //frames[2].Save("30.zdf");
                //hdrFrame.Save("HDR.zdf");

                Console.WriteLine("Displaying the HDR frame");
                
                //visualizer.ResetToFit();

                Console.WriteLine("Running the visualizer. Blocking until the window closes");
                
                
                //visualizer.Activate();
                
                //visualizer.ShowDialog();

                Thread thread = new Thread(new ThreadStart(delegate ()
                {
                    this.Invoke(new Action(delegate ()
                    {
                        
                        //visualizer.Show();
                        visualizer.Show(hdrFrame);
                        visualizer.ShowMaximized();
                        visualizer.ShowFullScreen();
                        visualizer.Run();
                    }));
                }));
                thread.Start();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String test = "zzzzzㅋㅋㅋㅁㅁaa";
            String msg = "l";
            try
            {
                String z = textBox1.Text;
                String x = proxy.zividfunc(z.Trim());
                label1.Text = z + " 명령 파이선 전달";
                Console.WriteLine(x);
                label1.Text = "파이선 응답: "+ x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //label1.Text = "Status";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            label1.Text = "연산시작";
            for (int i = 0; i < 1; i++) { 
                //Stopwatch stopw = new Stopwatch();
                //stopw.Start();
            
                var camera = zivid.ConnectCamera();
                label1.Text = "카메라 연결";
                Console.WriteLine("Recording HDR source images");
                var suggestSettingsParameters = new Zivid.NET.CaptureAssistant.SuggestSettingsParameters(Duration.FromMilliseconds(1200), Zivid.NET.CaptureAssistant.AmbientLightFrequency.hz60);
                Console.WriteLine("Running Capture Assistant with parameters: {0}", suggestSettingsParameters);
                var settingsList = Zivid.NET.CaptureAssistant.SuggestSettings(camera, suggestSettingsParameters);
                var frame = Zivid.NET.HDR.Capture(camera, settingsList);
                Console.WriteLine("Saving the frames");
                label1.Text = "할콘 연산용 이미지 저장";
                //frames[0].Save("10.zdf");
                //frames[1].Save("20.zdf");
                //frames[2].Save("30.zdf");
                frame.Save("../TEST.ply");
                frame.Save("../TEST.zdf");
                camera.Disconnect();
                label1.Text = "카메라 연결해제";



                if (halconTest.isInit)
                {
                    label1.Text = "할콘 연산시작";
                    halconTest.ProgramRun();

                    double[] DArr = halconTest.getPointArray();
                    try
                    {
                            String t = "";
                            if ((int)DArr[0] == 0 && (int)DArr[1] == 0 && (int)DArr[2] == 0)
                            {
                                label1.Text = "할콘 연산실패";
                                t = "Failed," + String.Format("{0:F1}", DArr[0]) + "," + String.Format("{0:F1}", DArr[1]) + "," + String.Format("{0:F1}", DArr[2]) + "," + String.Format("{0:F1}", DArr[3]) + "," + String.Format("{0:F1}", DArr[4]) + "," + String.Format("{0:F1}", DArr[5]);
                        }
                            else
                            {
                                label1.Text = "할콘 연산성공";
                                t = "v," + String.Format("{0:F1}", DArr[0]) + "," + String.Format("{0:F1}", DArr[1]) + "," + String.Format("{0:F1}", DArr[2]) + "," + String.Format("{0:F1}", DArr[3]) + "," + String.Format("{0:F1}", DArr[4]) + "," + String.Format("{0:F1}", DArr[5]);
                            }
                            Console.WriteLine(t);
                            String x = proxy.zividfunc(t);
                            label1.Text = "파이선에게 좌표 전달";
                            Console.WriteLine(x);
                            label1.Text = "파이선 응답: " + x;
                            halconTest.Dispose();
                            halconTest = HDev.Instance();
                    }
                    catch (Exception ex)
                    {
                        label1.Text = "할콘 연산실패";
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    halconTest.Dispose();
                    halconTest = HDev.Instance();
                }

                //stopw.Stop();
                //Console.WriteLine(stopw.ElapsedMilliseconds.ToString() + " ms");
                //FileStream fs = new FileStream("C:/Users/ABC/Documents/Visual Studio 2015/Projects/GC_PicknPlace/GC_PicknPlace/bin/x64/Debug/PyProj/LOG200320_1/CycleLog.txt", FileMode.Append, FileAccess.Write);
                //label1.Text = "싸이클타임 기록";
                //FileMode중 append는 이어쓰기. 파일이 없으면 만든다.

                //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

                //sw.WriteLine(stopw.ElapsedMilliseconds.ToString() + " ms");

                //sw.Flush();

                //sw.Close();

                //fs.Close();
                //test.Dispose();
                //label1.Text = "Status";
            }
            return;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                double[] DArr = new double[] { -9, 183.4, 293.97, 3.061, -0.051, 0.068, 0.1, 0.2 };
                String t = t = "m," + String.Format("{0:F2}", DArr[0]) + "," + String.Format("{0:F2}", DArr[1]) + "," + String.Format("{0:F2}", DArr[2]) + "," +
                                      String.Format("{0:F2}", DArr[3]) + "," + String.Format("{0:F2}", DArr[4]) + "," + String.Format("{0:F2}", DArr[5]) + "," +
                                      String.Format("{0:F2}", DArr[6]) + "," + String.Format("{0:F2}", DArr[7]);
                label1.Text = "로봇 CamHome 좌표 파이선 전달";
                Console.WriteLine(t);
                String x = proxy.zividfunc(t);
                Console.WriteLine(x);
                label1.Text = "파이선 응답:" + x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //label1.Text = "Status";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                double[] DArr = new double[] { 293.41, 140.87, 322.05, 3.221, 0.049, 0.061, 0.1, 0.2 };
                String t = t = "m," + String.Format("{0:F2}", DArr[0]) + "," + String.Format("{0:F2}", DArr[1]) + "," + String.Format("{0:F2}", DArr[2]) + "," +
                                      String.Format("{0:F2}", DArr[3]) + "," + String.Format("{0:F2}", DArr[4]) + "," + String.Format("{0:F2}", DArr[5]) + "," +
                                      String.Format("{0:F2}", DArr[6]) + "," + String.Format("{0:F2}", DArr[7]);
                label1.Text = "로봇 PickHome 좌표 파이선 전달";
                Console.WriteLine(t);
                String x = proxy.zividfunc(t);
                Console.WriteLine(x);
                label1.Text = "파이선 응답:"+ x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //label1.Text = "Status";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            

        }
    }
}
