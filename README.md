# PicknPlaceHLib

Halcon 3D Surface Based Matching을 수행하는 C# Project.
모델 생성, 합성, 매칭으로 나뉘어져 있으며, UI단의 요청에 따라 Halcon 3D Viewer를 제공한다.

매칭 성공시 생성되는 회전좌표는 0~360' 범위의 Roll Pitch Yaw  Camera 기준 좌표다.
위 회전행렬을 UR로봇의 Rotation Vector와 일치시키기 위해 회전벡터로 변환하여 XYZRxRyRz의 6축 좌표를 제공한다.
이 6축좌표는 는 ZIVID SDK의 3D Calibration후 생성되는 동질 회전 매트릭스를 이용해 로봇좌표로 변환된다.


산출물: HDevelope Project를 수행하는 C# Dll (빌드하여 생성된 DLL은 Halcon DLL과 함께 ILMerge로 다시금 합쳐 배포)

API Reference, UML ./Docu 디렉토리 참조.
초기 개인 테스트 프로젝트는 ./GC_Pick 디렉토리 참조.
