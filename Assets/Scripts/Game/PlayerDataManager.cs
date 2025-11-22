using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 玩家数据管理器
/// 位置：Assets/Scripts/Game/PlayerDataManager.cs
/// 功能：管理玩家数据的保存和加载
/// </summary>
[System.Serializable]
public class PlayerSaveData
{
    public string playerName = "";
    public int avatarId = 0;
    public int money = 20;
    public int debt = 0;
    public List<JokerData> jokers = new List<JokerData>();
}

public class PlayerDataManager : MonoBehaviour
{
    private const string SAVE_FILE_NAME = "playerdata.json";
    private string savePath;

    public static PlayerDataManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    public void SavePlayerData(PlayerSaveData data)
    {
        if (data == null)
            return;

        try
        {
            // 将JokerData列表转换为可序列化的格式
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"玩家数据已保存: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存玩家数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载玩家数据
    /// </summary>
    public PlayerSaveData LoadPlayerData()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("玩家数据文件不存在");
            return null;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
            
            // 恢复JokerData列表
            if (data.jokers == null)
                data.jokers = new List<JokerData>();

            Debug.Log($"玩家数据已加载: {savePath}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载玩家数据失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 删除玩家数据
    /// </summary>
    public void DeletePlayerData()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("玩家数据已删除");
        }
    }
}

