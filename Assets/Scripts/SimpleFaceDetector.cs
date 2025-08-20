using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity;
using Mediapipe;


public class SimpleFaceDetector : MonoBehaviour
{
    [Header("Face Detection Settings")]
    private string modelPath = "face_landmarker_v2_with_blendshapes.bytes";
    [SerializeField] private bool useWebcam = true;
    [SerializeField] private int targetFrameRate = 30;

    [Header("UI")]
    [SerializeField] private RawImage webcamDisplay; // 웹캠 화면을 표시할 UI
    [SerializeField] private Transform faceIndicator; // 얼굴 위치를 표시할 UI 오브젝트

    [Header("Annotation")]
    [SerializeField] private FaceLandmarkerResultAnnotationController annotationController;

    // Private fields
    private FaceLandmarker faceLandmarker;
    private WebCamTexture webCamTexture;
    private Texture2D inputTexture;
    private bool isProcessing = false;
    private bool isInitialized = false;

    // Face position tracking
    private Vector2 faceCenterPosition = Vector2.zero;
    private bool faceDetected = false;

    void Start()
    {
        // 프레임 레이트 설정
        Application.targetFrameRate = targetFrameRate;

        // FaceLandmarker 초기화
        StartCoroutine(InitializeSystem());
    }

    IEnumerator InitializeSystem()
    {
        Debug.Log("초기화 시작...");
        
        // 모델 파일 경로 설정 (StreamingAssets 폴더 사용)
        string fullModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);
        
        // 파일 존재 확인
        if (!System.IO.File.Exists(fullModelPath))
        {
            Debug.LogError($"모델 파일을 찾을 수 없습니다: {fullModelPath}");
            yield break;
        }

        // MediaPipe 초기화 확인
        if (GpuManager.GpuResources == null)
        {
            Debug.Log("GPU 리소스를 초기화합니다...");
            yield return new WaitForSeconds(0.5f); // GPU 초기화 대기
        }

        try
        {
            // FaceLandmarker 옵션 설정
            var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(modelAssetPath: fullModelPath);
            var options = new FaceLandmarkerOptions(
                baseOptions,
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE,
                numFaces: 1,
                minFaceDetectionConfidence: 0.5f,
                minFacePresenceConfidence: 0.5f,
                minTrackingConfidence: 0.5f,
                outputFaceBlendshapes: false,
                outputFaceTransformationMatrixes: false
            );

            // FaceLandmarker 생성 (GPU 리소스 사용)
            faceLandmarker = FaceLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            isInitialized = true;
            Debug.Log("FaceLandmarker 초기화 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"초기화 실패: {e.Message}\n{e.StackTrace}");
            yield break;
        }

        // 웹캠 초기화 (try-catch 외부에서)
        if (useWebcam)
        {
            yield return StartCoroutine(InitializeWebcam());
        }
    }

    IEnumerator InitializeWebcam()
    {
        // 사용 가능한 웹캠 장치 찾기
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("웹캠 장치를 찾을 수 없습니다.");
            yield break;
        }

        // 첫 번째 웹캠 사용
        string deviceName = WebCamTexture.devices[0].name;
        webCamTexture = new WebCamTexture(deviceName, 640, 480, 30);
        
        // UI가 할당되지 않은 경우 자동으로 생성
        if (webcamDisplay == null)
        {
            CreateWebcamUI();
        }
        
        // 웹캠 UI 표시 설정
        if (webcamDisplay != null)
        {
            webcamDisplay.texture = webCamTexture;
            webcamDisplay.material.mainTexture = webCamTexture;
        }
        
        webCamTexture.Play();

        // 웹캠이 실제로 시작될 때까지 대기
        yield return new WaitUntil(() => webCamTexture.isPlaying && webCamTexture.width > 16);

