// Debug toggles for the avatar overlay scene: C shows/hides the raw webcam preview,
// V shows/hides the face detector's raw keypoints. Uses the new Input System API since
// this project's Active Input Handling is set to "Input System Package (New)" only.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.FaceDetection
{
  public class OverlayInputController : MonoBehaviour
  {
    [SerializeField] private RawImage _cameraPreview;
    [SerializeField] private FaceKeypointsVisualizer _keypointsVisualizer;
    [SerializeField] private bool _cameraVisibleAtStart = false;
    [SerializeField] private bool _keypointsVisibleAtStart = false;

    private void Start()
    {
      if (_cameraPreview != null)
      {
        _cameraPreview.enabled = _cameraVisibleAtStart;
      }

      if (_keypointsVisualizer != null)
      {
        _keypointsVisualizer.SetVisible(_keypointsVisibleAtStart);
      }
    }

    private void Update()
    {
      var keyboard = Keyboard.current;
      if (keyboard == null)
      {
        return;
      }

      if (keyboard.cKey.wasPressedThisFrame && _cameraPreview != null)
      {
        _cameraPreview.enabled = !_cameraPreview.enabled;
      }

      if (keyboard.vKey.wasPressedThisFrame && _keypointsVisualizer != null)
      {
        _keypointsVisualizer.SetVisible(!_keypointsVisualizer.isVisible);
      }
    }
  }
}
