using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AvataController : MonoBehaviour
{
    [SerializeField] private string imageFile = "Avata/avata_idle.png";
    [SerializeField] private MeshRenderer meshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        StartCoroutine(LoadAvatarImage());
    }

    IEnumerator LoadAvatarImage() {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, imageFile);

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(filePath))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("이미지 로드 실패: " + uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.position = Vector3.zero;

                Material mat = new Material(Shader.Find("FB/avata shader"));
                mat.SetTexture("_mainTexture", texture);

                MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
                renderer.material = mat;

                Debug.Log("이미지를 Quad에 렌더링 완료!");
            }
        }
    }
}