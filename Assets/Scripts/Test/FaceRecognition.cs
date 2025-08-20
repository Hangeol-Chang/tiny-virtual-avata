using UnityEngine;

using System.Collections;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using System.Text;
using Mediapipe;
using Mediapipe.Unity.Sample;
using UnityEngine.Rendering;

public class FaceRecognition : MonoBehaviour
{
    [SerializeField] private FaceLandmarkerResultAnnotationController annotationController;

    public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();
    [SerializeField] protected Mediapipe.Unity.Screen screen;

    [SerializeField] private GameObject _bootstrapPrefab;
    private Bootstrap bootstrap;
    protected bool isPaused;

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

        // Running 시작
        // NOTE: The screen will be resized later, keeping the aspect ratio.
        screen.Initialize(imageSource);
        annotationController.isMirrored = false; // Set to true if you want the image to be mirrored
        annotationController.imageSize = new Vector2Int(imageSource.textureWidth, imageSource.textureHeight);

        var transformationOptions = imageSource.GetTransformationOptions();
        var flipHorizontally = transformationOptions.flipHorizontally;
        var flipVertically = transformationOptions.flipVertically;
        var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);


        // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
        var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
        using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;


        // Running
        while (true)
        {
            if (isPaused)
            {
                yield return new WaitWhile(() => isPaused);
            }

            
        }
    }

    // 최종 호출부
    private void DrawFaceLandmarks(FaceLandmarkerResult result, Image image, long timestamp)
    {
        annotationController.DrawNow(result);
    }
}