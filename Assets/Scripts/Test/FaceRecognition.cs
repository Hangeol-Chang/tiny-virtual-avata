using UnityEngine;

using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.FaceLandmarker;


public class FaceRecognition : MonoBehaviour
{
    [SerializeField] private FaceLandmarkerResultAnnotationController annotationController;

    private void Start()
    {
        // Initialize the face landmarker result annotation controller
        if (annotationController == null)
        {
            Debug.LogError("FaceLandmarkerResultAnnotationController is not assigned.");
            return;
        }

        // Additional setup can be done here if needed
    }

    public void DrawFaceLandmarks(FaceLandmarkerResult result)
    {
        annotationController.DrawNow(result);
    }
}