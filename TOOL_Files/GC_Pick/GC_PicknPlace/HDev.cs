using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;
using System.Threading;

namespace GC_PicknPlace
{
    class HDev
    {
        private static HDev mHDev;
        private HDevEngine MyEngine;
        private HDevProgram Program;
        private HDevProgramCall ProgramCall;
        private HDevOpMultiWindowImpl HDevWin;
        private bool isRun = false;
        public bool isInit = false;
        //0310 할콘에게 피사체의 6축 로드리게즈 좌표 획득
        static double[] DArr = new double[6];

        protected HDev()
        {
            //HTuple windowtest = Window;
            initHalconProgram();
        }

        public static HDev Instance()
        {
            if (mHDev == null)
            {
                mHDev = new HDev();
            }
            return mHDev;
        }

        public void Dispose()
        {
            HalconDispose();
            mHDev = null;
        }

        private void initHalconProgram()
        {
            MyEngine = new HDevEngine();
            HTuple Stat = null;
            HDevWin = new HDevOpMultiWindowImpl();

            MyEngine.SetHDevOperators(HDevWin);
            
            string halconExamples = HSystem.GetSystem("example_dir");
            //모델링 합성 후 피사체의 방향성을 추가한 모델이 사용된 프로젝트
            string ProgramPathString = "C:/Users/ABC/Documents/Visual Studio 2015/Projects/GC_PicknPlace/GC_PicknPlace/bin/x64/Debug/HalconProj/SurfaceMatchingFindModel200310.hdev";
            //string ProgramPathString = "HalconProj/SurfaceMatchingFindModel200219.hdev";
            string ProcedurePath = "C:/Program Files/MVTec/HALCON-18.11-Steady/procedures";
            //string ProgramPathString = "D:/workspace/example.hdev";

            MyEngine.SetProcedurePath(ProcedurePath);
            // 프로시져가 아닌 HALCON 코드로 실행 
            Program = new HDevProgram(ProgramPathString);
            ProgramCall = new HDevProgramCall(Program);
            //HTuple windowtest = Window;
            //HTuple Stat = null;

            isInit = true;
            isRun = false;
        }

        public double[] getPointArray()
        {
            return DArr;
        }
        public void ProgramRun()
        {
            if (isInit && !isRun)
            {
                isRun = true;

                try { 
                    ProgramCall.Execute();
                    HTuple X = ProgramCall.GetCtrlVarTuple("SubjectCoordX");
                    HTuple Y = ProgramCall.GetCtrlVarTuple("SubjectCoordY");
                    HTuple Z = ProgramCall.GetCtrlVarTuple("SubjectCoordZ");
                    //회전 좌표 추가
                    HTuple RX = ProgramCall.GetCtrlVarTuple("SubjectCoordRX");
                    HTuple RY = ProgramCall.GetCtrlVarTuple("SubjectCoordRY");
                    HTuple RZ = ProgramCall.GetCtrlVarTuple("SubjectCoordRZ");

                    Console.WriteLine(String.Format(X + " " + Y + " " + Z));
                    DArr[0] = X.D;
                    DArr[1] = Y.D;
                    DArr[2] = Z.D;
                    DArr[3] = RX.D;
                    DArr[4] = RY.D;
                    DArr[5] = RZ.D;
                    Console.WriteLine(DArr[0] + "" + DArr[1] + "" + DArr[2]);
                    Console.WriteLine(DArr[3] + "" + DArr[4] + "" + DArr[5]);
                    if (ProgramCall.GetCtrlVarTuple("SubjectCoordX") > 0)
                    {
                        isRun = false;
                        //HalconDispose();
                    }
                }catch(Exception ex)
                {

                    DArr[0] = 0;
                    DArr[1] = 0;
                    DArr[2] = 0;
                    DArr[3] = 0;
                    DArr[4] = 0;
                    DArr[5] = 0;

                    Console.WriteLine(DArr[0] + "" + DArr[1] + "" + DArr[2]);
                    Console.WriteLine(DArr[3] + "" + DArr[4] + "" + DArr[5]);
                }
                Thread test = new Thread(new ThreadStart(delegate ()
                {
                    //this.Invoke(new Action(delegate ()
                    //{
                    //HTuple Stat = ProgramCall.GetCtrlVarTuple("Value");
                    
                    //}));
                }));
                test.Start();
            }

        }
        private void HalconDispose()
        {
            //HTuple winID;
            //HDevWin.DevGetWindow(out winID);
            //HDevWin.DevClearWindow();
            HDevWin.DevCloseWindow();
            ProgramCall.Dispose();
            Program.Dispose();
            MyEngine.Dispose();
            isInit = false;
            isRun = false;
        }
    }
}
