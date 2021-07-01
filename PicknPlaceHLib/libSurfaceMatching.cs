

namespace PicknPlaceHLib {
    extern alias dest;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using System.Threading;
    using dest.HalconDotNet;
    using System.Reflection;
    using System.Resources;
    using System.Globalization;
    using System.IO;
    using System.Diagnostics;

    /// <summary>
    /// libSurfaceMatching Class 
    /// 3d surface matching을 하고 6축 좌표를 리턴 
    /// 필요시 2d, 3d halcon ui에 이미지를 넣어줌
    /// </summary>
    public class libSurfaceMatching
    {
        private HDevEngine MyEngine;
        private HDevProcedureCall ProcCall;
        private HDevProcedure Procedure;
        private HDevOpMultiWindowImpl HDevWin;
        private string plyfileName;
        private string[] sfmfileName;
        private string[] om3fileName;
        //private string ProcedurePath = "./";
        //private string ProcedurePath = "./";


        private HImage RGB_Scene;
        //private HTuple Ret3D;
        private HTuple ObjectModel_Scene;
        private HTuple ObjectModel3D_Result;
        private HTuple ObjectModel3D_ResultArrow;
        private HTuple ObjectModel3D_ResultRoI;
        private HWindow extWin;

        private int RoiForm = 1;
        private int MatchForm = 1;
        private int ScanXArea = 100;
        private int ScanXOverwrap = 50;
        private double ROIXAreaMin = 0.0;
        private double ROIXAreaMax = 0.0;
        private double ROIYAreaMin = 0.0;
        private double ROIYAreaMax = 0.0;
        private double ROIZPlaneMinDepth = 0.0;
        private double ROIZPlaneMaxDepth = 10;

        private string sampling_method = "fast";
        private double sampling_distance = 0.5;

        private double[] find_sfm_RelSamplingDistance;// = 0.1;
        private double[] find_sfm_KeyPointFraction;// = 0.2;
        private double[] find_sfm_MinScore;// = 0.35;
        private int[] find_sfm_NumMatch;// = 1;
        private string find_sfm_FindMethod = "mls";
        private string find_sfm_ScoreType = "model_point_fraction";
        private double find_sfm_max_overlap_dist_value = 0.5;
        private int AxisAlign = 0;
        private string find_sfm_max_overlap_dist_type = "max_overlap_dist_rel";
        private string find_sfm_pose_ref_use_scene_normals_value = "false";
        private int find_sfm_pose_ref_num_steps_value = 5;
        private int find_sfm_pose_ref_sub_sampling_value = 2;
        private int PickLimitDegree = 40;
        private int FindSurfModelTimeoutSec = 60;
        private double AutoRoiNOWPointN_BeforePointNDIFF = 11;
        private int MultiModelOverlapMatchingMode = 0;
        private double MultiModelOverlapMargin = 11;

        private Thread thread;

        private const string PATH_NAME = "./halconDev";
        private const string SURF_RESOURCE_NAME = "PicknPlaceHLib.Resource.SurfMatch_FileHandle_0_1_6.hdvp";
        private const string SURF_PROCEDURE_NAME = "./halconDev/SurfMatch_FileHandle_0_1_6.hdvp";
        private const string SURF_PROCEDURE_STR = "SurfMatch_FileHandle_0_1_6";
        private const string VIS3D_RESOURCE_NAME = "PicknPlaceHLib.Resource.Halcon3DVis_0_0_3.hdvp";
        private const string VIS3D_PROCEDURE_NAME = "./halconDev/Halcon3DVis_0_0_3.hdvp";
        private const string VIS3D_PROCEDURE_STR = "Halcon3DVis_0_0_3";
        private const string ROTSTATE_RESOURCE_NAME = "PicknPlaceHLib.Resource.RotateState_0_0_1.hdvp";
        private const string ROTSTATE_PROCEDURE_NAME = "./halconDev/RotateState_0_0_1.hdvp";
        private const string ROTSTATE_PROCEDURE_STR = "RotateState_0_0_1";
        private const string HCAMPAR_RESOURCE_NAME = "PicknPlaceHLib.Resource.HCamParam.dat";
        private const string HCAMPAR_FILE_NAME = "./halconDev/HCamParam.dat";
        private const string HCAMPAR_FILE_STR = "HCamParam";
        private const string ClassName = "libSurfaceMatching";


        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        /// <value>이벤트 콜백</value>
        /// <param name = "hfunc">"p" = PROCEDURE OBJ</param>
        /// <param name = "state">ENUM_HPROCEDURESTATE 값 콜백</param>
        public delegate void CBHalconState(string hfunc, int state);

        /// <summary>
        /// CBHalconState 프로시저객체와, 모델 변수의 상태를 알려주는 이벤트 핸들링
        /// </summary>
        /// <value>이벤트 콜백</value>
        public event CBHalconState mCBHalconState;