        Debug.Log($"웹캠 시작: {deviceName} ({webCamTexture.width}x{webCamTexture.height})");
    }

    void CreateWebcamUI()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WebcamCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 웹캠 디스플레이 생성
        GameObject webcamObj = new GameObject("WebcamDisplay");
        webcamObj.transform.SetParent(canvas.transform, false);
        
        webcamDisplay = webcamObj.AddComponent<RawImage>();
        RectTransform webcamRect = webcamDisplay.rectTransform;
        webcamRect.anchorMin = new Vector2(0, 0.5f);
        webcamRect.anchorMax = new Vector2(0.5f, 1f);
        webcamRect.offsetMin = Vector2.zero;
        webcamRect.offsetMax = Vector2.zero;

        // 얼굴 표시 인디케이터 생성
        GameObject indicatorObj = new GameObject("FaceIndicator");
        indicatorObj.transform.SetParent(webcamObj.transform, false);
        
        UnityEngine.UI.Image indicator = indicatorObj.AddComponent<UnityEngine.UI.Image>();
        indicator.color = UnityEngine.Color.red;
        faceIndicator = indicatorObj.transform;
        
        RectTransform indicatorRect = indicator.rectTransform;
        indicatorRect.sizeDelta = new Vector2(20, 20);
        indicatorRect.anchoredPosition = Vector2.zero;

        Debug.Log("웹캠 UI가 자동으로 생성되었습니다.");
    }

    void Update()
    {
        // 초기화가 완료되고 웹캠이 준비되었는지 확인
        if (isInitialized && webCamTexture != null && webCamTexture.isPlaying &&
            faceLandmarker != null && !isProcessing && 
            webCamTexture.width > 16 && webCamTexture.height > 16)
        {
            ProcessFrame();
        }

        // 얼굴 위치 표시 업데이트
        if (faceIndicator != null && faceDetected)
        {
            UpdateFaceIndicator();
        }

        // 얼굴 중심 위치 로그 출력 (매 60프레임마다)
        if (Time.frameCount % 60 == 0 && faceDetected)
        {
            Debug.Log($"얼굴 중심 위치: ({faceCenterPosition.x:F3}, {faceCenterPosition.y:F3})");
        }
    }

    void ProcessFrame()
    {
        if (webCamTexture.width <= 16 || webCamTexture.height <= 16)
            return;

        isProcessing = true;

        try
        {
            // WebCamTexture를 Texture2D로 변환
            if (inputTexture == null ||
                inputTexture.width != webCamTexture.width ||
                inputTexture.height != webCamTexture.height)
            {
                if (inputTexture != null)
                    Destroy(inputTexture);

                inputTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            }

            // 픽셀 데이터 복사
            Color32[] pixels = webCamTexture.GetPixels32();
            inputTexture.SetPixels32(pixels);
            inputTexture.Apply();

            // MediaPipe Image로 변환 (Mediapipe.Image를 명시적으로 사용)
            var image = new Mediapipe.Image(ImageFormat.Types.Format.Srgb, inputTexture.width, inputTexture.height,
                                inputTexture.width * 3, inputTexture.GetRawTextureData<byte>());

            // 얼굴 검출 수행
            var result = FaceLandmarkerResult.Alloc(1);
            bool detectionSuccess = faceLandmarker.TryDetect(image, null, ref result);

            // 결과 처리
            if (detectionSuccess)
            {
                ProcessFaceDetectionResult(result);

                // Annotation 업데이트 (옵션)
                if (annotationController != null)
                {
                    annotationController.DrawNow(result);
                }
            }
            else
            {
                faceDetected = false;
                faceCenterPosition = Vector2.zero;
                
                if (annotationController != null)
                {
                    annotationController.DrawNow(default);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"얼굴 검출 처리 중 오류: {e.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    void ProcessFaceDetectionResult(FaceLandmarkerResult result)
    {
        if (result.faceLandmarks != null && result.faceLandmarks.Count > 0)
        {
            faceDetected = true;

            // 첫 번째 얼굴의 랜드마크 가져오기
            var landmarks = result.faceLandmarks[0].landmarks;

            // 얼굴 중심 위치 계산 (코 끝 랜드마크 사용, 인덱스 1)
            if (landmarks.Count > 1)
            {
                var noseTip = landmarks[1]; // 코 끝
                faceCenterPosition = new Vector2(noseTip.x, noseTip.y);
            }
        }
        else
        {
            faceDetected = false;
            faceCenterPosition = Vector2.zero;
        }
    }

    void UpdateFaceIndicator()
    {
        if (faceIndicator != null && faceDetected)
        {
            // 얼굴 위치를 화면 좌표로 변환하여 UI 표시
            if (webcamDisplay != null)
            {
                RectTransform displayRect = webcamDisplay.rectTransform;
                Vector2 screenPos = new Vector2(
                    faceCenterPosition.x * displayRect.rect.width,
                    (1.0f - faceCenterPosition.y) * displayRect.rect.height
                );
                
                // 로컬 좌표로 변환
                Vector2 localPos = screenPos - displayRect.rect.size * 0.5f;
                faceIndicator.localPosition = new Vector3(localPos.x, localPos.y, 0);
            }
        }
    }

    // 공개 메서드 - 외부에서 얼굴 위치 정보 접근
    public Vector2 GetFaceCenterPosition()
    {
        return faceCenterPosition;
    }

    public bool IsFaceDetected()
    {
        return faceDetected;
    }

    void OnDestroy()
    {
        // 리소스 정리
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }

        if (inputTexture != null)
        {
            Destroy(inputTexture);
        }

        if (faceLandmarker != null)
        {
            faceLandmarker.Close();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (webCamTexture != null)
        {
            if (pauseStatus)
                webCamTexture.Pause();
            else
                webCamTexture.Play();
        }
    }
}
