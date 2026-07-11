// Draws small dot markers at the face detector's normalized keypoints (eyes, nose, mouth,
// ears) so they can be toggled on for debugging without touching the avatar controller.
using System.Collections.Generic;
using Mediapipe.Unity.CoordinateSystem;
using UnityEngine;
using UnityEngine.UI;

using FaceDetectionResult = Mediapipe.Tasks.Components.Containers.DetectionResult;
using UIImage = UnityEngine.UI.Image;

namespace Mediapipe.Unity.Sample.FaceDetection
{
#pragma warning disable IDE0065
  using Color = UnityEngine.Color;
#pragma warning restore IDE0065

  public class FaceKeypointsVisualizer : MonoBehaviour
  {
    [SerializeField] private RectTransform _screenRectTransform;
    [SerializeField] private int _maxPoints = 6;
    [SerializeField] private float _pointSize = 14f;
    [SerializeField] private Color _pointColor = Color.green;

    public bool isVisible { get; private set; }

    public bool isMirrored { get; set; }
    public RotationAngle rotationAngle { get; set; } = RotationAngle.Rotation0;
    public Vector2Int imageSize { get; set; }

    private readonly List<RectTransform> _pointPool = new List<RectTransform>();

    private readonly object _targetLock = new object();
    private FaceDetectionResult _currentTarget;
    private bool _isStale;
    private int _activeKeypointCount;

    private void Awake()
    {
      if (_screenRectTransform == null)
      {
        _screenRectTransform = GetComponent<RectTransform>();
      }

      for (var i = 0; i < _maxPoints; i++)
      {
        _pointPool.Add(CreatePoint(i));
      }
    }

    private RectTransform CreatePoint(int index)
    {
      var go = new GameObject($"Keypoint_{index}", typeof(RectTransform), typeof(UIImage));
      var rt = (RectTransform)go.transform;
      rt.SetParent(transform, false);
      rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      rt.sizeDelta = new Vector2(_pointSize, _pointSize);

      var image = go.GetComponent<UIImage>();
      image.color = _pointColor;
      image.raycastTarget = false;

      go.SetActive(false);
      return rt;
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

    public void SetVisible(bool visible)
    {
      isVisible = visible;
      ApplyVisibility();
    }

    private void LateUpdate()
    {
      if (_isStale)
      {
        SyncNow();
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
        _activeKeypointCount = 0;
        ApplyVisibility();
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

      var keypoints = best.keypoints;
      var count = keypoints == null ? 0 : Mathf.Min(keypoints.Count, _pointPool.Count);

      for (var i = 0; i < count; i++)
      {
        var localPoint = _screenRectTransform.rect.GetPoint(keypoints[i], rotationAngle, isMirrored);
        _pointPool[i].anchoredPosition = localPoint;
      }

      _activeKeypointCount = count;
      ApplyVisibility();
    }

    private void ApplyVisibility()
    {
      for (var i = 0; i < _pointPool.Count; i++)
      {
        _pointPool[i].gameObject.SetActive(isVisible && i < _activeKeypointCount);
      }
    }
  }
}
