namespace PicknPlaceHLib
{
    extern alias dest;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using dest.HalconDotNet;
    using System.Reflection;
    using System.IO;
    using System.Drawing;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Security;

    /// <summary>
    /// libDisp Class
    /// ply, om3, sfm 파일을 입력 받아 디스플레이에 이미지를 뿌려주는 클래스
    /// </summary>
    public class libDisp
    {
        private HDevEngine MyEngine;
        private HDevProcedureCall ProcCall;
        private HDevProcedure Procedure;
        private HDevOpMultiWindowImpl HDevWin2D;
        private HDevOpMultiWindowImpl HDevWin3D;
        private Thread thread;
        private string ImageFileName;
        private int MovingView;
        private HWindow extWin3D;

        private const string PATH_NAME = "./halconDev";
        private const string DUMMY_PROCEDURE_PATH_NAME = "./halconDev/DummyDisp.png";
        private const string DUMMY_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.DummyDisp.png";
        private const string DUMMY_PROCEDURE_NAME = "DummyDisp.png";

        private const string VIS_PROCEDURE_PATH_NAME = "./halconDev/HalconVis_0_0_1.hdvp";
        private const string VIS_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.HalconVis_0_0_1.hdvp";
        private const string VIS_PROCEDURE_NAME = "HalconVis_0_0_1";
        private const string ClassName = "libDisp";

        //private string visDummyfilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + "Dummy";

        /// <summary>
        /// libDisp 생성자
        /// </summary>
        public libDisp()
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            

            if (!Directory.Exists(PATH_NAME))
            {
                Directory.CreateDirectory(PATH_NAME);
            }

            if (!File.Exists(DUMMY_PROCEDURE_PATH_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = DUMMY_RESOURCE_PATH_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    Image img = Bitmap.FromStream(stream);
                    img.Save(DUMMY_PROCEDURE_PATH_NAME, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            if (File.Exists(VIS_PROCEDURE_PATH_NAME))
            {
                File.Delete(VIS_PROCEDURE_PATH_NAME);
            }
            if (!File.Exists(VIS_PROCEDURE_PATH_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = VIS_RESOURCE_PATH_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(VIS_PROCEDURE_PATH_NAME, result);
                }
            }
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }


        /// <summary>
        /// getDisp ply, om3, sfm 파일의 2D, 3D 이미지를 hwin에 넣어주는 메서드
        /// </summary>
        /// <param name="ImageFileName">전체 경로와 확장자를 포함한 파일 문자열 (.ply, .om3, .sfm)</param>
        /// <param name="MovingView">3D View Static = 0, 3D View Movable and Button(강제종료 불가)버전 = 1</param>
        /// <param name="extWin2D">form의 2D hwindow 핸들(om3, sfm 파일일 경우 null 입력 가능)</param>
        /// <param name="extWin3D">form의 3D hwindow 핸들</param>
        public void getDisp(string ImageFileName, int MovingView, HWindow extWin2D, HWindow extWin3D)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            
            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            this.extWin3D = extWin3D;
            try
            {
                if (thread != null)
                {
                    if (HDevWin2D != null)
                    {
                        HDevWin2D.Dispose();
                    }
                    if (HDevWin3D != null)
                    {
                        HDevWin3D.Dispose();
                    }
                    if (Procedure != null)
                    {
                        Procedure.Dispose();
                    }
                    if (ProcCall != null)
                    {
                        ProcCall.Dispose();
                    }
                    if (MyEngine != null)
                    {
                        MyEngine.Dispose();
                    }
                    
                    thread.Abort();
                    thread = null;
                }
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
            }
            
            this.ImageFileName = ImageFileName;
            this.MovingView = MovingView;
            if (thread != null)
            {
                thread.Interrupt();
            }
            MyEngine = new HDevEngine();
            MyEngine.SetProcedurePath(PATH_NAME);
            //MyEngine.SetEngineAttribute("execute_procedures_jit_compiled", "true");


            thread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    HDevWin2D = new HDevOpMultiWindowImpl(extWin2D);
                    HDevWin3D = new HDevOpMultiWindowImpl(extWin3D);
                    MyEngine.SetHDevOperators(HDevWin2D);
                    MyEngine.SetHDevOperators(HDevWin3D);
                    Procedure = new HDevProcedure(VIS_PROCEDURE_NAME);
                    //Procedure.CompileUsedProcedures();
                    ProcCall = new HDevProcedureCall(Procedure);
                    
                    ProcCall.SetInputCtrlParamTuple(1, ImageFileName);
                    ProcCall.SetInputCtrlParamTuple(2, MovingView);
                    ProcCall.SetInputCtrlParamTuple(3, extWin2D);
                    ProcCall.SetInputCtrlParamTuple(4, extWin3D);

                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                        "PATH_NAME " + PATH_NAME + '\n' +
                        "VIS_PROCEDURE_NAME " + VIS_PROCEDURE_NAME + '\n' +
                        "ImageFileName " + ImageFileName + '\n' +
                        "extWin2D " + extWin2D + '\n' +
                        "extWin3D " + extWin3D + '\n' +
                        "samplesurfValue " + extWin3D + '\n');
                    ProcCall.Execute();

                    
                }
                catch (HDevEngineException Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                }
                catch (Exception Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                }
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
            }));
            thread.Start();
        }

        /// <summary>
        /// Dispose 본 라이브러리에서 사용한 객체 파괴 메서드
        /// </summary>
        public void Dispose()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            try
            {
                if (thread.IsAlive)
                {
                    //HDevWin2D.DevClearWindow();
                    //HDevWin2D.DevCloseWindow();
                    HDevWin2D.Dispose();
                    //HDevWin3D.DevClearWindow();
                    //HDevWin3D.DevCloseWindow();
                    HDevWin3D.Dispose();
                    ProcCall.Dispose();
                    Procedure.Dispose();
                    //MyEngine.UnloadProcedure("CreateSurfModel");
                    //thread.Interrupt();
                    
                    thread.Abort();
                    thread = null;

                    if (File.Exists(VIS_PROCEDURE_PATH_NAME))
                    {
                        File.Delete(VIS_PROCEDURE_PATH_NAME);
                    }
                }

                if (ProcCall != null)
                {
                    ProcCall.Dispose();
                }
                MyEngine.UnloadAllProcedures();
                MyEngine.Dispose();
                HDevWin2D = null;
                HDevWin3D = null;
                ProcCall = null;
                Procedure = null;
                MyEngine = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(VIS_PROCEDURE_PATH_NAME))
                {
                    File.Delete(VIS_PROCEDURE_PATH_NAME);
                }
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
            }
          
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }
    }
}
