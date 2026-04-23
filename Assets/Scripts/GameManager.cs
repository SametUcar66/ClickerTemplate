using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Properties;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [CreateProperty]
    public double TotalBread { get; set; } = 0;
    
    [CreateProperty]
    public double BreadPerClick { get; set; } = 1;
    
    [CreateProperty]
    public double BreadPerSecond { get; set; } = 0;

    public List<UpgradeData> Upgrades = new List<UpgradeData>();

    public event Action OnBreadChanged;
    public event Action<double> OnOfflineEarningsCalculated;

    private string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (BreadPerSecond > 0)
        {
            AddBread(BreadPerSecond * Time.deltaTime);
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    public void AddBread(double amount)
    {
        TotalBread += amount;
        OnBreadChanged?.Invoke();
    }

    public void ClickBread()
    {
        AddBread(BreadPerClick);
    }

    public bool TryBuyUpgrade(UpgradeData upgrade)
    {
        if (TotalBread >= upgrade.CurrentCost)
        {
            TotalBread -= upgrade.CurrentCost;
            upgrade.Purchase();
            OnBreadChanged?.Invoke();
            SaveGame();
            return true;
        }
        return false;
    }

    [Serializable]
    public class SaveData
    {
        public double TotalBread;
        public double BreadPerClick;
        public double BreadPerSecond;
        public string LastSaveTime;
        public List<UpgradeSaveData> Upgrades = new List<UpgradeSaveData>();
    }

    [Serializable]
    public class UpgradeSaveData
    {
        public string Name;
        public int Level;
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            TotalBread = this.TotalBread,
            BreadPerClick = this.BreadPerClick,
            BreadPerSecond = this.BreadPerSecond,
            LastSaveTime = DateTime.UtcNow.ToString()
        };

        foreach (var upgrade in Upgrades)
        {
            data.Upgrades.Add(new UpgradeSaveData { Name = upgrade.Name, Level = upgrade.Level });
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game Saved to " + SavePath);
    }

    public void LoadGame()
    {
        InitializeDefaultUpgrades();

        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            this.TotalBread = data.TotalBread;
            this.BreadPerClick = data.BreadPerClick;
            this.BreadPerSecond = data.BreadPerSecond;

            foreach (var upgradeSave in data.Upgrades)
            {
                var upgrade = Upgrades.Find(u => u.Name == upgradeSave.Name);
                if (upgrade != null)
                {
                    upgrade.Level = upgradeSave.Level;
                }
            }

            CalculateOfflineEarnings(data.LastSaveTime);
        }
    }

    private void InitializeDefaultUpgrades()
    {
        if (Upgrades.Count == 0)
        {
            Upgrades.Add(new UpgradeData { Name = "Baker", BaseCost = 15, BreadPerSecondBonus = 1 });
            Upgrades.Add(new UpgradeData { Name = "Oven", BaseCost = 100, BreadPerSecondBonus = 5 });
            Upgrades.Add(new UpgradeData { Name = "Flour Mill", BaseCost = 1100, BreadPerSecondBonus = 8 });
        }
    }

    private void CalculateOfflineEarnings(string lastSaveTimeString)
    {
        if (DateTime.TryParse(lastSaveTimeString, out DateTime lastSaveTime))
        {
            TimeSpan timeAway = DateTime.UtcNow - lastSaveTime;
            double secondsAway = timeAway.TotalSeconds;

            if (secondsAway > 0 && BreadPerSecond > 0)
            {
                double earned = secondsAway * BreadPerSecond;
                TotalBread += earned;
                OnOfflineEarningsCalculated?.Invoke(earned);
                Debug.Log($"Offline Earnings: {earned} bread earned over {secondsAway:F0} seconds.");
            }
        }
    }
}

[Serializable]
public class UpgradeData
{
    [CreateProperty]
    public string Name { get; set; }
    
    [CreateProperty]
    public double BaseCost { get; set; }
    
    [CreateProperty]
    public double CostMultiplier { get; set; } = 1.15;
    
    [CreateProperty]
    public double BreadPerClickBonus { get; set; } = 0;
    
    [CreateProperty]
    public double BreadPerSecondBonus { get; set; } = 0;
    
    [CreateProperty]
    public int Level { get; set; } = 0;

    [CreateProperty]
    public double CurrentCost => BaseCost * Math.Pow(CostMultiplier, Level);

    public void Purchase()
    {
        Level++;
        GameManager.Instance.BreadPerClick += BreadPerClickBonus;
        GameManager.Instance.BreadPerSecond += BreadPerSecondBonus;
    }
}
