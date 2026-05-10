using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq; 

public class SpatialDigitalTwin : MonoBehaviour
{
    [Header("Server Connection")]
    public string serverIp = "192.168.100.1"; 
    public string port = "8000";
    
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
            yield return new WaitForSeconds(0.05f); 
        }
    }

    void UpdateUnityScene(string json) {
        try {
            JObject data = JObject.Parse(json);
            JObject mappings = data["mappings"] as JObject;
            JObject tags = data["tags"] as JObject;
            JObject draggingStates = data["is_dragging"] as JObject;
            JArray assets = data["assets"] as JArray;

            if (mappings == null || tags == null) return;

            foreach (var mapping in mappings) {
                string tagId = mapping.Key;
                int assetIndex = (int)mapping.Value;
                string targetName = (string)assets[assetIndex]["name"];
                GameObject targetObj = GameObject.Find(targetName);

                if (targetObj != null && tags.ContainsKey(tagId)) {
                    bool isDragging = (bool)draggingStates[tagId];
                    float tagX = (float)tags[tagId]["x"];
                    float tagZ = (float)tags[tagId]["z"];
                    // 높이값(y)이 필요하다면 추가, 없다면 기존 y 유지
                    float tagY = targetObj.transform.position.y; 

                    // 1. 하드웨어의 실시간 좌표 생성
                    Vector3 realWorldPos = new Vector3(tagX, tagY, tagZ);

                    // 2. 해당 오브젝트에 HardwareTagFollower 컴포넌트가 있는지 확인
                    HardwareTagFollower follower = targetObj.GetComponent<HardwareTagFollower>();

                    if (isDragging) {
                        // 버튼을 누르고 있을 때만 새로운 목표 좌표를 전달 (이때 애니메이션 발생)
                        if (follower != null) {
                            follower.UpdateHardwarePosition(realWorldPos);
                        } else {
                            // Follower 스크립트가 없는 일반 사물일 경우 기존 방식대로 이동
                            targetObj.transform.position = Vector3.Lerp(targetObj.transform.position, realWorldPos, Time.deltaTime * lerpSpeed);
                        }

                        // MPU 회전은 모든 객체 공통 적용
                        float ax = (float)tags[tagId]["ax"];
                        float az = (float)tags[tagId]["az"];
                        Quaternion targetRotation = Quaternion.Euler(ax * rotationSensitivity, 0, az * rotationSensitivity);
                        targetObj.transform.rotation = Quaternion.Lerp(targetObj.transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
                    }
                    else {
                        // 버튼을 떼면 follower의 목표 위치를 현재 위치로 고정시켜서 멈추게 함
                        if (follower != null) {
                            follower.UpdateHardwarePosition(targetObj.transform.position);
                        }
                    }
                }
            }
        } catch (System.Exception e) { }
    }
}