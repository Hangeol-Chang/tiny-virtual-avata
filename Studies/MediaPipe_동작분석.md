
## Package Code
### MultiFaceLandmarkListAnnoation.cs
-> Face Mark를 받아서 View에 표시해줌
-> Face, Iris LandmarkListAnnotation 코드가 있는 프리팹을 소환함

### VisionTaskAPI
-> 실질적으로 처리하는 놈인듯?

### FacewLandmerker
-> BaseVisionTaskApi를 상속받으며, 실제 동작하는 놈.


### FaceLandmarkResult
-> 결과를 주는 친구

### ListAnotation

### IrisLandmarkListAnnotation
### FaceLandmarkListAnnotation
-> 이 두 개 안에서 입 index, 눈 index 같은거 보고 처리해야함.
얼굴의 중심좌표는?

## Sample codes
### FaceLandmarkRunner.cs
-> MonoBehaviour를 상속받는 BaseRunner를 상속받는 객체

### VisionTaskRunner
- BaseRunner를 상속받아서 VisionTaskAPI를 사용하게 만들어 놓은거인듯?

### BaseRunner?
- TaskApiRunner.cs 라는 파일
- Play, Pause 등의 구조가 들어있음.
-> Coroutine으로 돌아감.

### ImageSourceProvider
- WebCam 가져오는애.


## Essentials
### Bootstrap ?
- AppSetting을 가지고 있음.

### AppSetting?
- 그냥 Sample의 실행 구조에 따른 것.
- ScriptableObject를 상속한 세팅스크립트.
- 내가 새로 오브젝트를 만들 순 없게 되어있음.

- 기본적인 앱 설정 등을 제어.
    - webcam <-> video 등



# Deep Dive

## FaceLandmarkRunner.cs
- OnFaceLandmarkDetectionOutput
    - liveCam 사용시 여기로 이미지 디텍션 결과가 넘어옴.
    - FaceLandmarkerResult 형태