        /// <summary>
        /// HPROCEDURE_STATE PROCEDURE 객체의 상태를 확인할 수 있는 Property 변수
        /// </summary>
        /// <value> ENUM_HPROCEDURESTATE 값을 SET하게 되어 있다</value>
        public int HPROCEDURE_STATE { get; private set; }
        
        /// <summary>
        /// libSurfaceMatching 생성자
        /// </summary>
        public libSurfaceMatching()
        {
            var currentMethodName = ClassName;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "Start");
            if (!Directory.Exists(PATH_NAME))
            {
                Directory.CreateDirectory(PATH_NAME);
            }
            if (File.Exists(SURF_PROCEDURE_NAME))
            {
                File.Delete(SURF_PROCEDURE_NAME);
            }
            if (!File.Exists(SURF_PROCEDURE_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = SURF_RESOURCE_NAME;


                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(SURF_PROCEDURE_NAME, result);
                }
            }
            if (File.Exists(VIS3D_PROCEDURE_NAME))
            {
                File.Delete(VIS3D_PROCEDURE_NAME);
            }
            if (!File.Exists(VIS3D_PROCEDURE_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = VIS3D_RESOURCE_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(VIS3D_PROCEDURE_NAME, result);
                }
            }

            if (File.Exists(ROTSTATE_PROCEDURE_NAME))
            {
                File.Delete(ROTSTATE_PROCEDURE_NAME);
            }
            if (!File.Exists(ROTSTATE_PROCEDURE_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = ROTSTATE_RESOURCE_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(ROTSTATE_PROCEDURE_NAME, result);
                }
            }

