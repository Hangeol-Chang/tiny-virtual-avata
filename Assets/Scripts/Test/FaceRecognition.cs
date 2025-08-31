using UnityEngine;
using System;
using System.Collections;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using System.Text;
using Mediapipe;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using UnityEngine.Rendering;

using Stopwatch = System.Diagnostics.Stopwatch;

public class FaceRecognition : MonoBehaviour
{
    protected long GetCurrentTimestampMillisec() => _stopwatch.IsRunning ? _stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond : -1;

    [SerializeField] private FaceLandmarkerResultAnnotationController annotationController;

    public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();
    [SerializeField] protected Mediapipe.Unity.Screen screen;

    private TextureFramePool _textureFramePool;

    [SerializeField] private GameObject _bootstrapPrefab;
    private Bootstrap bootstrap;
    protected bool isPaused;
    private readonly Stopwatch _stopwatch = new();

    private void Start()
    {
        // Initialize the face landmarker result annotation controller
        if (annotationController == null)
        {
            Debug.LogError("FaceLandmarkerResultAnnotationController is not assigned.");
            return;
        }
        // Additional setup can be done here if needed
        StartCoroutine(Run());
    }

    protected Bootstrap FindBootstrap()
    {
        var bootstrapObj = GameObject.Find("Bootstrap");
        if (bootstrapObj == null)
        {
            Debug.Log("Initializing the Bootstrap GameObject");
            bootstrapObj = Instantiate(_bootstrapPrefab);
            bootstrapObj.name = "BootStrap";
            DontDestroyOnLoad(bootstrapObj);
        }

        return bootstrapObj.GetComponent<Bootstrap>();
    }

    private IEnumerator Run()
    {
        bootstrap = FindBootstrap();
        yield return new WaitUntil(() => bootstrap.isFinished);

        isPaused = false;
        _stopwatch.Restart();

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("== Face Recognition Configuration: ==");
        sb.Append($"Delegate = {config.Delegate}\n");
        sb.Append($"Image Read Mode = {config.ImageReadMode}\n");
        sb.Append($"Running Mode = {config.RunningMode}\n");
        sb.Append($"NumFaces = {config.NumFaces}\n");
        sb.Append($"MinFaceDetectionConfidence = {config.MinFaceDetectionConfidence}\n");
        sb.Append($"MinFacePresenceConfidence = {config.MinFacePresenceConfidence}\n");
        sb.Append($"MinTrackingConfidence = {config.MinTrackingConfidence}\n");
        sb.Append($"OutputFaceBlendshapes = {config.OutputFaceBlendshapes}\n");
        sb.Append($"OutputFacialTransformationMatrixes = {config.OutputFacialTransformationMatrixes}\n");
        sb.Append($"Model Path = {config.ModelPath}\n");

        Debug.Log(sb.ToString());

        yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

        // config 
        var options = config.GetFaceLandmarkerOptions(DrawFaceLandmarks);
        var taskApi = FaceLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
        var imageSource = ImageSourceProvider.ImageSource;

        yield return imageSource.Play();

        if (!imageSource.isPrepared)
        {
            Debug.LogError("Failed to start ImageSource, exiting...");
            yield break;
        }

        _textureFramePool = new TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

        // Running 시작
        // NOTE: The screen will be resized later, keeping the aspect ratio.
        screen.Initialize(imageSource);
        annotationController.isMirrored = false; // Set to true if you want the image to be mirrored
        annotationController.imageSize = new Vector2Int(imageSource.textureWidth, imageSource.textureHeight);

        var transformationOptions = imageSource.GetTransformationOptions();
        var flipHorizontally = transformationOptions.flipHorizontally;
        var flipVertically = transformationOptions.flipVertically;
        var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);


        AsyncGPUReadbackRequest req = default;
        var waitUntilReqDone = new WaitUntil(() => req.done);
        var waitForEndOfFrame = new WaitForEndOfFrame();
        var result = FaceLandmarkerResult.Alloc(options.numFaces);

        // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
        var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
        using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

        Debug.Log("Starting the main processing loop");
        // Running
        while (true)
        {
            if (isPaused) yield return new WaitWhile(() => isPaused);

            // 텍스처 프레임 풀에서 사용할 수 있는 TextureFrame을 가져옵니다. 없으면 다음 프레임까지 대기합니다.
            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
            {
                // 여기서 걸림.
                yield return null;
                continue;
            }

            Image image = null;
            switch (config.ImageReadMode)
            {
                case ImageReadMode.GPU:
                    // GPU 모드: GPU에서 텍스처를 읽어와서 이미지로 변환합니다.
                    if (!canUseGpuImage)
                    {
                        throw new System.Exception("ImageReadMode.GPU is not supported");
                    }
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildGPUImage(glContext);
                    // GPU에서 텍스처 복사가 완료될 때까지 한 프레임 대기합니다.
                    yield return waitForEndOfFrame;
                    break;
                case ImageReadMode.CPU:
                    // CPU 모드: CPU에서 텍스처를 읽어와서 이미지로 변환합니다.
                    yield return waitForEndOfFrame;
                    textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release(); // 사용한 TextureFrame 반환
                    break;
                case ImageReadMode.CPUAsync:
                default:
                    // 비동기 CPU 모드: AsyncGPUReadback을 사용해 비동기로 텍스처를 읽어옵니다.
                    req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    yield return waitUntilReqDone; // 읽기 완료까지 대기

                    if (req.hasError)
                    {
                        Debug.LogWarning($"Failed to read texture from the image source");
                        continue;
                    }
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release(); // 사용한 TextureFrame 반환
                    break;
            }
            

            // 라이브 스트림 모드: 비동기로 얼굴 검출을 요청합니다. 결과는 콜백에서 처리됩니다.
            // image에서 검출하는 API를 호출함.
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
        }
    }

    // 최종 호출부
    private void DrawFaceLandmarks(FaceLandmarkerResult result, Image image, long timestamp)
    {
        // 얼굴의 중심좌표 Debug
        Debug.Log("FaceLandmarkerResult received at timestamp: " + timestamp);
        Debug.Log("Face center coordinates: " + result.ToString());
        annotationController.DrawNow(result);
    }
}