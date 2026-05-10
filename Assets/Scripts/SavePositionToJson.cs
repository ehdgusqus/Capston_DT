using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class ObjectData
{
    public string name;
    public double x; 
    public double y;
    public double z;
}

[System.Serializable]
public class ObjectDataList
{
    public List<ObjectData> items = new List<ObjectData>();
}

public class SavePositionToJson : MonoBehaviour
{
    private string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/SpawnedObjectsLog.json";
        LogPositionToJson();
    }

    void LogPositionToJson()
    {
        ObjectDataList dataList = new ObjectDataList();

        // 1. 기존 JSON 파일 읽어오기
        if (File.Exists(filePath))
        {
            string existingJson = File.ReadAllText(filePath);
            dataList = JsonUtility.FromJson<ObjectDataList>(existingJson);
            
            if (dataList == null)
            {
                dataList = new ObjectDataList();
            }
        }

        // 💡 2. 중복 이름 검사 (핵심 추가 부분)
        // 리스트(items) 안에 지금 저장하려는 객체와 똑같은 이름이 있는지 찾아서 그 번호(Index)를 가져옵니다.
        int existingIndex = dataList.items.FindIndex(item => item.name == gameObject.name);

        if (existingIndex != -1)
        {
            // 이미 같은 이름이 존재한다면? -> 배열에 새로 추가하지 않고, 기존 위치의 좌표값만 갱신합니다.
            dataList.items[existingIndex].x = (double)transform.position.x;
            dataList.items[existingIndex].y = (double)transform.position.y;
            dataList.items[existingIndex].z = (double)transform.position.z;
            
            Debug.Log($"[{gameObject.name}] 중복된 이름 발견! 기존 데이터의 좌표만 업데이트했습니다.");
        }
        else
        {
            // 같은 이름이 없다면? -> 새로운 데이터를 만들어서 배열에 추가합니다.
            ObjectData newData = new ObjectData();
            newData.name = gameObject.name;
            newData.x = (double)transform.position.x;
            newData.y = (double)transform.position.y;
            newData.z = (double)transform.position.z;

            dataList.items.Add(newData);
            Debug.Log($"[{gameObject.name}] 새로운 객체의 좌표가 JSON에 추가되었습니다.");
        }

        // 3. 파일로 다시 저장
        string finalJson = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(filePath, finalJson);
    }
}