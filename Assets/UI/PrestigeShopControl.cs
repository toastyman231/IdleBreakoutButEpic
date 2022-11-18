using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PrestigeShopControl : MonoBehaviour
{
    public static event EventHandler PrestigeEvent;
    
    [SerializeField] private UIDocument upgradeDocument;
    [SerializeField] private UpgradeShopControl upgradeControl;

    private GroupBox _upgradesBox;
    private Button _upgradeButton;
    private Button _prestigeButton;
    private Dictionary<string, PrestigeUpgrade> _upgrades;

    private UIDocument _uiDocument;
    // Start is called before the first frame update
    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        _upgrades = new Dictionary<string, PrestigeUpgrade>();
        
        _upgrades.Add("CashBonus", new PrestigeUpgrade("Level\nComplete\nCash Bonus", "CashBonus", 2, (i => BigInteger.Pow(2, i + 1)), 
            0, 10));
        _upgrades.Add("SpeedIncrease", 
            new PrestigeUpgrade("Ball Speed\nIncrease", "SpeedIncrease", 5, 
                i => (int)((5f/2f) * Mathf.Pow(2, i + 1)), 0, 9));
        _upgrades.Add("BallPower", 
            new PrestigeUpgrade("Ball Power\nMultiplier", "BallPower", 5, 
                i => (int)((5f/2f) * Mathf.Pow(2, i + 1)), 0, 18));
        _upgrades.Add("MaxBalls", 
            new PrestigeUpgrade("Maximum\nNumber of\nBalls", "MaxBalls", 4, i => (int)(i * 1.5f), 0, 32));
        
        _uiDocument.rootVisualElement.Q<Label>("GoldText").text = PlayerPrefs.GetString("gold", "0");
        
        _upgradeButton = _uiDocument.rootVisualElement.Q<Button>("UpgradesTabButton");
        _prestigeButton = _uiDocument.rootVisualElement.Q<Button>("PrestigeTabButton");
        _upgradeButton.RegisterCallback<ClickEvent, UIDocument>(SwitchToPanel, upgradeDocument);
        _prestigeButton.RegisterCallback<ClickEvent, UIDocument>(SwitchToPanel, _uiDocument);
        
        _uiDocument.rootVisualElement.Q<Button>("CloseButton").RegisterCallback<ClickEvent>((type) =>
        {
            _uiDocument.rootVisualElement.Q<GroupBox>("BackgroundPanel").visible = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = true;
        });

        _upgradesBox = _uiDocument.rootVisualElement.Q<GroupBox>("UpgradesBox"); 
        
        foreach (var upgradeBox in _upgradesBox.Children())
        {
            string key = upgradeBox.name.Substring(0, upgradeBox.name.IndexOf("Container"));
            Label upgradeLabel = upgradeBox.Q<Label>();
            upgradeLabel.text = _upgrades[key].GetLabelText();
            Button upgradeButton = upgradeBox.Q<Button>();
            upgradeButton.text = _upgrades[key].GetCost();
            upgradeButton.RegisterCallback<ClickEvent, UpgradeEventData>(_upgrades[key].LevelUp, new UpgradeEventData(upgradeButton, upgradeLabel));
        }
        
        _uiDocument.rootVisualElement.Q<Button>("ClaimButton").RegisterCallback<ClickEvent>(ClaimAndPrestige);
    }

    private void OnDestroy()
    {
        _upgradeButton.UnregisterCallback<ClickEvent, UIDocument>(SwitchToPanel);
        _prestigeButton.UnregisterCallback<ClickEvent, UIDocument>(SwitchToPanel);
        _uiDocument.rootVisualElement.Q<Button>("ClaimButton").UnregisterCallback<ClickEvent>(ClaimAndPrestige);
        
        foreach (var upgradeBox in _upgradesBox.Children())
        {
            string key = upgradeBox.name.Substring(0, upgradeBox.name.IndexOf("Container"));
            upgradeBox.Q<Button>().UnregisterCallback<ClickEvent, UpgradeEventData>(_upgrades[key].LevelUp);
        }
    }

    private void SwitchToPanel(ClickEvent evt, UIDocument document)
    {
        if (document.rootVisualElement.Q("BackgroundPanel").visible) return;

        _uiDocument.rootVisualElement.Q("BackgroundPanel").visible = false;
        upgradeDocument.rootVisualElement.Q("BackgroundPanel").visible = false;

        document.rootVisualElement.Q("BackgroundPanel").visible = true;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = false;

        if (document.name.ToLower().Contains("upgrade") && document.rootVisualElement.Q("BackgroundPanel").visible)
        {
            upgradeControl.OnUIShow();
        }
        else
        {
            upgradeControl.OnUIHide();
        }
    }

    private void ClaimAndPrestige(ClickEvent evt)
    {
        if (World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().GoldToClaim == 0) return;
        
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Gold +=
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().GoldToClaim;

        int prestiges = PlayerPrefs.GetInt("prestiges", 0);
        PlayerPrefs.SetInt("prestiges", prestiges + 1);
        PlayerPrefs.SetString("gold", World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Gold.ToString());
        PlayerPrefs.Save();

        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery destroyQuery = manager.CreateEntityQuery(ComponentType.ReadOnly<BasicBallSharedData>());
        //EntityQuery destroyBricks = manager.CreateEntityQuery(ComponentType.ReadOnly<BrickTag>());
        //manager.DestroyEntity(destroyBricks);
        manager.DestroyEntity(destroyQuery);
        BallShopControl.InvokeBallUpdateEvent(BallType.BasicBall, 0);
        
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Money = BigInteger.Zero;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().GoldToClaim = BigInteger.Zero;
        
        BallShopControl.InvokeIncreaseMoneyEvent();
        _uiDocument.rootVisualElement.Q<Label>("ClaimText").text = "CLAIM 0 GOLD ON RESET";
        
        Entity globalEntity = manager.CreateEntityQuery(ComponentType.ReadOnly<GlobalData>()).GetSingletonEntity();
        GlobalData globalData = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>()
            .GetSingleton<GlobalData>();
        manager.SetComponentData(globalEntity, new GlobalData
        {
            CashBonus = globalData.CashBonus,
            ClickX = 1,
            CurrentLevel =  1,
            GlobalSpeed = globalData.GlobalSpeed,
            MaxBalls = globalData.MaxBalls,
            PowerMultiplier = globalData.PowerMultiplier,
            SpeedScale = globalData.SpeedScale
        });
        
        BallShopControl.InvokeLevelLoadEvent();
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LevelControlSystem>().LoadLevel(Resources.Load<BrickPositionData>("Brick Layouts/Level1").positions);
        PrestigeEvent?.Invoke(this, EventArgs.Empty);
        
        _uiDocument.rootVisualElement.Q<GroupBox>("BackgroundPanel").visible = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = true;
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    // Update is called once per frame
    void Update()
    {
        if (_uiDocument.rootVisualElement.Q("BackgroundPanel").visible)
        {
            _uiDocument.rootVisualElement.Q<Label>("GoldText").text = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<MoneySystem>().Gold.ToString();
            _uiDocument.rootVisualElement.Q<Label>("ClaimText").text = "CLAIM " + World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<MoneySystem>().GoldToClaim + " GOLD ON RESET";

            int currentLevel = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>()
                .GetSingleton<GlobalData>().CurrentLevel;
            int nextGoldLevel = Mathf.CeilToInt((currentLevel / 20f)) * 20;

            _uiDocument.rootVisualElement.Q<Label>("NextLabel").text = "Next Gold at Level " + nextGoldLevel;
        }
    }
}

