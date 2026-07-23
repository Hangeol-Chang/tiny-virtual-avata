// Debug/runtime control panel for the avatar overlay scene, toggled with F1.
// Currently just wires the avatar scale slider; more controls get added here later.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.FaceDetection
{
  public class ControlPanelController : MonoBehaviour
  {
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private FaceAvatarController _faceAvatarController;
    [SerializeField] private Slider _avatarScaleSlider;
    [SerializeField] private Text _avatarScaleValueLabel;

    private void Start()
    {
      if (_avatarScaleSlider != null)
      {
        _avatarScaleSlider.minValue = FaceAvatarController.MinScale;
        _avatarScaleSlider.maxValue = FaceAvatarController.MaxScale;
        _avatarScaleSlider.wholeNumbers = false;

        if (_faceAvatarController != null)
        {
          _avatarScaleSlider.value = _faceAvatarController.Scale;
        }

        _avatarScaleSlider.onValueChanged.AddListener(OnAvatarScaleChanged);
        OnAvatarScaleChanged(_avatarScaleSlider.value);
      }

      if (_panelRoot != null)
      {
        _panelRoot.SetActive(false);
      }
    }

    private void Update()
    {
      var keyboard = Keyboard.current;
      if (keyboard == null || _panelRoot == null)
      {
        return;
      }

      if (keyboard.f1Key.wasPressedThisFrame)
      {
        _panelRoot.SetActive(!_panelRoot.activeSelf);
      }
    }

    private void OnAvatarScaleChanged(float value)
    {
      if (_faceAvatarController != null)
      {
        _faceAvatarController.Scale = value;
      }

      if (_avatarScaleValueLabel != null)
      {
        _avatarScaleValueLabel.text = $"Avatar Scale: {value:0.00}";
      }
    }
  }
}
