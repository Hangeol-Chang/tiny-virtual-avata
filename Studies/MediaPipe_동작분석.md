
## Package Code
### MultiFaceLandmarkListAnnoation.cs
-> Face Mark를 받아서 View에 표시해줌

### VisionTaskAPI
-> 실질적으로 처리하는 놈인듯?


## Sample codes
### FaceLandmarkRunner.cs
-> MonoBehaviour를 상속받는 BaseRunner를 상속받는 객체

### VisionTaskRunner
- BaseRunner를 상속받아서 VisionTaskAPI를 사용하게 만들어 놓은거인듯?

### BaseRunner?
- TaskApiRunner.cs 라는 파일
- Play, Pause 등의 구조가 들어있음.
-> Coroutine으로 돌아감.


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
