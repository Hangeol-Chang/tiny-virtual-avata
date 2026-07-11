// Makes the built standalone Windows player's window background transparent via the
// Desktop Window Manager, so only the avatar (rendered over a zero-alpha camera clear
// color) is visible, like a desktop overlay. Only takes effect in a Windows standalone
// build; the Editor Game view cannot be made transparent this way since it isn't a
// top-level OS window.
using UnityEngine;

namespace Mediapipe.Unity.Sample.FaceDetection
{
  public class TransparentWindow : MonoBehaviour
  {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct Margins
    {
      public int cxLeftWidth;
      public int cxRightWidth;
      public int cyTopHeight;
      public int cyBottomHeight;
    }

    private const int GwlExstyle = -20;
    private const int WsExLayered = 0x80000;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(System.IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(System.IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(System.IntPtr hWnd, ref Margins margins);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmIsCompositionEnabled(out bool enabled);
#endif

    private void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
      DwmIsCompositionEnabled(out var compositionEnabled);
      if (!compositionEnabled)
      {
        Debug.LogWarning("[TransparentWindow] Desktop Window Manager composition is disabled; the window cannot be made transparent.");
        return;
      }

      var hwnd = GetActiveWindow();
      var margins = new Margins { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
      DwmExtendFrameIntoClientArea(hwnd, ref margins);

      var exStyle = GetWindowLong(hwnd, GwlExstyle);
      SetWindowLong(hwnd, GwlExstyle, exStyle | WsExLayered);
#else
      Debug.Log("[TransparentWindow] Desktop window transparency only applies to a Windows standalone build; skipping in the Editor.");
#endif
    }
  }
}