public class PrestigeUpgrade
{
    private readonly string _name;
    private readonly string _type;
    private BigInteger _cost;
    private int _currentLevel;
    private readonly int _startLevel;
    private readonly int _maxLevel;
    private Func<int, BigInteger> IncreaseCost;

    public PrestigeUpgrade(string name, string type, int initialCost, Func<int, BigInteger> increaseFunc, int startLevel, int maxLevel)
    {
        _name = name;
        _type = type;
        _currentLevel = PlayerPrefs.GetInt(type + "Level", startLevel);
        IncreaseCost = increaseFunc;
        _cost = (_type == "MaxBalls") ? PlayerPrefs.GetInt(type + "Cost", initialCost) : IncreaseCost(_currentLevel);
        _startLevel = startLevel;
        _maxLevel = maxLevel;
        if (_currentLevel == _startLevel) _cost = initialCost;
    }

    public string GetCost()
    {
        int cost = (_type == "MaxBalls") ? PlayerPrefs.GetInt(_type + "Cost", (int)_cost) : (int)IncreaseCost(_currentLevel);
        return _currentLevel == _maxLevel ? "SOLD OUT" : cost.ToString() + " GOLD";
    }

    public string GetLabelText()
    {
        int currentValue = 0;
        int nextLevelValue = 0;
        switch (_type)
        {
            case "CashBonus":
                currentValue = (int)(Mathf.Pow(2, _currentLevel - 2) * 100);
                if (_currentLevel == _startLevel) currentValue = 0;
                return _name + "\n" + currentValue + "%";
            case "SpeedIncrease":
                currentValue = _currentLevel + 1;
                nextLevelValue = _currentLevel + 2;
                if (_currentLevel == _startLevel) currentValue = 1; 
                if (_currentLevel == _startLevel) nextLevelValue = 2; 
                return _name + "\n" + currentValue + "x" + " >> " + nextLevelValue + "x";
            case "BallPower":
                currentValue = (int)Mathf.Pow(2, _currentLevel);
                nextLevelValue = (int)Mathf.Pow(2, _currentLevel + 1);
                return _name + "\n" + currentValue + "x" + " >> \n" + nextLevelValue + "x";
            case "MaxBalls":
                currentValue = 10 * _currentLevel + 50;
                nextLevelValue = 10 * (_currentLevel + 1) + 50;
                return _name + "\n" + currentValue + " >> " + nextLevelValue;
            default:
                return "Something went wrong calculating label text for " + _name;
        }
    }

