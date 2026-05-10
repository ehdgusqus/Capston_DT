using UnityEngine;
using UnityEditor;

public class ReplaceWithMeshColliderTool
{
    [MenuItem("Tools/모든 객체를 메쉬 콜라이더로 교체")]
    public static void ReplaceToMeshColliders()
    {
        // 씬 내의 형태(Mesh)를 가진 모든 오브젝트를 찾습니다.
        MeshFilter[] meshFilters = Object.FindObjectsOfType<MeshFilter>();
        int count = 0;

        foreach (MeshFilter filter in meshFilters)
        {
            GameObject go = filter.gameObject;

            // 1. 기존에 잘못 들어간 BoxCollider가 있다면 삭제합니다.
            BoxCollider oldBox = go.GetComponent<BoxCollider>();
            if (oldBox != null)
            {
                Object.DestroyImmediate(oldBox);
            }

            // 2. 이미 MeshCollider가 없다면 새로 추가합니다.
            if (go.GetComponent<MeshCollider>() == null)
            {
                MeshCollider meshCollider = go.AddComponent<MeshCollider>();
                // 오브젝트가 가진 실제 형태(Mesh)를 콜라이더에 똑같이 적용합니다.
                meshCollider.sharedMesh = filter.sharedMesh; 
                count++;
            }
        }

        Debug.Log($"작업 완료: 총 {count}개의 오브젝트에 형태와 똑같은 Mesh Collider를 적용했습니다.");
    }
}