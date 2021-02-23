

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
    /// libModelCreate Surface매칭용 3D모델 생성 Class
    /// </summary>
    public class libModelCreate
    {
        private Thread thread;
        private HDevEngine MyEngine;
        private HDevProcedureCall ProcCall;
        private HDevOpMultiWindowImpl HDevWin;
        private HDevProcedure Procedure;
        private string plyfileName;
        //private string ProcedurePath = "./";
        
        //private int BackgroundFeature = 0;
        private int SimpleObj = 0;

        private double BOXLengthX = 0;
        private double BOXLengthY = 0;
        private double BOXLengthZ = 0;
        private double SphereRadius = 0;
        private double CylinderRadius = 0;
        private double CylinderZMinExt = 0;
        private double CylinderZMaxExt = 0;
        private double minDepth = 0.0;
        private double maxDepth = 10.0;
        private int Background_Feature = 0;
        private int Smooth_Feature = 0;

        private int ModelForm = 0;
        
        private string sampling_method = "fast_compute_normals";
        private double sampling_distance = 0.9;

        private int triangulate_greedyKnnCnt = 60;
        private string triangulate_greedyKnnRadiusParam = "z_factor";
        private double triangulate_greedyKnnRadiusValue = 0.5;
        private double triangulate_smallsurfaceremoveValue = 0.0;
        private int triangulate_greedy_mesh_dilationValue = 0;

        private string connection_obj3d_Param = "distance_3d";
        private double connection_obj3d_value = 10;

        private int create_sfm_useInvertNormals = 0;
        private double create_sfm_RelSampleDistance = 0.03;

        private double CreateSurfModelTimeoutSec = 120;

        private double Simple_HalfCut = 0;

        private HTuple RetOM3;
        private HTuple RetSFM;

        //private int ExtWinOnOff = 0;

        private const string PATH_NAME = "./halconDev";
        private const string DUMMY_PROCEDURE_PATH_NAME = "./halconDev/DummyDisp.png";
        private const string DUMMY_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.DummyDisp.png";
        private const string DUMMY_PROCEDURE_NAME = "DummyDisp.png";
        

        private const string CREATE_PROCEDURE_PATH_NAME = "./halconDev/CreateSurfModel_0_0_8.hdvp";
        private const string CREATE_RESOURCE_PATH_NAME = "PicknPlaceHLib.Resource.CreateSurfModel_0_0_8.hdvp";
        private const string CREATE_PROCEDURE_NAME = "CreateSurfModel_0_0_8";
        private const string ClassName = "libModelCreate";

        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        /// <param name="hfunc">"p" = PROCEDURE OBJ, "m" = MODEL VAR</param>
        /// <param name="state">ENUM_HPROCEDURESTATE, ENUM_HMODELSTATE 값 콜백</param>
        public delegate void CBHalconState(string hfunc, int state);
        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        public event CBHalconState mCBHalconState;

        /// <summary>
        /// HPROCEDURE_STATE PROCEDURE 객체의 상태를 확인할 수 있는 Property 변수
        /// </summary>
        /// <value> ENUM_HPROCEDURESTATE 값을 SET하게 되어 있다</value>
        public int HPROCEDURE_STATE { get; private set; }

        /// <summary>
        /// HMODEL_STATE MODEL 변수의 상태를 확인할 수 있는 Property 변수
        /// </summary>
        /// <value> ENUM_HMODELSTATE값을 SET하게 되어있다</value>
        public int HMODEL_STATE { get; private set;} 
    
        
        /*private libModelCreate()
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            MyEngine = new HDevEngine();
            HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");

        }*/

        /// <summary>
        /// libModelCreate Class 생성자
        /// </summary>
        public libModelCreate()
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            //MyEngine = new HDevEngine();
            
            //this.sfm_om3_Name = sfm_om3_Name;
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
            if (File.Exists(CREATE_PROCEDURE_PATH_NAME))
            {
                File.Delete(CREATE_PROCEDURE_PATH_NAME);
            }
            if (!File.Exists(CREATE_PROCEDURE_PATH_NAME))
            {


                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = CREATE_RESOURCE_PATH_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(CREATE_PROCEDURE_PATH_NAME, result);
                }
            }
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// saveImageCreateModelParam 3DImage(Scene)에서 피사체를 추출하여 모델을 생성하는 파라메터를 정의하는 메서드 
        /// </summary>
        /// <param name="plyfileName">경로 포함한 .ply file명 </param>
        /// <param name="minDepth">배경 분리 셋팅 값: 지면 기준은 0.0이며, 0이상부터는 지면보다 높은데서 PCL형성(default:0.0) -N ~ N (카메라 센서 위치) </param>
        /// <param name="maxDepth">배경 분리 셋팅 값: 피사체의 PCL이 형성될 높이이며, 0.0 ~ N(카메라 센서 위치) </param>
        /// <param name="Background_Feature">(v0.04 default:0) *사용안하는 편이 나을듯...* 배경 분리에 관계적 알고리즘 사용(스무싱 필수) = 1, 지면을 기준으로 피사체까지의 z depth min과 max를 사용 = 0 (default:0) 0, 1</param>
        /// <param name="Smooth_Feature">(default:0),1 SurfaceModel 생성 전 Smoothing하여 PointCloud를 평준화한 모델링 생성</param>
        /// <param name="ModelForm"><para>PointModel = 0, EdgeModel = 1, Trianglulate(Polygon)Model = 2 (defalut:0) </para>
        /// <para>PointModel은 Point요소만을 가지고 매칭하며, 피사체가 복잡할 수록 매칭 성공률이 증가</para>
        /// <para>EdgeModel은 편평하고 단순한 형상의 피사체에 유리(시편, 자, 판 조각 등)</para>
        /// <para>Triangle Model은 Point요소에 Triangle(Polygon)을 입혀 매칭 성공률을 높힘 단순하고 입체적인 피사체일 수록 유리</para></param>
        /// <param name="sampling_method"><para>3D Image(Scene)를 샘플링하는 방법 (default:"fast_compute_normals") "accurate", "accurate_use_normals", "fast", "fast_compute_normals" </para>
        /// <para>normals가 추가된 옵션은 씬의 Point에 법선을 추가하여 씬을 구성하는 각 point들이 방향을 지니게 된다 법선이 추가되면 매칭 성공율이 증가.</para></param>
        /// <param name="sampling_distance">3D Image(Scene)를 샘플링하는 값 (default:0.5) 0.1 ~ 0.9</param>
        /// <param name="triangulate_greedyKnnCnt"><para>피사체 3D Model을 구성하는 Point를 몇개를 이어 triangulate를 생성할 것인지에 대한 Knn 갯수 (default:40) int 0 ~ 100</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링</para>
        /// <para>값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링</para> </param>
        /// <param name="triangulate_greedyKnnRadiusParam">fixed: greedyKnnRadius를 고정하여 point 반경 산출, z_factor: z좌표에 greedyKnnRadiusValue를 곱하여 반경 산출(default:"z_factor") "auto", "fixed", "z_factor"</param>
        /// <param name="triangulate_greedyKnnRadiusValue"><para>피사체 3D Model을 구성하는 중심Point의 반경값(default:0.5) 0.15 ~ 5.0</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링</para>
        /// <para>값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링</para></param>
        /// <param name="triangulate_smallsurfaceremoveValue">3D Model의 작은 surface 제거 값 (default:0.0) 0.0 ~ 1000.0</param>
        /// <param name="triangulate_greedy_mesh_dilationValue">3D Model의 surface 팽창 값 (default:0) 0 ~ 3</param>
        /// <param name="connection_obj3d_Param">(v0.04 BackgroundFeature연동 값으로 미사용 default:"") 배경분리 피쳐 (default:"distance_3d") "distance_3d", "mesh"</param>
        /// <param name="connection_obj3d_value">(v0.04 v0.04 BackgroundFeature연동 값으로 미사용 default:"0")배경분리 값 (default:"10") 0.0 ~ 100.0</param>
        /// <param name="create_sfm_useInvertNormals">SurfaceModel의 KeyPoint normals방향 반전(default:0)0: 방향처리 안함, 1: 방향 반전</param>
        /// <param name="create_sfm_RelSampleDistance"><para>3D Model을 Surface Model로 변환할 때 Surface Model의 Point간 배치거리에 대한 퍼센테이지 (모델 직경(mm) * 값 = 점간 배치거리) (default:"0.03") 0.0 ~ 1.0</para>
        /// <para>값이 클 경우 SurfaceModel의 Point간의 배치 거리가 넓어져 정교함이 낮은 피사체 모델이 생성</para>
        /// <para>값이 작을 경우 SurfaceModel의 Point간의 배치 거리가 좁아져 정교함이 높은 피사체 모델이 생성</para></param>
        /// <param name="CreateSurfModelTimeoutSec">Surface Model 변환 타임아웃 시간 단위 "초"</param>
        public void saveImageCreateModelParam(string plyfileName, double minDepth, double maxDepth, int Background_Feature, int Smooth_Feature,int ModelForm, string sampling_method, double sampling_distance, int triangulate_greedyKnnCnt, string triangulate_greedyKnnRadiusParam, double triangulate_greedyKnnRadiusValue, double triangulate_smallsurfaceremoveValue, int triangulate_greedy_mesh_dilationValue, string connection_obj3d_Param, double connection_obj3d_value, int create_sfm_useInvertNormals, double create_sfm_RelSampleDistance, double CreateSurfModelTimeoutSec)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            
            this.plyfileName = plyfileName;

            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            if(Background_Feature == 0 || Background_Feature == 1) { 
                this.Background_Feature = Background_Feature;
            }
            if(Smooth_Feature == 0 || Smooth_Feature == 1)
            {
                this.Smooth_Feature = Smooth_Feature;
            }
            if (ModelForm == 0 || ModelForm == 1 || ModelForm == 2)
            {
                this.ModelForm = ModelForm;
            }
            // 'accurate', 'accurate_use_normals', 'fast', 'fast_compute_normals'
            //원본 이미지 샘플링
            if (sampling_method == "accurate" || sampling_method == "accurate_use_normals" ||
                sampling_method == "fast" || sampling_method == "fast_compute_normals")
            {
                this.sampling_method = sampling_method;
            }
            if (sampling_distance > 0.0 && sampling_distance < 10.0)
            {
                this.sampling_distance = sampling_distance;
            }

            //폴리곤 생성 피쳐
            if (triangulate_greedyKnnCnt >= 0 && triangulate_greedyKnnCnt < 100)
            {
                this.triangulate_greedyKnnCnt = triangulate_greedyKnnCnt;
            }
            if (triangulate_greedyKnnRadiusParam == "auto" || triangulate_greedyKnnRadiusParam == "fixed" || triangulate_greedyKnnRadiusParam == "z_factor")
            {
                this.triangulate_greedyKnnRadiusParam = triangulate_greedyKnnRadiusParam;
            }
            if (triangulate_greedyKnnRadiusValue >= 0 && triangulate_greedyKnnRadiusValue < 5)
            {
                this.triangulate_greedyKnnRadiusValue = triangulate_greedyKnnRadiusValue;
            }
            if (triangulate_greedy_mesh_dilationValue >= 0 && triangulate_greedy_mesh_dilationValue < 4)
            {
                this.triangulate_greedy_mesh_dilationValue = triangulate_greedy_mesh_dilationValue;
            }
            
            if (triangulate_smallsurfaceremoveValue >= 0.0 && triangulate_smallsurfaceremoveValue < 1000)
            {
                this.triangulate_smallsurfaceremoveValue = triangulate_smallsurfaceremoveValue;
            }

            if(connection_obj3d_Param == "distance_3d" || connection_obj3d_Param == "mesh")
            {
                this.connection_obj3d_Param = connection_obj3d_Param;
            }

            if (connection_obj3d_value > 0.0 && connection_obj3d_value < 100.0)
            {
                this.connection_obj3d_value = connection_obj3d_value;
            }
            if (create_sfm_useInvertNormals == 0 || create_sfm_useInvertNormals == 1)
            {
                this.create_sfm_useInvertNormals = create_sfm_useInvertNormals;
            }
            if (create_sfm_RelSampleDistance >= 0.0 && create_sfm_RelSampleDistance <= 1.0)
            {
                this.create_sfm_RelSampleDistance = create_sfm_RelSampleDistance;
            }
            if (CreateSurfModelTimeoutSec >= 0.0 && CreateSurfModelTimeoutSec <= 600)
            {
                this.CreateSurfModelTimeoutSec = CreateSurfModelTimeoutSec;
            }
            this.Simple_HalfCut = 0;


            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                "plyfileName " + plyfileName + '\n' +
                "minDepth " + minDepth + '\n' +
                "maxDepth " + maxDepth + '\n' +
                "Background_Feature " + Background_Feature + '\n' +
                "ModelForm " + ModelForm + '\n' +
                "sampling_method " + sampling_method + '\n' +
                "sampling_distance " + sampling_distance + '\n' +
                "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                "connection_obj3d_Param " + connection_obj3d_Param + '\n' +
                "connection_obj3d_value " + connection_obj3d_value + '\n' +
                "create_sfm_useInvertNormals " + create_sfm_useInvertNormals + '\n' +
                "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n' +
                "CreateSurfModelTimeoutSec " + CreateSurfModelTimeoutSec + '\n');
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// saveSimpleCreateModelParam SimpleObject 3D Model을 생성하는 메서드
        /// </summary>
        /// <param name="SimpleObj">0:박스 모델 생성, 1:구체 모델 생성, 2: 원통 모델 생성</param>
        /// <param name="BOXLengthX">SimpleObj = 0 일 경우, 박스의 X길이(mm) </param>
        /// <param name="BOXLengthY">SimpleObj = 0 일 경우, 박스의 Y길이(mm)</param>
        /// <param name="BOXLengthZ">SimpleObj = 0 일 경우, 박스의 Z길이(mm)</param>
        /// <param name="SphereRadius">SimpleObj = 1 일 경우, 구체의 반지름(mm)</param>
        /// <param name="CylinderRadius">SimpleObj = 2 일 경우, 원통의 반지름(mm)</param>
        /// <param name="CylinderZMinExt">SimpleOb = 2 일 경우, 높이의 중간 0을 기준으로 원통 하판 음의 위치(mm)</param>
        /// <param name="CylinderZMaxExt">SimpleObj = 2 일 경우, 높이의 중간 0을 기준으로 원통 상판 양의 위치(mm)</param>
        /// <param name="ModelForm"><para>PointModel = 0, EdgeModel = 1, Triangle(Polygon)Model = 2 (defalut:0)</para> 
        /// <para>PointModel은 Point요소만을 가지고 매칭하며, 피사체가 복잡할 수록 매칭 성공률이 증가</para>
        /// <para>EdgeModel은 편평하고 단순한 형상의 피사체에 유리(시편, 자, 판 조각 등)</para>
        /// <para>Triangle Model은 Point요소에 Triangle(Polygon)을 입혀 매칭 성공률을 높힘 단순하고 입체적인 피사체일 수록 유리</para></param>
        /// <param name="sampling_method"><para>3D Image(Scene)를 샘플링하는 방법 (default:"fast_compute_normals") "accurate", "accurate_use_normals", "fast", "fast_compute_normals" </para>
        /// <para>normals가 추가된 옵션은 씬의 Point에 법선을 추가하여 씬을 구성하는 각 point들이 방향을 지니게 된다 법선이 추가되면 매칭 성공율이 증가.</para></param>
        /// <param name="sampling_distance">3D Image(Scene)를 샘플링하는 값 (default:0.5) 0.1 ~ 0.9</param>
        /// <param name="triangulate_greedyKnnCnt"><para>피사체 3D Model을 구성하는 Point를 몇개를 이어 triangulate를 생성할 것인지에 대한 Knn 갯수 (default:40) int 0 ~ 100</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링 되며, 값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링 됨</para></param>
        /// <param name="triangulate_greedyKnnRadiusParam">fixed: greedyKnnRadius를 고정하여 point 반경 산출, z_factor: z좌표에 greedyKnnRadiusValue곱하여 반경 산출(default:"z_factor") "auto", "fixed", "z_factor"</param>
        /// <param name="triangulate_greedyKnnRadiusValue"><para>피사체 3D Model을 구성하는 중심Point의 반경값(default:0.5) 0.15 ~ 5.0</para>
        /// <para>값이 클 경우 단순한 triangulate를 갖는 피사체가 모델링</para>
        /// <para>값이 작을 경우 복잡한 triangulate를 갖게되는 피사체가 모델링 됨</para></param>
        /// <param name="triangulate_smallsurfaceremoveValue">3D Model의 작은 surface 제거 값 (default:0.0) 0.0 ~ 1000.0</param>
        /// <param name="triangulate_greedy_mesh_dilationValue">3D Model의 surface 팽창 값 (default:0) 0 ~ 3</param>
        /// <param name="create_sfm_useInvertNormals">SurfaceModel의 KeyPoint normals방향 반전(default:0) 0: 방향처리 안함, 1: 방향 반전</param>
        /// <param name="create_sfm_RelSampleDistance"><para>3D Model을 Surface Model로 변환할 때 Surface Model의 Point간 배치거리에 대한 퍼센테이지 (모델 직경(mm) * 값 = 점간 배치거리) (default:"0.03") 0.0 ~ 1.0</para>
        /// <para>값이 클 경우 SurfaceModel의 Point간의 배치 거리가 넓어져 정교함이 낮은 피사체 모델이 생성</para>
        /// <para>값이 작을 경우 SurfaceModel의 Point간의 배치 거리가 좁아져 정교함이 높은 피사체 모델이 생성</para></param>
        /// <param name="CreateSurfModelTimeoutSec">Surface Model 변환 타임아웃 시간 단위 "초"</param>
        /// <param name="Simple_HalfCut">Surface Model 생성전 SIMPLE OBJ3D의 탑뷰를 기준으로 Z축 중앙부터 뒷편의 포인트 제거</param>
        public void saveSimpleCreateModelParam(int SimpleObj, double BOXLengthX, double BOXLengthY, double BOXLengthZ, double SphereRadius, double CylinderRadius, double CylinderZMinExt, double CylinderZMaxExt, int ModelForm, string sampling_method, double sampling_distance, int triangulate_greedyKnnCnt, string triangulate_greedyKnnRadiusParam, double triangulate_greedyKnnRadiusValue, double triangulate_smallsurfaceremoveValue, int triangulate_greedy_mesh_dilationValue, int create_sfm_useInvertNormals, double create_sfm_RelSampleDistance, double CreateSurfModelTimeoutSec, int Simple_HalfCut)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            this.plyfileName = "";
            if (SimpleObj == 0)
            {
                this.SimpleObj = SimpleObj;
                this.BOXLengthX = BOXLengthX;
                this.BOXLengthY = BOXLengthY;
                this.BOXLengthZ = BOXLengthZ;
                this.SphereRadius = 0;
                this.CylinderRadius = 0;
                this.CylinderZMinExt = 0;
                this.CylinderZMaxExt = 0;
            }
            if (SimpleObj == 1)
            {
                this.SimpleObj = SimpleObj;
                this.BOXLengthX = 0;
                this.BOXLengthY = 0;
                this.BOXLengthZ = 0;
                this.SphereRadius = SphereRadius;
                this.CylinderRadius = 0;
                this.CylinderZMinExt = 0;
                this.CylinderZMaxExt = 0;
            }
            if (SimpleObj == 2)
            {
                this.SimpleObj = SimpleObj;
                this.BOXLengthX = 0;
                this.BOXLengthY = 0;
                this.BOXLengthZ = 0;
                this.SphereRadius = 0;
                this.CylinderRadius = CylinderRadius;
                this.CylinderZMinExt = CylinderZMinExt;
                this.CylinderZMaxExt = CylinderZMaxExt;
            }
            
            this.minDepth = 0;
            this.maxDepth = 0;
            

            if (ModelForm == 0 || ModelForm == 1 || ModelForm == 2)
            {
                this.ModelForm = ModelForm;
            }
            // 'accurate', 'accurate_use_normals', 'fast', 'fast_compute_normals'
            //원본 이미지 샘플링
            if (sampling_method == "accurate" || sampling_method == "accurate_use_normals" ||
                sampling_method == "fast" || sampling_method == "fast_compute_normals")
            {
                this.sampling_method = sampling_method;
            }
            if (sampling_distance > 0.0 && sampling_distance < 10.0)
            {
                this.sampling_distance = sampling_distance;
            }

            //폴리곤 생성 피쳐
            if (triangulate_greedyKnnCnt >= 0 && triangulate_greedyKnnCnt < 100)
            {
                this.triangulate_greedyKnnCnt = triangulate_greedyKnnCnt;
            }
            if (triangulate_greedyKnnRadiusParam == "auto" || triangulate_greedyKnnRadiusParam == "fixed" || triangulate_greedyKnnRadiusParam == "z_factor")
            {
                this.triangulate_greedyKnnRadiusParam = triangulate_greedyKnnRadiusParam;
            }
            if (triangulate_greedyKnnRadiusValue >= 0 && triangulate_greedyKnnRadiusValue < 5)
            {
                this.triangulate_greedyKnnRadiusValue = triangulate_greedyKnnRadiusValue;
            }
            if (triangulate_greedy_mesh_dilationValue >= 0 && triangulate_greedy_mesh_dilationValue < 4)
            {
                this.triangulate_greedy_mesh_dilationValue = triangulate_greedy_mesh_dilationValue;
            }

            if (triangulate_smallsurfaceremoveValue >= 0.0 && triangulate_smallsurfaceremoveValue < 1000)
            {
                this.triangulate_smallsurfaceremoveValue = triangulate_smallsurfaceremoveValue;
            }
            if (create_sfm_useInvertNormals == 0 || create_sfm_useInvertNormals == 1)
            {
                this.create_sfm_useInvertNormals = create_sfm_useInvertNormals;
            }
            if (create_sfm_RelSampleDistance >= 0.0 && create_sfm_RelSampleDistance <= 1.0)
            {
                this.create_sfm_RelSampleDistance = create_sfm_RelSampleDistance;
            }
            if (CreateSurfModelTimeoutSec >= 0.0 && CreateSurfModelTimeoutSec <= 600)
            {
                this.CreateSurfModelTimeoutSec = CreateSurfModelTimeoutSec;
            }
            this.Simple_HalfCut = 0;
            if (Simple_HalfCut == 0 || Simple_HalfCut == 1)
            {
                this.Simple_HalfCut = Simple_HalfCut;
            }

            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                "SimpleObj " + SimpleObj + '\n' +
                "BOXLengthX " + BOXLengthX + '\n' +
                "BOXLengthY " + BOXLengthY + '\n' +
                "BOXLengthZ " + BOXLengthZ + '\n' +
                "SphereRadius " + SphereRadius + '\n' +
                "CylinderRadius " + CylinderRadius + '\n' +
                "CylinderZMinExt " + CylinderZMinExt + '\n' +
                "CylinderZMaxExt " + CylinderZMaxExt + '\n' +
                "ModelForm " + ModelForm + '\n' +
                "sampling_method " + sampling_method + '\n' +
                "sampling_distance " + sampling_distance + '\n' +
                "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                "create_sfm_useInvertNormals " + create_sfm_useInvertNormals + '\n' +
                "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n' +
                "CreateSurfModelTimeoutSec " + CreateSurfModelTimeoutSec + '\n' +
                "Simple_HalfCut " + Simple_HalfCut + '\n');
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }


        /// <summary>
        /// ModelCreate 모델 생성 메서드
        /// 생성 과정중 UI에 HALCON 3D 모델링을 넣어줌
        /// </summary>
        /// <param name="extWin">HWindow 핸들</param>
        public void ModelCreate(HWindow extWin)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            try
            {
                if (thread != null)
                {
                    if (HDevWin != null)
                    {
                        HDevWin.Dispose();
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
                    //thread.Interrupt();
                    thread.Abort();
                    thread = null;
                }
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
            }
            MyEngine = new HDevEngine();
            MyEngine.SetProcedurePath(PATH_NAME);

            thread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    //HTuple WindowHandle;
                    //HOperatorSet.OpenWindow(0, 0, X, Y, extWin, "visible", "", out WindowHandle);

                    HDevWin = new HDevOpMultiWindowImpl(extWin);
                    MyEngine.SetHDevOperators(HDevWin);
                    Procedure = new HDevProcedure(CREATE_PROCEDURE_NAME);
                    ProcCall = new HDevProcedureCall(Procedure);

                    ProcCall.SetInputCtrlParamTuple(1, extWin);
                    ProcCall.SetInputCtrlParamTuple(2, 1);
                    ProcCall.SetInputCtrlParamTuple(3, plyfileName);
                    if (plyfileName.Length <= 1)
                    {
                        ProcCall.SetInputCtrlParamTuple(4, SimpleObj);
                        ProcCall.SetInputCtrlParamTuple(5, BOXLengthX);
                        ProcCall.SetInputCtrlParamTuple(6, BOXLengthY);
                        ProcCall.SetInputCtrlParamTuple(7, BOXLengthZ);
                        ProcCall.SetInputCtrlParamTuple(8, SphereRadius);
                        ProcCall.SetInputCtrlParamTuple(9, CylinderRadius);
                        ProcCall.SetInputCtrlParamTuple(10, CylinderZMinExt);
                        ProcCall.SetInputCtrlParamTuple(11, CylinderZMaxExt);
                    }
                    else
                    {
                        ProcCall.SetInputCtrlParamTuple(4, 0);
                        ProcCall.SetInputCtrlParamTuple(5, 0);
                        ProcCall.SetInputCtrlParamTuple(6, 0);
                        ProcCall.SetInputCtrlParamTuple(7, 0);
                        ProcCall.SetInputCtrlParamTuple(8, 0);
                        ProcCall.SetInputCtrlParamTuple(9, 0);
                        ProcCall.SetInputCtrlParamTuple(10, 0);
                        ProcCall.SetInputCtrlParamTuple(11, 0);
                    }
                    ProcCall.SetInputCtrlParamTuple(12, minDepth);
                    ProcCall.SetInputCtrlParamTuple(13, maxDepth);
                    ProcCall.SetInputCtrlParamTuple(14, Background_Feature);
                    ProcCall.SetInputCtrlParamTuple(15, Smooth_Feature);
                    ProcCall.SetInputCtrlParamTuple(16, ModelForm);
                    ProcCall.SetInputCtrlParamTuple(17, sampling_method);
                    ProcCall.SetInputCtrlParamTuple(18, sampling_distance);
                    ProcCall.SetInputCtrlParamTuple(19, triangulate_greedyKnnCnt);
                    ProcCall.SetInputCtrlParamTuple(20, triangulate_greedyKnnRadiusParam);
                    ProcCall.SetInputCtrlParamTuple(21, triangulate_greedyKnnRadiusValue);
                    ProcCall.SetInputCtrlParamTuple(22, triangulate_smallsurfaceremoveValue);
                    ProcCall.SetInputCtrlParamTuple(23, triangulate_greedy_mesh_dilationValue);
                    ProcCall.SetInputCtrlParamTuple(24, connection_obj3d_Param);
                    ProcCall.SetInputCtrlParamTuple(25, connection_obj3d_value);
                    ProcCall.SetInputCtrlParamTuple(26, create_sfm_useInvertNormals);
                    ProcCall.SetInputCtrlParamTuple(27, create_sfm_RelSampleDistance);
                    ProcCall.SetInputCtrlParamTuple(28, CreateSurfModelTimeoutSec);
                    ProcCall.SetInputCtrlParamTuple(29, Simple_HalfCut);

                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                        "PATH_NAME " + PATH_NAME + '\n' +
                        "CREATE_PROCEDURE_NAME " + CREATE_PROCEDURE_NAME + '\n' +
                        "plyfileName " + plyfileName + '\n' +
                        "SimpleObj " + SimpleObj + '\n' +
                        "BOXLengthX " + BOXLengthX + '\n' +
                        "BOXLengthY " + BOXLengthY + '\n' +
                        "BOXLengthZ " + BOXLengthZ + '\n' +
                        "SphereRadius " + SphereRadius + '\n' +
                        "CylinderRadius " + CylinderRadius + '\n' +
                        "CylinderZMinExt " + CylinderZMinExt + '\n' +
                        "CylinderZMaxExt " + CylinderZMaxExt + '\n' +
                        "minDepth " + minDepth + '\n' +
                        "maxDepth " + maxDepth + '\n' +
                        "Background_Feature " + Background_Feature + '\n' +
                        "Smooth_Feature " + Smooth_Feature + '\n' +
                        "ModelForm " + ModelForm + '\n' +
                        "sampling_method " + sampling_method + '\n' +
                        "sampling_distance " + sampling_distance + '\n' +
                        "triangulate_greedyKnnCnt " + triangulate_greedyKnnCnt + '\n' +
                        "triangulate_greedyKnnRadiusParam " + triangulate_greedyKnnRadiusParam + '\n' +
                        "triangulate_greedyKnnRadiusValue " + triangulate_greedyKnnRadiusValue + '\n' +
                        "triangulate_smallsurfaceremoveValue " + triangulate_smallsurfaceremoveValue + '\n' +
                        "triangulate_greedy_mesh_dilationValue " + triangulate_greedy_mesh_dilationValue + '\n' +
                        "connection_obj3d_Param " + connection_obj3d_Param + '\n' +
                        "connection_obj3d_value " + connection_obj3d_value + '\n' +
                        "create_sfm_useInvertNormals " + create_sfm_useInvertNormals + '\n' +
                        "create_sfm_RelSampleDistance " + create_sfm_RelSampleDistance + '\n' +
                        "CreateSurfModelTimeoutSec " + CreateSurfModelTimeoutSec + '\n' +
                        "Simple_HalfCut " + Simple_HalfCut + '\n');
                    


                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREREADY;
                    ProcCall.Execute();
                    string CreateResult = ProcCall.GetOutputCtrlParamTuple("CreateResult").S;
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "CreateResult= " + CreateResult);
                    if (CreateResult == "Done")
                    {
                        HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE;
                        mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE);
                        RetOM3 = ProcCall.GetOutputCtrlParamTuple("RetOM3");
                        RetSFM = ProcCall.GetOutputCtrlParamTuple("RetSFM");
                        HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELREADY;
                        mCBHalconState?.Invoke("m", (int)ENUM_HMODELSTATE.MODELREADY);
                    }
                    else
                    {
                        HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                        mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                        HMODEL_STATE = (int)ENUM_HMODELSTATE.MODELNONE;
                    }
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
        /// Create_OM3_SFM 3D 모델파일 생성 메서드
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
            try {
                if (thread.IsAlive)
                {
                    if (HDevWin != null)
                    {
                        HDevWin.Dispose();
                    }
                    if (ProcCall != null)
                    {
                        ProcCall.Dispose();
                    }
                    if(Procedure != null)
                    {
                        Procedure.Dispose();
                    }
                    if(MyEngine != null)
                    {
                        MyEngine.Dispose();
                    }

                    //MyEngine.UnloadProcedure("CreateSurfModel");
                    thread.Abort();
                    thread = null;
                    
                    if (File.Exists(CREATE_PROCEDURE_PATH_NAME))
                    {
                        File.Delete(CREATE_PROCEDURE_PATH_NAME);
                    }
                }
                MyEngine.UnloadAllProcedures();
                MyEngine.Dispose();
                HDevWin = null;
                ProcCall = null;
                Procedure = null;
                MyEngine = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                if (File.Exists(CREATE_PROCEDURE_PATH_NAME))
                {
                    File.Delete(CREATE_PROCEDURE_PATH_NAME);
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