    public void LevelUp(ClickEvent evt, UpgradeEventData data)
    {
        if (World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Gold < _cost || _currentLevel == _maxLevel) return;

        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Gold -= _cost;

        _currentLevel++;
        _cost = (_type == "MaxBalls") ? IncreaseCost((int)_cost) : IncreaseCost(_currentLevel);

        //GlobalData globalData = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>()
           // .GetSingleton<GlobalData>();

        int currentValue = 0;

        switch (_type)
        {
            case "CashBonus":
                currentValue = (int)(Mathf.Pow(2, _currentLevel - 2) * 100f);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(
                    new GlobalDataEventArgs{EventType = Field.CASHBONUS, NewData = currentValue});
                break;
            case "SpeedIncrease":
                currentValue = _currentLevel + 1;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(
                    new GlobalDataEventArgs{EventType = Field.SPEED, NewData = currentValue});
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.AllBalls, true, -1, 0, "-1", -1);
                break;
            case "BallPower":
                currentValue = (int)Mathf.Pow(2, _currentLevel);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(
                    new GlobalDataEventArgs{EventType = Field.POWER, NewData = currentValue});
                break;
            case "MaxBalls":
                currentValue = 10 * _currentLevel + 50;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(
                    new GlobalDataEventArgs{EventType = Field.BALLS, NewData = currentValue});
                break;
            default:
                Debug.Log("Error levelling up prestige upgrade: " + _name); 
                break;
        }
        
        PlayerPrefs.SetInt(_type + "Level", _currentLevel);
        if (_type == "MaxBalls") PlayerPrefs.SetInt(_type + "Cost", (int)_cost);
        PlayerPrefs.SetString("gold", World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<MoneySystem>().Gold.ToString());
        PlayerPrefs.Save();

        data.label.text = GetLabelText();
        data.button.text = GetCost();
    }
}
