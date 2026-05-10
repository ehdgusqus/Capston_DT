using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq; 

public class SpatialDigitalTwin : MonoBehaviour
{
    [Header("Server Connection")]
    public string serverIp = "192.168.100.1"; 
    public string port = "8000";
    
    [Header("Movement Constraints")]
    public bool useConstraints = true;
    public Vector2 minBounds = new Vector2(-15f, -10f); // 맵의 최소 X, Z
    public Vector2 maxBounds = new Vector2(15f, 10f);   // 맵의 최대 X, Z

    [Header("Sync Settings")]
    public float lerpSpeed = 15f;
    public float rotationSensitivity = 15f;

    private string syncUrl;

    void Start() {
        syncUrl = $"http://{serverIp}:{port}/api/sync";
        StartCoroutine(SyncLoop());
    }

    IEnumerator SyncLoop() {
        while (true) {
            using (UnityWebRequest request = UnityWebRequest.Get(syncUrl)) {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) {
                    UpdateUnityScene(request.downloadHandler.text);
                }
            }
            yield return new WaitForSeconds(0.05f); // 20Hz 동기화
        }
    }

    void UpdateUnityScene(string json) {
        try {
            JObject data = JObject.Parse(json);
            JObject mappings = data["mappings"] as JObject;
            JObject tags = data["tags"] as JObject;
            JObject draggingStates = data["is_dragging"] as JObject;
            JArray assets = data["assets"] as JArray;

            if (mappings == null || tags == null || assets == null) return;

            foreach (var mapping in mappings) {
                string tagId = mapping.Key;
                int assetIndex = (int)mapping.Value;
                string targetName = (string)assets[assetIndex]["name"];
                GameObject targetObj = GameObject.Find(targetName);

                if (targetObj != null && tags.ContainsKey(tagId)) {
                    // [데이터 추출]
                    bool isDragging = draggingStates.ContainsKey(tagId) ? (bool)draggingStates[tagId] : false;
                    float tagX = (float)tags[tagId]["x"];
                    float tagZ = (float)tags[tagId]["z"];

                    // --- 단계 1: 범위 제한 (Map Constraints) ---
                    if (useConstraints) {
                        tagX = Mathf.Clamp(tagX, minBounds.x, maxBounds.x);
                        tagZ = Mathf.Clamp(tagZ, minBounds.y, maxBounds.y);
                    }

                    Vector3 targetPos = new Vector3(tagX, targetObj.transform.position.y, tagZ);

                    // --- 단계 2: 디버그 로그 출력 ---
                    string logColor = isDragging ? "yellow" : "white";
                    Debug.Log($"<color={logColor}>[GPS] {tagId} -> {targetName} | X:{tagX:F2}, Z:{tagZ:F2} | Drag:{isDragging}</color>");

                    HardwareTagFollower follower = targetObj.GetComponent<HardwareTagFollower>();

                    // --- 단계 3: 물리적 이동 및 애니메이션 처리 ---
                    if (isDragging) {
                        if (follower != null) {
                            // 벽 뚫기 방지 로직이 포함된 Follower 스크립트 실행
                            follower.UpdateHardwarePosition(targetPos);
                        } else {
                            // 일반 물체: Raycast로 벽 체크 후 이동 (간이 물리)
                            MoveWithCollisionCheck(targetObj, targetPos);
                        }

                        // MPU 회전 적용
                        float ax = (float)tags[tagId]["ax"];
                        float az = (float)tags[tagId]["az"];
                        Quaternion targetRotation = Quaternion.Euler(ax * rotationSensitivity, 0, az * rotationSensitivity);
                        targetObj.transform.rotation = Quaternion.Lerp(targetObj.transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
                    }
                    else if (follower != null) {
                        // 드래그 중이 아닐 때 애니메이션 정지
                        follower.UpdateHardwarePosition(targetObj.transform.position);
                    }
                }
            }
        } catch { }
    }

    // 간단한 물리 체크: 이동 방향에 장애물이 있으면 멈춤
    void MoveWithCollisionCheck(GameObject obj, Vector3 target) {
        Vector3 direction = (target - obj.transform.position).normalized;
        float checkDistance = 0.5f; // 장애물 감지 거리

        // Raycast를 쏴서 장애물이 없을 때만 이동
        if (!Physics.Raycast(obj.transform.position + Vector3.up * 0.5f, direction, checkDistance)) {
            obj.transform.position = Vector3.Lerp(obj.transform.position, target, Time.deltaTime * lerpSpeed);
        }
    }
}