            if (File.Exists(HCAMPAR_FILE_NAME))
            {
                File.Delete(HCAMPAR_FILE_NAME);
            }
            if (!File.Exists(HCAMPAR_FILE_NAME))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = HCAMPAR_RESOURCE_NAME;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText(HCAMPAR_FILE_NAME, result);
                }
            }
            //FileStream outputFileStream = new FileStream("./SurfMatch_FileHandle.hdvp", FileMode.Create);
            //var sr = new StreamReader(stream);
            //stream.CopyTo(outputFileStream);

            //MyEngine.SetEngineAttribute("debug_port", 54545);
            //MyEngine.SetEngineAttribute("debug_password", "gc");
            //MyEngine.StartDebugServer();
            HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }
        /*
        public libSurfaceMatching_FileHandle(string sfm_om3_Name)
        {
            MyEngine = new HDevEngine();
            //this.plyfileName = plyfileName;
            this.sfm_om3_Name = sfm_om3_Name;
        }*/

        /// <summary>
        /// saveSceneParam 
        /// 매칭 전, 씬에 대한 처리 방법과 연산 속도와 정확도를 위해 어느 정도로 샘플링 할지 셋팅하는 메서드
        /// </summary>
        /// <param name="RoiForm">0:미사용, 1:Moving ROI, 2:Static ROI, 3:Auto ROI (default:"0")</param>
        /// <param name="MatchForm">0:SurfaceMatch, 1:SurfaceMatch with EDGE, 2:SurfaceMatch with Triangulate(default:"0") </param>
        /// <param name="ScanXArea">(ROI_USE = 1일 경우 기입)WIDTH 탐색 범위 0 ~ N </param>
        /// <param name="ScanXOverwrap">(ROI_USE = 1일 경우 기입) ScanXArea보다 크거나 같을 경우 MOVING ROI탐색시 겹치는 영역없음, 작을 경우 겹치는 영역 발생</param>
        /// <param name="ROIXAreaMin">(ROI_USE = 1 또는 2일 경우 기입)ZIVID FOV WIDTH 중간값 0을 기준으로 왼쪽 음의 값 </param>
        /// <param name="ROIXAreaMax">(ROI_USE = 1 또는 2일 경우 기입)ZIVID FOV WIDTH 중간값 0을 기준으로 오른쪽 양의 값 </param>
        /// <param name="ROIYAreaMin">(ROI_USE = 2일 경우 기입)ZIVID FOV HEIGHT 중간값 0을 기준으로 왼쪽 음의 값 </param>
        /// <param name="ROIYAreaMax">(ROI_USE = 2일 경우 기입)ZIVID FOV HEIGHT 중간값 0을 기준으로 오른쪽 양의 값 </param>
        /// <param name="ROIZPlaneMinDepth">(ROI_USE = 1 또는 2일 경우, ROI 사용시)작업대 지면(0)을 기준으로 Z축 ROI 최소 범위값</param>
        /// <param name="ROIZPlaneMaxDepth">(ROI_USE = 1 또는 2일 경우, ROI 사용시)ZPlaneMinDepth를 기준으로 Z축 ROI 최대 범위값</param>
        /// <param name="sampling_method">샘플링 도구 (default:"fast") "accurate", "accurate_use_normals", "fast", "fast_compute_normals" </param>
        /// <param name="sampling_distance">샘플 값 (default:0.5) 0.1 ~ 0.9</param>
        /// <param name="AutoRoiNOWPointN_BeforePointNDIFF"><para>Auto ROI사용(RoiForm=3)- XYZ PointCloud 중 Z를 1단위로 잘라 이전, 현재 X,Y상에 존재하는 Point갯수에 대한 차 값</para> 
        /// <para> 작을 수록 지면을 정교하게 제거, 클수록 피사체와 지면이 포함</para></param>
        public void saveSceneParam(int RoiForm, int MatchForm, int ScanXArea, int ScanXOverwrap, int ROIXAreaMin, int ROIXAreaMax, int ROIYAreaMin, int ROIYAreaMax, double ROIZPlaneMinDepth, double ROIZPlaneMaxDepth, string sampling_method, double sampling_distance, double AutoRoiNOWPointN_BeforePointNDIFF)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");


            //SurfaceMatch with ROI form
            if (RoiForm == 0 || RoiForm == 1 || RoiForm == 2 || RoiForm == 3)
            {
                this.RoiForm = RoiForm;
            }
            //SurfaceMatch with match form;
            if (MatchForm == 0 || MatchForm == 1 || MatchForm == 2)
            {
                this.MatchForm = MatchForm;
            }

            //NOT USE ROI
            if (RoiForm == 0)
            {
                this.ScanXArea = 0;
                this.ScanXOverwrap = 0;
                this.ROIXAreaMin = 0;
                this.ROIXAreaMax = 0;
                this.ROIYAreaMin = 0;
                this.ROIYAreaMax = 0;
                this.ROIZPlaneMinDepth = 0;
                this.ROIZPlaneMaxDepth = 0;
            }

            //Moving ROI Param
            if (RoiForm == 1)
            {
                this.ScanXArea = ScanXArea;
                this.ScanXOverwrap = ScanXOverwrap;
                this.ROIXAreaMin = ROIXAreaMin;
                this.ROIXAreaMax = ROIXAreaMax;
                this.ROIYAreaMin = 0;
                this.ROIYAreaMax = 0;
                this.ROIZPlaneMinDepth = ROIZPlaneMinDepth;
                this.ROIZPlaneMaxDepth = ROIZPlaneMaxDepth;
                this.AutoRoiNOWPointN_BeforePointNDIFF = 0;
            }
            //Static ROI Param
            if (RoiForm == 2)
            {
                this.ScanXArea = 0;
                this.ScanXOverwrap = 0;
                this.ROIXAreaMin = ROIXAreaMin;
                this.ROIXAreaMax = ROIXAreaMax;
                this.ROIYAreaMin = ROIYAreaMin;
                this.ROIYAreaMax = ROIYAreaMax;
                this.ROIZPlaneMinDepth = ROIZPlaneMinDepth;
                this.ROIZPlaneMaxDepth = ROIZPlaneMaxDepth;
                this.AutoRoiNOWPointN_BeforePointNDIFF = 0;
            }
            if(RoiForm == 3)
            {
                this.ScanXArea = 0;
                this.ScanXOverwrap = 0;
                this.ROIXAreaMin = ROIXAreaMin;
                this.ROIXAreaMax = ROIXAreaMax;
                this.ROIYAreaMin = ROIYAreaMin;
                this.ROIYAreaMax = ROIYAreaMax;
                this.ROIZPlaneMinDepth = ROIZPlaneMinDepth;
                this.ROIZPlaneMaxDepth = ROIZPlaneMaxDepth;
                this.AutoRoiNOWPointN_BeforePointNDIFF = AutoRoiNOWPointN_BeforePointNDIFF;
            }
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

            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                "RoiForm " + RoiForm + '\n' +
                "MatchForm " + MatchForm + '\n' +
                "ScanXArea " + ScanXArea + '\n' +
                "ScanXOverwrap " + ScanXOverwrap + '\n' +
                "ROIXAreaMin " + ROIXAreaMin + '\n' +
                "ROIXAreaMax " + ROIXAreaMax + '\n' +
                "ROIYAreaMin " + ROIYAreaMin + '\n' +
                "ROIYAreaMax " + ROIYAreaMax + '\n' +
                "ROIZPlaneMinDepth " + ROIZPlaneMinDepth + '\n' +
                "ROIZPlaneMaxDepth " + ROIZPlaneMaxDepth + '\n' +
                "sampling_method " + sampling_method + '\n' +
                "sampling_distance " + sampling_distance + '\n' +
                "AutoRoiNOWPointN_BeforePointNDIFF " + AutoRoiNOWPointN_BeforePointNDIFF + '\n' );
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// saveSurfmatchParam 
        /// 3d surface matching에 있어 찾는 방법, 점수 산정 등에 대한 설정 메서드
        /// </summary>
        /// <param name="find_sfm_RelSamplingDistance"><para>매칭할 씬과 3D 모델간의 연관거리 값(default:0.1) 0.0 ~ 1.0</para>
        /// <para>값이 클 경우</para>
        /// <para>1. 씬의 피사체 Point와 SurfaceModel 피사체 Point의 동질성이 다소 낮더라도 매칭 판정</para>
        /// <para>2. 연산량 감소</para>
        /// <para>값이 작을 경우 </para>
        /// <para>1. 씬의 피사체 Point와 SurfaceModel 피사체 Point의 동질성이 매우 높아야 매칭 판정</para>
        /// <para>2. 연산량 증가</para></param>
        /// <param name="find_sfm_KeyPointFraction"><para>매칭할 씬과 3D 모델의 사용할 점구름의 %비율(default: 0.1) 0.0 ~ 1.0</para> 
        /// <para>값이 클 경우</para>
        /// <para>1. 반복 매칭시 결과값이 안정적</para>
        /// <para>2. SurfaceModel이 정교하고 획득한 씬이 안정적이어야 매칭 성공률증가.</para>
        /// <para>3. 연산량 증가</para>
        /// <para>값이 작을 경우</para>
        /// <para>1. 반복 매칭시 결과값은 불안정</para>
        /// <para>2. SurfaceModel과 씬이 정교하지 않아도 매칭 성공률 증가</para>
        /// <para>3. 연산량 감소</para></param>
        /// <param name="find_sfm_MinScore">매칭 최소 합격기준 %(default : 0.35) 0.0 ~ 1.0 </param>
        /// <param name="find_sfm_NumMatch">매칭 갯수(default:1) 1 ~ 100 </param>
        /// <param name="find_sfm_FindMethod">매칭 방법(default:"mls") "mls", "fast"</param>
        /// <param name="find_sfm_ScoreType"> <para>점수 산정 방법(default:"model_point_fraction") </para>
        /// <para>"model_point_fraction"(씬에서 훈련된 모델의 매칭된 포인트 수를 모델의 총 포인트 수로 나눈 값)</para>
        /// <para>"num_model_points"(훈련모델과 매칭된 씬의 오브젝트 point간의 연관거리를 이용한 가중 점수)</para>
        /// <para>"num_scene_points"(씬에서 매칭된 훈련 모델의 매칭된 point갯수)</para></param>
        /// <param name="find_sfm_max_overlap_dist_value"><para>피사체 간 중첩 판정 거리 값</para>
        /// <para>max_overlap_dist_rel = 1일 경우 중첩 불가 (default: 0.5) 0.0 ~ 1 </para>
        /// <para>max_overlap_dist_abs = 1일 경우 중첩 불가 1 ~ n </para></param>
        /// <param name="AxisAlign">정육면체, 구체, 원통 단순한 피사체의 좌표를 정렬하는 기능 (default:0) 0 = off, 1 = on</param>
        /// <param name="find_sfm_max_overlap_dist_type">피사체 간 중첩 판정 거리 방식 지정 (default:"max_overlap_dist_rel"), "max_overlap_dist_rel", "max_overlap_dist_abs"  </param>
        /// <param name="find_sfm_pose_ref_use_scene_normals_value">매칭 시 Scene의 normals 사용 여부 (default:0) 0 = off, 1 = on</param>
        /// <param name="find_sfm_pose_ref_num_steps_value">Dense pose refinement 반복 횟수 
        /// <para>값이 높을 수록 정확도는 올라가지만 연산시간이 늘어나며, 특정 횟수에서 최적화가 되면 그 이상 반복해도 정확도는 올라가지 않음(default:5) 1 ~ n </para></param>
        /// <param name="find_sfm_pose_ref_sub_sampling_value">Dense pose refinement에 사용될 Scene point 비율
        /// <para>값을 높이면 사용되는 point는 줄어들고 연산시간, 정확도 낮아짐(default:2) 1 ~ n </para></param>
        /// <param name="PickLimitDegree">Picking 각도 제한값</param>
        /// <param name="FindSurfModelTimeoutSec">FindSurfaceModel Operator 수행 제한시간</param>
        /// <param name="MultiModelOverlapMatchingMode"><para>0: 멀티모델에서 중복 매칭 허용</para>
        /// <para>1: 멀티모델에서 중복 매칭 발생시 결과들의 중심점 거리가 MultiModelOverlapMargin 값 보다 작으면 스코어가 높은 매칭만을 남김</para>
        /// <para>2: 멀티모델에서 중복 매칭 발생시 결과들의 바운드박스 거리가 MultiModelOverlapMargin 값 보다 작으면 스코어가 높은 매칭만을 남김</para></param>
        /// <param name="MultiModelOverlapMargin">MultiModelOverlapMatchingMode을 사용할 경우 중심점/바운드박스 간 거리 마진값</param>
        public void saveSurfmatchParam(double[] find_sfm_RelSamplingDistance, double[] find_sfm_KeyPointFraction, double[] find_sfm_MinScore, int[] find_sfm_NumMatch, string find_sfm_FindMethod, string find_sfm_ScoreType, double find_sfm_max_overlap_dist_value, int AxisAlign, string find_sfm_max_overlap_dist_type, int find_sfm_pose_ref_use_scene_normals_value, int find_sfm_pose_ref_num_steps_value, int find_sfm_pose_ref_sub_sampling_value, int PickLimitDegree, int FindSurfModelTimeoutSec, int MultiModelOverlapMatchingMode, double MultiModelOverlapMargin)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            this.find_sfm_RelSamplingDistance = find_sfm_RelSamplingDistance;
            this.find_sfm_KeyPointFraction = find_sfm_KeyPointFraction;
            this.find_sfm_MinScore = find_sfm_MinScore;
            this.find_sfm_NumMatch = find_sfm_NumMatch;
            if (find_sfm_FindMethod == "fast" || find_sfm_FindMethod == "mls")
            {
                this.find_sfm_FindMethod = find_sfm_FindMethod;
            }
            if (find_sfm_ScoreType == "model_point_fraction" || find_sfm_ScoreType == "num_model_points" || find_sfm_ScoreType == "num_scene_points")
            {
                this.find_sfm_ScoreType = find_sfm_ScoreType;
            }
            if (find_sfm_max_overlap_dist_value > 0.0 && find_sfm_max_overlap_dist_value < 1000)
            {
                this.find_sfm_max_overlap_dist_value = find_sfm_max_overlap_dist_value;
            }
            if (AxisAlign == 0 || AxisAlign == 1 || AxisAlign == 2 || AxisAlign == 3 || AxisAlign == 4 || AxisAlign == 5 || AxisAlign == 6)
            {
                this.AxisAlign = AxisAlign;
            }
            if (find_sfm_max_overlap_dist_type == "max_overlap_dist_rel" || find_sfm_max_overlap_dist_type == "max_overlap_dist_abs")
            {
                this.find_sfm_max_overlap_dist_type = find_sfm_max_overlap_dist_type;
            }
            if (find_sfm_pose_ref_use_scene_normals_value == 0)
            {
                this.find_sfm_pose_ref_use_scene_normals_value = "false";
            }
            else if (find_sfm_pose_ref_use_scene_normals_value == 1)
            {
                this.find_sfm_pose_ref_use_scene_normals_value = "true";
            }
            else
            {
                this.find_sfm_pose_ref_use_scene_normals_value = "false";
            }
            if (find_sfm_pose_ref_num_steps_value > 0 && find_sfm_pose_ref_num_steps_value < 100)
            {
                this.find_sfm_pose_ref_num_steps_value = find_sfm_pose_ref_num_steps_value;
            }
            if (find_sfm_pose_ref_sub_sampling_value > 0 && find_sfm_pose_ref_sub_sampling_value < 100)
            {
                this.find_sfm_pose_ref_sub_sampling_value = find_sfm_pose_ref_sub_sampling_value;
            }
            if (PickLimitDegree > 0 && PickLimitDegree < 360) {
                this.PickLimitDegree = PickLimitDegree;
            }
            if (FindSurfModelTimeoutSec >= 0)
            {
                this.FindSurfModelTimeoutSec = FindSurfModelTimeoutSec;
            }
            if (MultiModelOverlapMatchingMode == 0 || MultiModelOverlapMatchingMode == 1 || MultiModelOverlapMatchingMode == 2)
            {
                this.MultiModelOverlapMatchingMode = MultiModelOverlapMatchingMode;
            }
            this.MultiModelOverlapMargin = MultiModelOverlapMargin;

            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                "find_sfm_RelSamplingDistance " + find_sfm_RelSamplingDistance + '\n' +
                "find_sfm_KeyPointFraction " + find_sfm_KeyPointFraction + '\n' +
                "find_sfm_MinScore " + find_sfm_MinScore + '\n' +
                "find_sfm_NumMatch " + find_sfm_NumMatch + '\n' +
                "find_sfm_FindMethod " + find_sfm_FindMethod + '\n' +
                "find_sfm_ScoreType " + find_sfm_ScoreType + '\n' +
                "find_sfm_max_overlap_dist_value " + find_sfm_max_overlap_dist_value + '\n' +
                "AxisAlign " + AxisAlign + '\n' +
                "find_sfm_max_overlap_dist_type " + find_sfm_max_overlap_dist_type + '\n' +
                "find_sfm_pose_ref_use_scene_normals_value " + find_sfm_pose_ref_use_scene_normals_value + '\n' +
                "find_sfm_pose_ref_num_steps_value " + find_sfm_pose_ref_num_steps_value + '\n' +
                "find_sfm_pose_ref_sub_sampling_value " + find_sfm_pose_ref_sub_sampling_value + '\n' +
                "PickLimitDegree " + PickLimitDegree + '\n' +
                "FindSurfModelTimeoutSec " + FindSurfModelTimeoutSec + '\n' +
                "MultiModelOverlapMatchingMode " + MultiModelOverlapMatchingMode + '\n' +
                "MultiModelOverlapMargin " + MultiModelOverlapMargin + '\n');
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
        }

        /// <summary>
        /// findSurFaceMatching 3d surface matching 수행 메서드
        /// </summary>
        /// <param name="plyfileName">현재 씬의 3d 이미지인입 인자 (경로를 포함한 ply파일명) </param>
        /// <param name="sfmfileName">피사체의 surface model 파일(경로를 포함한 sfm파일명)</param>
        /// <param name="om3fileName">피사체의 object3 d model 파일(경로를 포함한 om3파일명)</param>
        /// <returns> 피사체의 로봇 6축 Base 좌표 (형식 Array[matchingid, score, tx, ty, tz, rx, ry, rz], ex:)[0, 0.7, 200.23, 150.5, 52.1, 0.1, 0.5, 0.1]) </returns>
        public string[] findSurFaceMatching(string plyfileName, string[] sfmfileName, string[] om3fileName)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
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
                    thread.Interrupt();
                    thread.Abort();
                }
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, '\n' + Ex.Message);
            }
            
            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            this.plyfileName = plyfileName;
            this.sfmfileName = sfmfileName;
            this.om3fileName = om3fileName;
            
            //double[] DArr = new double[6] { 0, 0, 0, 0, 0, 0 };
            //List<string> result;// = new List<string>();
            string[] retval = new string[1000];
            try
            {
                MyEngine = new HDevEngine();
                MyEngine.SetProcedurePath(PATH_NAME);
                Procedure = new HDevProcedure(SURF_PROCEDURE_STR);
                ProcCall = new HDevProcedureCall(Procedure);
                ProcCall.SetInputCtrlParamTuple(1, plyfileName);
                ProcCall.SetInputCtrlParamTuple(2, sfmfileName);
                ProcCall.SetInputCtrlParamTuple(3, om3fileName);
                ProcCall.SetInputCtrlParamTuple(4, RoiForm);
                ProcCall.SetInputCtrlParamTuple(5, MatchForm);
                ProcCall.SetInputCtrlParamTuple(6, ScanXArea);
                ProcCall.SetInputCtrlParamTuple(7, ScanXOverwrap);
                ProcCall.SetInputCtrlParamTuple(8, ROIXAreaMin);
                ProcCall.SetInputCtrlParamTuple(9, ROIXAreaMax);
                ProcCall.SetInputCtrlParamTuple(10, ROIYAreaMin);
                ProcCall.SetInputCtrlParamTuple(11, ROIYAreaMax);
                ProcCall.SetInputCtrlParamTuple(12, ROIZPlaneMinDepth);
                ProcCall.SetInputCtrlParamTuple(13, ROIZPlaneMaxDepth);
                ProcCall.SetInputCtrlParamTuple(14, sampling_method);
                ProcCall.SetInputCtrlParamTuple(15, sampling_distance);
                ProcCall.SetInputCtrlParamTuple(16, find_sfm_RelSamplingDistance);
                ProcCall.SetInputCtrlParamTuple(17, find_sfm_KeyPointFraction);
                ProcCall.SetInputCtrlParamTuple(18, find_sfm_MinScore);
                ProcCall.SetInputCtrlParamTuple(19, find_sfm_NumMatch);
                ProcCall.SetInputCtrlParamTuple(20, find_sfm_FindMethod);
                ProcCall.SetInputCtrlParamTuple(21, find_sfm_ScoreType);
                ProcCall.SetInputCtrlParamTuple(22, find_sfm_max_overlap_dist_value);
                ProcCall.SetInputCtrlParamTuple(23, AxisAlign);
                ProcCall.SetInputCtrlParamTuple(24, find_sfm_max_overlap_dist_type);
                ProcCall.SetInputCtrlParamTuple(25, find_sfm_pose_ref_use_scene_normals_value);
                ProcCall.SetInputCtrlParamTuple(26, find_sfm_pose_ref_num_steps_value);
                ProcCall.SetInputCtrlParamTuple(27, find_sfm_pose_ref_sub_sampling_value);
                ProcCall.SetInputCtrlParamTuple(28, PickLimitDegree);
                ProcCall.SetInputCtrlParamTuple(29, FindSurfModelTimeoutSec);
                ProcCall.SetInputCtrlParamTuple(30, AutoRoiNOWPointN_BeforePointNDIFF);
                ProcCall.SetInputCtrlParamTuple(31, MultiModelOverlapMatchingMode);
                ProcCall.SetInputCtrlParamTuple(32, MultiModelOverlapMargin);
                //ProcCall.SetInputCtrlParamTuple(12, ExtWin);

                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                    "PATH_NAME " + PATH_NAME + '\n' +
                    "SURF_PROCEDURE_STR " + SURF_PROCEDURE_STR + '\n' +
                    "plyfileName " + plyfileName + '\n' +
                    "sfmfileName " + sfmfileName + '\n' +
                    "om3fileName " + om3fileName + '\n' +
                    "RoiForm " + RoiForm + '\n' +
                    "MatchForm " + MatchForm + '\n' +
                    "ScanXArea " + ScanXArea + '\n' +
                    "ScanXOverwrap " + ScanXOverwrap + '\n' +
                    "ROIXAreaMin " + ROIXAreaMin + '\n' +
                    "ROIXAreaMax " + ROIXAreaMax + '\n' +
                    "ROIYAreaMin " + ROIYAreaMin + '\n' +
                    "ROIYAreaMax " + ROIYAreaMax + '\n' +
                    "ROIZPlaneMinDepth " + ROIZPlaneMinDepth + '\n' +
                    "ROIZPlaneMaxDepth " + ROIZPlaneMaxDepth + '\n' +
                    "sampling_method " + sampling_method + '\n' +
                    "sampling_distance " + sampling_distance + '\n' +
                    "find_sfm_RelSamplingDistance " + find_sfm_RelSamplingDistance + '\n' +
                    "find_sfm_KeyPointFraction " + find_sfm_KeyPointFraction + '\n' +
                    "find_sfm_MinScore " + find_sfm_MinScore + '\n' +
                    "find_sfm_NumMatch " + find_sfm_NumMatch + '\n' +
                    "find_sfm_FindMethod " + find_sfm_FindMethod + '\n' +
                    "find_sfm_ScoreType " + find_sfm_ScoreType + '\n' +
                    "find_sfm_max_overlap_dist_value " + find_sfm_max_overlap_dist_value + '\n' +
                    "AxisAlign " + AxisAlign + '\n' +
                    "find_sfm_max_overlap_dist_type " + find_sfm_max_overlap_dist_type + '\n' +
                    "find_sfm_pose_ref_use_scene_normals_value " + find_sfm_pose_ref_use_scene_normals_value + '\n' +
                    "find_sfm_pose_ref_num_steps_value " + find_sfm_pose_ref_num_steps_value + '\n' +
                    "find_sfm_pose_ref_sub_sampling_value " + find_sfm_pose_ref_sub_sampling_value + '\n' +
                    "PickLimitDegree " + PickLimitDegree + '\n' +
                    "FindSurfModelTimeoutSec " + FindSurfModelTimeoutSec + '\n' +
                    "AutoRoiNOWPointN_BeforePointNDIFF " + AutoRoiNOWPointN_BeforePointNDIFF + '\n' +
                    "MultiModelOverlapMatchingMode " + MultiModelOverlapMatchingMode + '\n' +
                    "MultiModelOverlapMargin " + MultiModelOverlapMargin + '\n');
                HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREREADY;
                ProcCall.Execute();
                HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE;
                mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE);
                HTuple MatchResult = ProcCall.GetOutputCtrlParamTuple("MatchResult");
                ObjectModel_Scene = ProcCall.GetOutputCtrlParamTuple("ObjectModel_Scene");
                ObjectModel3D_Result = ProcCall.GetOutputCtrlParamTuple("ObjectModel3D_Result");
                ObjectModel3D_ResultArrow = ProcCall.GetOutputCtrlParamTuple("ObjectModel3D_ResultArrow");
                ObjectModel3D_ResultRoI = ProcCall.GetOutputCtrlParamTuple("ObjectModel3D_ResultRoI");
                RGB_Scene = ProcCall.GetOutputIconicParamImage("RGB_Scene");
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + MatchResult.S + " " + MatchResult.SArr);
                //result = new List<string>(MatchResult.SArr);
                //result.CopyTo(retval);
                retval = MatchResult.SArr;
                string retval1Dim = "";
                foreach (var ret in retval)
                {
                    retval1Dim = retval1Dim + ret + '\n';
                }
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' +
                    "retval1Dim " + '\n' + retval1Dim);
                //Ret3D = new HTuple(ObjectModel_Scene, ObjectModel3D_Result);
                //Console.WriteLine(DArr[0] + " " + DArr[1] + " " + DArr[2] + " " + DArr[3] + " " + DArr[4] + " " + DArr[5]);
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
            return retval;
        }

        

        /// <summary>
        /// getResult3DDisp 메서드
        /// form에 붙은 halconwindow에 3d 이미지 인입
        /// </summary>
        /// <param name="MOVING_VIEW">3D View Static = 0, 3D View Movable and Button(강제종료 불가)버전 = 1</param>
        /// <param name="extWin">form의 hwindow 핸들</param>
        public void getResult3DDisp(int MOVING_VIEW, HWindow extWin)
        {
            
            int MOVINGDISP = 0;
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");
            this.extWin = extWin;

            if (MOVING_VIEW == 0 || MOVING_VIEW == 1)
            {
                MOVINGDISP = MOVING_VIEW;
            }

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
            
            thread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    MyEngine = new HDevEngine();
                    MyEngine.SetProcedurePath(PATH_NAME);
                    HDevWin = new HDevOpMultiWindowImpl(extWin);
                    MyEngine.SetHDevOperators(HDevWin);
                    Procedure = new HDevProcedure(VIS3D_PROCEDURE_STR);
                    ProcCall = new HDevProcedureCall(Procedure);
                    ProcCall.SetInputCtrlParamTuple(1, MOVINGDISP);
                    ProcCall.SetInputCtrlParamTuple(2, extWin);
                    ProcCall.SetInputCtrlParamTuple(3, ObjectModel_Scene);
                    ProcCall.SetInputCtrlParamTuple(4, ObjectModel3D_Result);
                    ProcCall.SetInputCtrlParamTuple(5, ObjectModel3D_ResultArrow);
                    ProcCall.SetInputCtrlParamTuple(6, ObjectModel3D_ResultRoI);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREREADY;
                    ProcCall.Execute();
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREDONE);
                    //HDevWin.Dispose();
                    //ProcCall.Dispose();
                    //Procedure.Dispose();
                    //MyEngine.Dispose();
                    //HDevWin = null;
                    //Procedure = null;
                    //ProcCall = null;
                    //MyEngine = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch (HDevEngineException Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                }
                catch (Exception Ex)
                {
                    HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + Ex.Message);
                    HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR;
                    mCBHalconState?.Invoke("p", (int)ENUM_HPROCEDURESTATE.PROCEDUREERROR);
                }
                /*finally
                {
                    HDevWin.Dispose();
                    ProcCall.Dispose();
                    Procedure.Dispose();
                    MyEngine.Dispose();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }*/
                HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
                HPROCEDURE_STATE = (int)ENUM_HPROCEDURESTATE.PROCEDURENONE;
                
            }));
            thread.Start();
        }

        /// <summary>
        /// getResult2DDisp 메서드
        /// form에 붙은 halconwindow에 2d 이미지 인입
        /// </summary>
        /// <param name="extWin">form의 hwindow 핸들</param>
        public void getResult2DDisp(HWindow extWin)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "START");

            try
            {
                RGB_Scene.DispImage(extWin);
                RGB_Scene.DispColor(extWin);
            }
            catch (HDevEngineException Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + Ex.Message);
            }
            catch (Exception Ex)
            {
                HLog.LogStr(ClassName, currentMethodName.ToString() + Ex.Message);
            }
            HLog.LogStr(ClassName, currentMethodName.ToString() + '\n' + "END");
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
                    if (MyEngine != null)
                    {
                        MyEngine.Dispose();
                    }

                    //ProcCall.Dispose();
                    thread.Abort();
                    thread = null;
                    if (File.Exists(VIS3D_PROCEDURE_NAME))
                    {
                        File.Delete(VIS3D_PROCEDURE_NAME);
                    }
                    if (File.Exists(SURF_PROCEDURE_NAME))
                    {
                        File.Delete(SURF_PROCEDURE_NAME);
                    }
                    if (File.Exists(ROTSTATE_PROCEDURE_NAME))
                    {
                        File.Delete(ROTSTATE_PROCEDURE_NAME);
                    }
                    if (File.Exists(HCAMPAR_FILE_NAME))
                    {
                        File.Delete(HCAMPAR_FILE_NAME);
                    }
                }
                //if(MyEngine != null)
                //{
                //    MyEngine.UnloadAllProcedures();
                //    MyEngine.Dispose();
                //}
                
                HDevWin = null;
                ProcCall = null;
                Procedure = null;
                MyEngine = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(VIS3D_PROCEDURE_NAME))
                {
                    File.Delete(VIS3D_PROCEDURE_NAME);
                }
                if (File.Exists(SURF_PROCEDURE_NAME))
                {
                    File.Delete(SURF_PROCEDURE_NAME);
                }
                if (File.Exists(ROTSTATE_PROCEDURE_NAME))
                {
                    File.Delete(ROTSTATE_PROCEDURE_NAME);
                }
                if (File.Exists(HCAMPAR_FILE_NAME))
                {
                    File.Delete(HCAMPAR_FILE_NAME);
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

