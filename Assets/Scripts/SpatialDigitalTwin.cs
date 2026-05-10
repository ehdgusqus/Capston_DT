using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq; 

public class SpatialDigitalTwin : MonoBehaviour
{
    [Header("Server Connection")]
    public string serverIp = "192.168.100.1"; // ASUS 노트북 IP
    public string port = "8000";
    
    [Header("Sync Settings")]
    public float lerpSpeed = 15f;           // 위치/회전이 따라오는 부드러운 정도
    public float rotationSensitivity = 15f; // MPU9250 가속도 값에 대한 회전 민감도

    private string syncUrl;

    void Start() {
        // 서버 API 주소 설정
        syncUrl = $"http://{serverIp}:{port}/api/sync";
        Debug.Log($"<color=cyan>[SYSTEM]</color> API Sync URL: {syncUrl}");
        
        // 실시간 통신 루프 시작
        StartCoroutine(SyncLoop());
    }

    // 서버와 지속적으로 통신하는 코루틴
    IEnumerator SyncLoop() {
        while (true) {
            using (UnityWebRequest request = UnityWebRequest.Get(syncUrl)) {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success) {
                    UpdateUnityScene(request.downloadHandler.text);
                } else {
                    Debug.LogWarning($"<color=red>[ERROR]</color> Connection Failed: {request.error}");
                }
            }
            // 0.05초 대기 (초당 20번 업데이트 = 20Hz)
            yield return new WaitForSeconds(0.05f); 
        }
    }

    void UpdateUnityScene(string json) {
        // [원본 로그] 서버에서 오는 전체 JSON 데이터를 보고 싶을 때 활성화
        Debug.Log("<color=grey>[JSON FULL]</color> " + json);

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

                // 1. 매핑된 에셋 정보 확인
                if (assetIndex >= assets.Count) continue;
                string targetName = (string)assets[assetIndex]["name"];
                GameObject targetObj = GameObject.Find(targetName);

                if (targetObj != null && tags.ContainsKey(tagId)) {
                    // 2. 실시간 데이터 추출 (좌표 및 버튼 상태)
                    bool isDragging = draggingStates.ContainsKey(tagId) ? (bool)draggingStates[tagId] : false;
                    float tagX = (float)tags[tagId]["x"];
                    float tagZ = (float)tags[tagId]["z"];
                    Vector3 realWorldPos = new Vector3(tagX, targetObj.transform.position.y, tagZ);

                    // [디버그 로그] 버튼 상태에 따라 색상 변경 (노랑=이동중, 하양=대기중)
                    string logColor = isDragging ? "yellow" : "white";
                    Debug.Log($"<color={logColor}>[GPS] {tagId} -> {targetName} | X:{tagX:F2}, Z:{tagZ:F2} | Drag:{isDragging}</color>");

                    // 3. 캐릭터 애니메이션 컴포넌트(HardwareTagFollower) 확인
                    HardwareTagFollower follower = targetObj.GetComponent<HardwareTagFollower>();

                    // 4. 버튼 상태(isDragging)에 따른 인터랙션 처리
                    if (isDragging) {
                        // --- [이동 로직] ---
                        if (follower != null) {
                            // 캐릭터일 경우: 애니메이션 컨트롤러에 좌표 전달 (걸어감)
                            follower.UpdateHardwarePosition(realWorldPos);
                        } else {
                            // 일반 물체일 경우: 직접 Lerp 이동
                            targetObj.transform.position = Vector3.Lerp(targetObj.transform.position, realWorldPos, Time.deltaTime * lerpSpeed);
                        }

                        // --- [MPU9250 회전 로직] ---
                        float ax = (float)tags[tagId]["ax"];
                        float az = (float)tags[tagId]["az"];
                        // 가속도 센서 값을 회전 각도로 변환
                        Quaternion targetRotation = Quaternion.Euler(ax * rotationSensitivity, 0, az * rotationSensitivity);
                        targetObj.transform.rotation = Quaternion.Lerp(targetObj.transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
                    }
                    else {
                        // 버튼을 떼면 캐릭터의 목표 위치를 현재 위치로 세팅하여 '정지 애니메이션' 유도
                        if (follower != null) {
                            follower.UpdateHardwarePosition(targetObj.transform.position);
                        }
                    }
                }
            }
        } catch (System.Exception e) {
            // 초기 매핑 전 데이터 부재로 인한 에러 방지
        }
    }
}