using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicknPlaceHLib
{
    /// <summary>
    /// PROCEDURE 객체 상태
    /// </summary>
    public enum ENUM_HPROCEDURESTATE : int
    {
        /// <summary>
        /// Halcon 프로시저 미수행 상태
        /// </summary>
        PROCEDURENONE = 0,
        /// <summary>
        /// Halcon 엔진, 프로시저를 생성하고 파라미터를 전부 저장하고 프로시저 호출 직전 상태
        /// </summary>
        PROCEDUREREADY,
        /// <summary>
        /// Halcon 프로시저를 수행하고 정상적으로 완료한 상태
        /// </summary>
        PROCEDUREDONE,
        /// <summary>
        /// Halcon 프로시저 내의 에러를 반환한 상태
        /// </summary>
        PROCEDUREERROR
    }

    /// <summary>
    /// MODEL 변수 상태
    /// </summary>
    public enum ENUM_HMODELSTATE : int
    {
        /// <summary>
        /// Halcon 프로시저에서 C#으로 반환되는 Object3DModel이 없는 상태
        /// </summary>
        MODELNONE = 0,
        /// <summary>
        /// Halcon 프로시저에서 C#으로 반환되는 Object3DModel이 있는 상태
        /// </summary>
        MODELREADY,
        /// <summary>
        /// C#에서 Object3DModel을 파일로 정상 생성한 상태
        /// </summary>
        MODELCREATE,
        /// <summary>
        /// C#에서 Object3DModel을 파일로 생성 못한 상태
        /// </summary>
        MODELERROR
    }


    internal class UTIL
    {
    }
}
