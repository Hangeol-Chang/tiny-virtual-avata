// Moves a UI RectTransform to follow the primary detected face, using the same
// image-space -> local-space conversion helpers the MediaPipe sample annotations use,
// so the avatar lines up with the camera preview without any extra calibration.
using Mediapipe.Unity.CoordinateSystem;
using UnityEngine;

using FaceDetectionResult = Mediapipe.Tasks.Components.Containers.DetectionResult;

namespace Mediapipe.Unity.Sample.FaceDetection
{
  public class FaceAvatarController : MonoBehaviour
  {
    [SerializeField] private RectTransform _avatarRectTransform;
    [SerializeField] private RectTransform _screenRectTransform;
    [SerializeField] private float _followSpeed = 12f;
    [SerializeField, Range(0, 1)] private float _minDetectionScore = 0.5f;
    [SerializeField] private bool _hideWhenNoFace = true;

    public bool isMirrored { get; set; }
    public RotationAngle rotationAngle { get; set; } = RotationAngle.Rotation0;
    public Vector2Int imageSize { get; set; }

    private readonly object _targetLock = new object();
    private FaceDetectionResult _currentTarget;
    private bool _isStale;

    private bool _hasValidPosition;
    private Vector2 _targetPosition;

    private void Awake()
    {
      if (_avatarRectTransform == null)
      {
        _avatarRectTransform = GetComponent<RectTransform>();
      }
    }

    /// <summary>Update the target from the render thread (LIVE_STREAM callback).</summary>
    public void DrawLater(FaceDetectionResult target)
    {
      lock (_targetLock)
      {
        target.CloneTo(ref _currentTarget);
        _isStale = true;
      }
    }

    /// <summary>Update the target immediately. Must be called from the main thread.</summary>
    public void DrawNow(FaceDetectionResult target)
    {
      lock (_targetLock)
      {
        target.CloneTo(ref _currentTarget);
      }
      SyncNow();
    }

    private void LateUpdate()
    {
      if (_isStale)
      {
        SyncNow();
      }

      if (_hasValidPosition)
      {
        if (_hideWhenNoFace && !_avatarRectTransform.gameObject.activeSelf)
        {
          _avatarRectTransform.gameObject.SetActive(true);
        }

        var t = 1f - Mathf.Exp(-_followSpeed * Time.deltaTime);
        _avatarRectTransform.anchoredPosition = Vector2.Lerp(_avatarRectTransform.anchoredPosition, _targetPosition, t);
      }
      else if (_hideWhenNoFace && _avatarRectTransform.gameObject.activeSelf)
      {
        _avatarRectTransform.gameObject.SetActive(false);
      }
    }

    private void SyncNow()
    {
      FaceDetectionResult target;
      lock (_targetLock)
      {
        _isStale = false;
        target = _currentTarget;
      }

      if (target.detections == null || target.detections.Count == 0 || imageSize.x <= 0 || imageSize.y <= 0)
      {
        _hasValidPosition = false;
        return;
      }

      var best = target.detections[0];
      var bestScore = best.categories != null && best.categories.Count > 0 ? best.categories[0].score : 1f;

      for (var i = 1; i < target.detections.Count; i++)
      {
        var d = target.detections[i];
        var score = d.categories != null && d.categories.Count > 0 ? d.categories[0].score : 1f;
        if (score > bestScore)
        {
          bestScore = score;
          best = d;
        }
      }

      if (bestScore < _minDetectionScore)
      {
        _hasValidPosition = false;
        return;
      }

      var box = best.boundingBox;
      var centerX = (box.left + box.right) / 2f;
      var centerY = (box.top + box.bottom) / 2f;

      var localPoint = ImageCoordinate.ImageToPoint(_screenRectTransform.rect, (int)centerX, (int)centerY, imageSize.x, imageSize.y, rotationAngle, isMirrored);
      _targetPosition = localPoint;
      _hasValidPosition = true;
    }
  }
}
