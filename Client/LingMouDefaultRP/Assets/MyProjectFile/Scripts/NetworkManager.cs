using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

[Serializable]
public class RequestData
{
    public float[] robot_pos;
    public string robot_face;
    public float[] crate_pos;
}

[Serializable]
public class ResponseData
{
    public string[] actions;
}

public class NetworkManager : MonoBehaviour
{
    // 修改为你的 Python 服务器地址
    private string serverUrl = "http://127.0.0.1:5000/ask_action";

    public IEnumerator SendRequest(Vector3 robotPos, Vector3 robotForward, Vector3 cratePos, Action<string[]> callback)
    {
        // 1. 整理数据
        RequestData data = new RequestData();
        // 简化坐标：假设是网格系统，取整
        data.robot_pos = new float[] { Mathf.Round(robotPos.x), Mathf.Round(robotPos.z) };
        data.crate_pos = new float[] { Mathf.Round(cratePos.x), Mathf.Round(cratePos.z) };
        data.robot_face = GetDirectionName(robotForward);

        string json = JsonUtility.ToJson(data);

        // 2. 发送 HTTP POST
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("发送给大模型: " + json);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("网络错误: " + www.error);
            }
            else
            {
                Debug.Log("大模型回复: " + www.downloadHandler.text);
                // 3. 解析回复
                ResponseData res = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);
                callback?.Invoke(res.actions);
            }
        }
    }

    // 辅助：把向量转为东南西北（简化版）
    private string GetDirectionName(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0 ? "East" : "West";
        else
            return dir.z > 0 ? "North" : "South";
    }
}