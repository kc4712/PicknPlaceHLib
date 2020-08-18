

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

    /// <summary>
    /// libModelUnion Class
    /// 피사체를 다각도에서 찍은 생성모델을 합성하여 하나의 입체 모델을 만드는 클래스
    /// </summary>
    public class libModelUnion
    {
        private Thread thread;
        private HDevEngine MyEngine;
        private HDevProcedureCall ProcCall;
        private HDevOpMultiWindowImpl HDevWin;
        private HDevProcedure Procedure;
        private string[] filesName;
        //private string ProcedurePath = "./";

        private string register_obj3d_Param = "robust";
        private double smooth_mls_knncnt = 60;

        private int triangulate_greedyKnnCnt = 40;
        private string triangulate_greedyKnnRadiusParam = "auto";
        private double triangulate_greedyKnnRadiusValue = 0.1;
        private double triangulate_smallsurfaceremoveValue = 0.1;
        private int triangulate_greedy_mesh_dilationValue = 0;

        private string sampling_method = "fast";
        private double sampling_distance = 0.5;

        private double create_sfm_RelSampleDistance = 0.9;
        private HTuple RetOM3;
        private HTuple RetSFM;

        private const string PATH_NAME = "./halconDev";
        private const string DUMMY_PROCEDURE_PATH_NAME = "./halconDev/DummyDisp.png";
        private const string DUMMY_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.DummyDisp.png";
        private const string DUMMY_PROCEDURE_NAME = "DummyDisp.png";

        private const string UNION_PROCEDURE_PATH_NAME = "./halconDev/ModelUnion_0_0_2.hdvp";
        private const string UNION_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.ModelUnion_0_0_2.hdvp";
        private const string UNION_PROCEDURE_NAME = "ModelUnion_0_0_2";
        private const string ClassName = "libModelUnion";


        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        /// <value>이벤트 콜백</value>
        /// <param name = "hfunc">"p" = PROCEDURE OBJ, "m" = MODEL VAR</param>
        /// <param name = "state">ENUM_HPROCEDURESTATE, ENUM_HMODELSTATE 값 콜백</param>
        public delegate void CBHalconState(string hfunc, int state);
        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        /// <value>이벤트 콜백</value>
        public event CBHalconState mCBHalconState;

        /// <summary>
        /// HPROCEDURE_STATE PROCEDURE 객체의 상태를 확인할 수 있는 Property 변수.
        /// ENUM_HPROCEDURESTATE 값을 SET하게 되어 있다
        /// </summary>
        public int HPROCEDURE_STATE { get; private set; }

        /// <summary>
        /// HMODEL_STATE MODEL 변수의 상태를 확인할 수 있는 Property 변수.
        /// ENUM_HMODELSTATE값을 SET하게 되어있다
        /// </summary>
        public int HMODEL_STATE { get; private set; }


        /// <summary>
        /// libModelUnion 생성 메서드
        /// </summary>
        public libModelUnion()
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            MyEngine = new HDevEngine();
            HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// libModelUnion 생성 메서드
        /// 배경을 포함한 피사체 영역의 z축 min, max와, 피사체 영역의 왼쪽상단 꼭지점, 오른쪽하단꼭지점 만큼을 이용해 입체 사각형을 만들어 피사체와 배경을 분리
        /// </summary>
        /// <param name="filesName">경로를 포함한 om3파일명 </param>
        public libModelUnion(string[] filesName)
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            MyEngine = new HDevEngine();
            this.filesName = filesName;
            HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
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
            if (File.Exists(UNION_PROCEDURE_PATH_NAME))
            {
                File.Delete(UNION_PROCEDURE_PATH_NAME);
            }
            if (!File.Exists(UNION_PROCEDURE_PATH_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = UNION_RESOURCE_PATH_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(UNION_PROCEDURE_PATH_NAME, result);
                }
            }
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// saveUnionParam 합성용 파라메터 설정 메서드
        /// </summary>
        /// <param name="smooth_mls_knncnt">smoothing mls knn 갯수 (default:60) 0.1 ~ 60.0 </param>
        /// <param name="register_obj3d_Param">연속성을 가진 obj3d모델 등록 방법(default:"robust") "accurate", "robust"</param>
        /// <param name="triangulate_greedyKnnCnt"><para>피사체 3D Model을 구성하는 Point를 몇개를 이어 triangulate를 생성할 것인지에 대한 Knn 갯수 (default:40) int 0 ~ 100</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링</para>
        /// <para>값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링</para> </param>
        /// <param name="triangulate_greedyKnnRadiusParam">fixed: greedyKnnRadius를 고정하여 point 반경 산출, z_factor: z좌표에 greedyKnnRadiusValue를 곱하여 반경 산출(default:"z_factor") "auto", "fixed", "z_factor"</param>
        /// <param name="triangulate_greedyKnnRadiusValue"><para>피사체 3D Model을 구성하는 중심Point의 반경값(default:0.5) 0.15 ~ 5.0</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링</para>
        /// <para>값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링</para></param>
        /// <param name="triangulate_smallsurfaceremoveValue">3D Model의 작은 surface 제거 값 (default:0.0) 0.0 ~ 1000.0</param>
        /// <param name="triangulate_greedy_mesh_dilationValue">3D Model의 surface 팽창 값 (default:0) 0 ~ 3</param>
        /// <param name="sampling_method"><para>3D Image(Scene)를 샘플링하는 방법 (default:"fast_compute_normals") "accurate", "accurate_use_normals", "fast", "fast_compute_normals" </para>
        /// <para>normals가 추가된 옵션은 씬의 Point에 법선을 추가하여 씬을 구성하는 각 point들이 방향을 지니게 된다 법선이 추가되면 매칭 성공율이 증가.</para></param>
        /// <param name="sampling_distance">3D Image(Scene)를 샘플링하는 값 (default:0.5) 0.1 ~ 0.9</param>
        /// <param name="create_sfm_RelSampleDistance"><para>3D Model을 Surface Model로 변환할 때 Surface Model의 Point간 배치거리에 대한 퍼센테이지 (모델 직경(mm) * 값 = 점간 배치거리) (default:"0.03") 0.0 ~ 1.0</para>
        public void saveUnionParam(double smooth_mls_knncnt, string register_obj3d_Param, int triangulate_greedyKnnCnt, string triangulate_greedyKnnRadiusParam, double triangulate_greedyKnnRadiusValue, double triangulate_smallsurfaceremoveValue, int triangulate_greedy_mesh_dilationValue, string sampling_method, double sampling_distance, double create_sfm_RelSampleDistance)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            if (smooth_mls_knncnt > 0 && smooth_mls_knncnt < 100)
            {
                this.smooth_mls_knncnt = smooth_mls_knncnt;
            }
            if (register_obj3d_Param == "accurate" || register_obj3d_Param == "robust")
            {
                this.register_obj3d_Param = register_obj3d_Param;
            }

            //폴리곤 생성 피쳐
            if (triangulate_greedyKnnCnt > 0 && triangulate_greedyKnnCnt < 100)
            {
                this.triangulate_greedyKnnCnt = triangulate_greedyKnnCnt;
            }
            if (triangulate_greedyKnnRadiusParam == "auto" || triangulate_greedyKnnRadiusParam == "fixed" || triangulate_greedyKnnRadiusParam == "z_factor")
            {
                this.triangulate_greedyKnnRadiusParam = triangulate_greedyKnnRadiusParam;
            }
            if (triangulate_greedyKnnRadiusValue > 0 && triangulate_greedyKnnRadiusValue < 5)
            {
                this.triangulate_greedyKnnRadiusValue = triangulate_greedyKnnRadiusValue;
            }

            if (triangulate_greedyKnnRadiusParam == "auto" || triangulate_greedyKnnRadiusParam == "fixed")
            {
                if (!(triangulate_greedyKnnRadiusValue >= 0.15 && triangulate_greedyKnnRadiusValue < 5))
                {
                    this.triangulate_greedyKnnRadiusValue = 0.15;
                }
            }
            if (triangulate_smallsurfaceremoveValue >= 0 && triangulate_smallsurfaceremoveValue < 10000)
            {
                this.triangulate_smallsurfaceremoveValue = triangulate_smallsurfaceremoveValue;
            }
            if (triangulate_greedy_mesh_dilationValue >= 0 && triangulate_greedy_mesh_dilationValue < 4)
            {
                this.triangulate_greedy_mesh_dilationValue = triangulate_greedy_mesh_dilationValue;
            }


            //원본 이미지 샘플링
            if (sampling_method == "accurate" || sampling_method == "accurate_use_normals" ||
                sampling_method == "fast" || sampling_method == "fast_compute_normals")
            {
                this.sampling_method = sampling_method;
            }
            if (sampling_distance > 0.0 && sampling_distance < 1.0)
            {
                this.sampling_distance = sampling_distance;
            }

            //표면모델 샘플링
            if (create_sfm_RelSampleDistance > 0.0 && create_sfm_RelSampleDistance < 1.0)
            {
                this.create_sfm_RelSampleDistance = create_sfm_RelSampleDistance;
            }
            
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                "smooth_mls_knncnt " + smooth_mls_knncnt + '\n' +
                "register_obj3d_Param " + register_obj3d_Param + '\n' +
                "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                "sampling_method " + sampling_method + '\n' +
                "sampling_distance " + sampling_distance + '\n' +
                "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n');
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }


        /// <summary>
        /// ModelUnion 모델 생성 메서드
        /// 생성 과정중 UI에 HALCON 3D 모델링을 넣어줌
        /// output: surface 모델 파일(.sfm), 3d object 모델 파일 (.om3)
        /// </summary>
        /// <param name="extWin">HWindow 핸들</param>
        public void ModelUnion(HWindow extWin)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            if (thread != null)
            {
                thread.Interrupt();
            }
            MyEngine.SetProcedurePath(PATH_NAME);

            thread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    HDevWin = new HDevOpMultiWindowImpl(extWin);
                    MyEngine.SetHDevOperators(HDevWin);
                    Procedure = new HDevProcedure(UNION_PROCEDURE_NAME);
                    ProcCall = new HDevProcedureCall(Procedure);
                    ProcCall.SetInputCtrlParamTuple(1, extWin);
                    ProcCall.SetInputCtrlParamTuple(2, 1);
                    ProcCall.SetInputCtrlParamTuple(3, filesName);
                    ProcCall.SetInputCtrlParamTuple(4, smooth_mls_knncnt);
                    ProcCall.SetInputCtrlParamTuple(5, register_obj3d_Param);
                    ProcCall.SetInputCtrlParamTuple(6, triangulate_greedyKnnCnt);
                    ProcCall.SetInputCtrlParamTuple(7, triangulate_greedyKnnRadiusParam);
                    ProcCall.SetInputCtrlParamTuple(8, triangulate_greedyKnnRadiusValue);
                    ProcCall.SetInputCtrlParamTuple(9, triangulate_smallsurfaceremoveValue);
                    ProcCall.SetInputCtrlParamTuple(10, triangulate_greedy_mesh_dilationValue);
                    ProcCall.SetInputCtrlParamTuple(11, sampling_method);
                    ProcCall.SetInputCtrlParamTuple(12, sampling_distance);
                    ProcCall.SetInputCtrlParamTuple(13, create_sfm_RelSampleDistance);
                    string filename1Dim = "";
                    foreach(var fileN in filesName)
                    {
                        filename1Dim = filename1Dim + fileN + ", ";
                    }
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                        "PATH_NAME " + PATH_NAME + '\n' +
                        "UNION_PROCEDURE_NAME " + UNION_PROCEDURE_NAME + '\n' +
                        "filename1Dim " + filename1Dim + '\n' +
                        "smooth_mls_knncnt " + smooth_mls_knncnt + '\n' +
                        "register_obj3d_Param " + register_obj3d_Param + '\n' +
                        "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                        "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                        "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                        "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                        "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                        "sampling_method " + sampling_method + '\n' +
                        "sampling_distance " + sampling_distance + '\n' +
                        "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n');
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREREADY;
                    ProcCall.Execute();
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE);
                    RetOM3 = ProcCall.GetOutputCtrlParamTuple("RetOM3");
                    RetSFM = ProcCall.GetOutputCtrlParamTuple("RetSFM");
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELREADY;
                    mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELREADY);
                }
                catch (HDevEngineException Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
                }
                catch (Exception Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
                }
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
                HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            }));
            thread.Start();
        }

        /// <summary>
        /// ModelUnion 모델 생성 메서드
        /// output: surface 모델 파일(.sfm), 3d object 모델 파일 (.om3)
        /// </summary>
        public void ModelUnion()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            if (thread != null)
            {
                thread.Interrupt();
            }
            MyEngine.SetProcedurePath(PATH_NAME);
            HWindow extWin = new HWindow();
            thread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    HDevWin = new HDevOpMultiWindowImpl(extWin);
                    MyEngine.SetHDevOperators(HDevWin);
                    Procedure = new HDevProcedure(UNION_PROCEDURE_NAME);
                    ProcCall = new HDevProcedureCall(Procedure);
                    ProcCall.SetInputCtrlParamTuple(1, extWin);
                    ProcCall.SetInputCtrlParamTuple(2, 0);
                    ProcCall.SetInputCtrlParamTuple(3, filesName);
                    ProcCall.SetInputCtrlParamTuple(4, smooth_mls_knncnt);
                    ProcCall.SetInputCtrlParamTuple(5, register_obj3d_Param);
                    ProcCall.SetInputCtrlParamTuple(6, triangulate_greedyKnnCnt);
                    ProcCall.SetInputCtrlParamTuple(7, triangulate_greedyKnnRadiusParam);
                    ProcCall.SetInputCtrlParamTuple(8, triangulate_greedyKnnRadiusValue);
                    ProcCall.SetInputCtrlParamTuple(9, triangulate_smallsurfaceremoveValue);
                    ProcCall.SetInputCtrlParamTuple(10, triangulate_greedy_mesh_dilationValue);
                    ProcCall.SetInputCtrlParamTuple(11, sampling_method);
                    ProcCall.SetInputCtrlParamTuple(12, sampling_distance);
                    ProcCall.SetInputCtrlParamTuple(13, create_sfm_RelSampleDistance);

                    string filename1Dim = "";
                    foreach (var fileN in filesName)
                    {
                        filename1Dim = filename1Dim + fileN + ", ";
                    }
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                        "PATH_NAME " + PATH_NAME + '\n' +
                        "UNION_PROCEDURE_NAME " + UNION_PROCEDURE_NAME + '\n' +
                        "filename1Dim " + filename1Dim + '\n' +
                        "smooth_mls_knncnt " + smooth_mls_knncnt + '\n' +
                        "register_obj3d_Param " + register_obj3d_Param + '\n' +
                        "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                        "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                        "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                        "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                        "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                        "sampling_method " + sampling_method + '\n' +
                        "sampling_distance " + sampling_distance + '\n' +
                        "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n');
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREREADY;
                    ProcCall.Execute();
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE);
                    RetOM3 = ProcCall.GetOutputCtrlParamTuple("RetOM3");
                    RetSFM = ProcCall.GetOutputCtrlParamTuple("RetSFM");
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELREADY;
                    mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELREADY);
                }
                catch (HDevEngineException Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
                }
                catch (Exception Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                    HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
                }
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
                HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            }));
            thread.Start();
        }

        /// <summary>
        /// Create_OM3_SFM 합성 3D 모델 생성 메서드
        /// </summary>
        /// <param name="OM3FileName">OM3(할콘3D모델)파일이 생성될 디렉토리를 포함한 절대 경로</param>
        /// <param name="SFMFileName">SFM(Surface Mathing용 3D모델)파일이 생성될 디렉토리를 포함한 절대 경로</param>
        public void Create_OM3_SFM(string OM3FileName, string SFMFileName)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            try
            {
                HTuple GenParam = new HTuple();
                HTuple GenName = new HTuple();
                HOperatorSet.WriteObjectModel3d(RetOM3, "om3", OM3FileName, GenParam, GenName);
                HOperatorSet.WriteSurfaceModel(RetSFM, SFMFileName);
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                    "OM3FileName " + OM3FileName + '\n' +
                    "SFMFileName " + SFMFileName + '\n');
                HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELCREATE;
                mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELCREATE);
            }
            catch (HDevEngineException Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELERROR;
                mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELERROR);
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELERROR;
                mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELERROR);
            }
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
            HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
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
                if (thread != null)
                {
                    //Console.Write("");
                    HDevWin.DevClearWindow();
                    HDevWin.DevCloseWindow();
                    HDevWin.Dispose();
                    ProcCall.Dispose();
                    Procedure.Dispose();
                    //Console.Write("");

                    //MyEngine.UnloadProcedure("CreateSurfModel");
                    thread.Interrupt();
                    thread.Abort();

                    if (File.Exists(UNION_PROCEDURE_PATH_NAME))
                    {
                        File.Delete(UNION_PROCEDURE_PATH_NAME);
                    }
                }

                if (ProcCall != null)
                {
                    ProcCall.Dispose();

                    //MyEngine.UnloadProcedure("CreateSurfModel");


                }
                MyEngine.UnloadAllProcedures();
                MyEngine.Dispose();
                ProcCall = null;
                MyEngine = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(UNION_PROCEDURE_PATH_NAME))
                {
                    File.Delete(UNION_PROCEDURE_PATH_NAME);
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