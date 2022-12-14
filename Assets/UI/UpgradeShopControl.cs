using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Systems;
using Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradeShopControl : MonoBehaviour
{
    [SerializeField] private UIDocument prestigeDocument;
    [SerializeField] private BallShopControl ballShop;

    public static event EventHandler<NewBallEventData> NewBallEvent; 

    private Dictionary<string, Label> _labels;
    private Dictionary<string, Upgrade> _upgrades;
    private Dictionary<string, Button> _buttons;
    private List<VisualElement> _upgradeContainers;

    private GroupBox _upgradesPanel;
    private Button _upgradeButton;
    private Button _prestigeButton;

    private UIDocument _uiDocument;

    private void Start()
    {
        _labels = new Dictionary<string, Label>();
        _buttons = new Dictionary<string, Button>();
        _upgrades = new Dictionary<string, Upgrade>();
        _upgradeContainers = new List<VisualElement>();
        _uiDocument = GetComponent<UIDocument>();

        PrestigeShopControl.PrestigeEvent += ResetUpgrades;

        _upgradesPanel = _uiDocument.rootVisualElement.Q<GroupBox>("BackgroundPanel").Q<GroupBox>("UpgradesPanel");
        
        _upgrades.Add("BasicSpeed", new Upgrade("Speed", BallType.BasicBall, 100, 2.0f, Field.SPEED, 1, 10));
        _upgrades.Add("BasicPower", new Upgrade("Power", BallType.BasicBall, 250, 1.65f, Field.POWER, 1, int.MaxValue));
        _upgrades.Add("PlasmaRange", new Upgrade("Range", BallType.PlasmaBall, 1000, 2.5f, Field.RANGE, 1, 7));
        _upgrades.Add("PlasmaPower", new Upgrade("Power", BallType.PlasmaBall, 1250, 1.5f, Field.POWER, 3, int.MaxValue, 3));
        _upgrades.Add("SniperSpeed", new Upgrade("Speed", BallType.SniperBall, 7500, 1.75f, Field.SPEED, 4, 10));
        _upgrades.Add("SniperPower", new Upgrade("Power", BallType.SniperBall, 8000, 1.35f, Field.POWER, 5, int.MaxValue, 5));
        _upgrades.Add("ScatterExtraBalls", new Upgrade("Extra Balls", BallType.ScatterBall, 75000, 2.5f, Field.EXTRA, 1, 10));
        _upgrades.Add("ScatterPower", new Upgrade("Power", BallType.ScatterBall, 100000, 1.3f, Field.POWER, 10, int.MaxValue, 10));
        _upgrades.Add("CannonballSpeed", new Upgrade("Speed", BallType.Cannonball, 100000, 1.5f, Field.SPEED, 4, 16, 2));
        _upgrades.Add("CannonballPower", new Upgrade("Power", BallType.Cannonball, 150000, 1.25f, Field.POWER, 50, int.MaxValue, 25));
        _upgrades.Add("PoisonSpeed", new Upgrade("Speed", BallType.PoisonBall, 120000, 1.5f, Field.SPEED, 5, 15, 2));
        _upgrades.Add("PoisonPower", new Upgrade("Power", BallType.PoisonBall, 50000, 1.2f, Field.POWER, 5, int.MaxValue, 5));

        foreach (var upgradeBox in _upgradesPanel.Q<GroupBox>("BallUpgrades").Children())
        {
            _upgradeContainers.Add(upgradeBox);
            ToggleUpgradeBox(upgradeBox.name.Substring(0, upgradeBox.name.IndexOf("Upgrade")), upgradeBox);
            
            foreach (var upgrade in upgradeBox.Children())
            {
                if(upgrade.name.Contains("IGNORE")) continue;

                if (upgrade.name.Contains("DeleteButton"))
                {
                    string delKey = upgrade.name.Substring(0, upgrade.name.IndexOf("Delete"));
                    
                    upgrade.RegisterCallback<ClickEvent, Action<string>>(_upgrades[delKey].DeleteBall, OnDelete);
                    continue;
                }
                
                Button upgradeButton = upgrade.Q<Button>();
                string key = upgradeButton.name.Substring(0, upgradeButton.name.IndexOf("Buy"));
                _buttons.Add(key, upgradeButton);
                Label upgradeLabel = upgrade.Q<Label>();
                _labels.Add(key, upgradeLabel);
            
                _buttons[key].RegisterCallback<ClickEvent, UpgradeEventData>(_upgrades[key].LevelUp, new UpgradeEventData(upgradeButton, upgradeLabel));
            }
        }
        _upgrades.Add("ClickX", new Upgrade("ClickX", BallType.BasicBall, 50, 1.2f, Field.CLICKX, 1, int.MaxValue));

        _labels.Add("ClickX", _upgradesPanel.Q<GroupBox>("OtherUpgrades").Q<GroupBox>("ClickXUpgradeContainer").Q<Label>("UpgradeLabel"));
        _buttons.Add("ClickX", _upgradesPanel.Q<GroupBox>("OtherUpgrades").Q<GroupBox>("ClickXUpgradeContainer").Q<Button>("ClickXBuyButton"));
        _buttons["ClickX"].RegisterCallback<ClickEvent, UpgradeEventData>(_upgrades["ClickX"].LevelUp, new UpgradeEventData(_buttons["ClickX"], _labels["ClickX"]));
        
        _uiDocument.rootVisualElement.Q<Button>("CloseButton").RegisterCallback<ClickEvent>((type) =>
        {
            _uiDocument.rootVisualElement.Q<GroupBox>("BackgroundPanel").visible = false;
            OnUIHide();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = true;
        });

        _upgradeButton = _uiDocument.rootVisualElement.Q<Button>("UpgradesTabButton");
        _prestigeButton = _uiDocument.rootVisualElement.Q<Button>("PrestigeTabButton");
        _upgradeButton.RegisterCallback<ClickEvent, UIDocument>(SwitchToPanel, _uiDocument);
        _prestigeButton.RegisterCallback<ClickEvent, UIDocument>(SwitchToPanel, prestigeDocument);
        NewBallEvent += ReturnUpgrades;
    }

    private void OnDestroy()
    {
        _upgradeButton.UnregisterCallback<ClickEvent, UIDocument>(SwitchToPanel);
        _prestigeButton.UnregisterCallback<ClickEvent, UIDocument>(SwitchToPanel);
        NewBallEvent -= ReturnUpgrades;
        PrestigeShopControl.PrestigeEvent -= ResetUpgrades;
    }

    private void SwitchToPanel(ClickEvent evt, UIDocument document)
    {
        if (document.rootVisualElement.Q("BackgroundPanel").visible) return;

            _uiDocument.rootVisualElement.Q("BackgroundPanel").visible = false;
        prestigeDocument.rootVisualElement.Q("BackgroundPanel").visible = false;

        document.rootVisualElement.Q("BackgroundPanel").visible = true;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = false;

        if (document.name.ToLower().Contains("upgrade") && document.rootVisualElement.Q("BackgroundPanel").visible)
        {
            OnUIShow();
        }
        else
        {
            OnUIHide();
        }
    }

    public void OnUIShow()
    {
        foreach (var upgradeBox in _upgradeContainers)
        {
            ToggleUpgradeBox(upgradeBox.name.Substring(0, upgradeBox.name.IndexOf("Upgrade")), upgradeBox);
        }
        
        ToggleUpgradeBox("ClickX", _uiDocument.rootVisualElement.Q<GroupBox>("ClickXUpgradeContainer"));
    }

    private void OnDelete(string ballType)
    {
        OnUIShow();
        ballShop.ResetBall(ballType);
    }

    public void OnUIHide()
    {
        foreach (var upgradeBox in _upgradeContainers)
        {
            upgradeBox.visible = false;
        }
    }

    private void ToggleUpgradeBox(string ballType, VisualElement upgradeBox)
    {
        if (ballType == "ClickX")
        {
            _labels["ClickX"].text = _upgrades["ClickX"].Name + "\n" + _upgrades["ClickX"].GetCurrentLevel() +
                                     " >> " + (_upgrades["ClickX"].GetCurrentLevel() +
                                               _upgrades["ClickX"].GetStepSize());
            _buttons["ClickX"].text = (_upgrades["ClickX"].GetCurrentLevel() == _upgrades["ClickX"].GetMaxLevel()) ? "SOLD OUT" : "$" + _upgrades["ClickX"].GetCost().NumberFormat();
            return;
        }
        
        EntityQuery entityQuery;
        switch (ballType)
        {
            case "Basic":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<BallTag>());
                break;
            case "Plasma":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<PlasmaTag>());
                break;
            case "Sniper":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<SniperTag>());
                break;
            case "Scatter":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<ScatterTag>());
                break;
            case "Cannonball":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<CannonballTag>());
                break;
            case "Poison":
                entityQuery =
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<PoisonTag>());
                break;
            default:
                Debug.Log("Error toggling upgrade panel: " + ballType);
                return;
        }

        upgradeBox.visible = entityQuery.CalculateEntityCount() > 0;
        if (!upgradeBox.visible) return;

        foreach (var element in upgradeBox.Children())
        {
            if (element.name.Contains("Upgrade"))
            {
                string key = element.name.Substring(0, element.name.IndexOf("Upgrade"));

                element.Q<Label>("UpgradeLabel").text = _upgrades[key].Name + "\n" + _upgrades[key].GetCurrentLevel() +
                                                        " >> " + (_upgrades[key].GetCurrentLevel() +
                                                                  _upgrades[key].GetStepSize());
                element.Q<Button>().text = (_upgrades[key].GetCurrentLevel() == _upgrades[key].GetMaxLevel()) ? "SOLD OUT" : "$" + _upgrades[key].GetCost().NumberFormat();
            }
        }
    }

    private void ResetUpgrades(object sender, EventArgs args)
    {
        foreach (KeyValuePair<string, Upgrade> upgrade in _upgrades)
        {
            upgrade.Value.Reset();
        }
    }

    private void ReturnUpgrades(object sender, NewBallEventData args)
    {
        if (args.ballType == "Plasma")
        {
            BallSharedDataUpdateSystem updateSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSharedDataUpdateSystem>();
            Enum.TryParse<BallType>(args.ballType + "Ball", out var result);
            updateSystem.SetBallData(result, false, _upgrades[args.ballType + "Power"].GetCurrentLevel(), 
                -1, "-1", args.newAmount, _upgrades[args.ballType + "Range"].GetCurrentLevel());
        } else if (args.ballType == "Scatter")
        {
            BallSharedDataUpdateSystem updateSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSharedDataUpdateSystem>();
            Enum.TryParse<BallType>(args.ballType + "Ball", out var result);
            updateSystem.SetBallData(result, false, _upgrades[args.ballType + "Power"].GetCurrentLevel(), 
                -1, "-1", args.newAmount, _upgrades[args.ballType + "ExtraBalls"].GetCurrentLevel());
        } else if (args.ballType == "Cannonball")
        {
            BallSharedDataUpdateSystem updateSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSharedDataUpdateSystem>();
            Enum.TryParse<BallType>(args.ballType, out var result);
            updateSystem.SetBallData(result, false, _upgrades[args.ballType + "Power"].GetCurrentLevel(), 
                _upgrades[args.ballType + "Speed"].GetCurrentLevel(), "-1", args.newAmount);
        }
        else
        {
            BallSharedDataUpdateSystem updateSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSharedDataUpdateSystem>();
            Enum.TryParse<BallType>(args.ballType + "Ball", out var result);
            updateSystem.SetBallData(result, false, _upgrades[args.ballType + "Power"].GetCurrentLevel(), 
                _upgrades[args.ballType + "Speed"].GetCurrentLevel(), "-1", args.newAmount);
        }
    }

    public static void InvokeNewBallEvent(string ballType, int newAmount)
    {
        NewBallEvent?.Invoke(null, new NewBallEventData(ballType, newAmount));
    }
}

public record UpgradeEventData(Button button, Label label);

public record NewBallEventData(string ballType, int newAmount);

public class Upgrade
{
    private static Dictionary<string, int> _numUpgrades;
    
    public string Name { get; }

    private readonly BallType _ballType;
    private BigInteger _cost;
    private readonly BigInteger _initialCost;
    private readonly float _increaseAmount;
    private readonly Field _stat;
    private int _currentLevel;
    private readonly int _startLevel;
    private readonly int _maxLevel;
    private readonly int _stepSize;

    private World _world;

    public Upgrade(string name, BallType ballType, int cost, float increaseAmount, Field stat, int startLevel, int maxLevel, int stepSize = 1)
    {
        Name = name;
        _ballType = ballType;
        _cost = new BigInteger(cost);
        _initialCost = _cost;
        _increaseAmount = increaseAmount;
        _stat = stat;
        _currentLevel = startLevel;
        _startLevel = startLevel;
        _maxLevel = maxLevel;
        _world = World.DefaultGameObjectInjectionWorld;
        _stepSize = stepSize;

        _numUpgrades ??= new Dictionary<string, int>();
        _numUpgrades.TryAdd(_ballType + name, 0);
    }

    public void Reset()
    {
        _cost = _initialCost;
        _currentLevel = _startLevel;
        _numUpgrades.Clear();
        _numUpgrades.TryAdd(_ballType + Name, 0);
    }

    public void LevelUp(ClickEvent evt, UpgradeEventData data)
    {
        if (CanBuy())
        {
            _world.GetOrCreateSystem<MoneySystem>().Money = BigInteger.Subtract(_world.GetOrCreateSystem<MoneySystem>().Money, _cost);
            BallShopControl.InvokeIncreaseMoneyEvent();
            
            _currentLevel += _stepSize;
            _numUpgrades[_ballType + Name] += 1;

            if (_ballType == BallType.PlasmaBall)
            {
                // Slow plasma balls with more upgrades
                var plasmaBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlasmaTag>());
                var plasmaBalls = plasmaBallQuery.ToEntityArray(Allocator.Temp);
                int newSpeed = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(plasmaBalls[^1]).Speed;
                
                newSpeed = Mathf.CeilToInt(newSpeed * (0.9f / _numUpgrades[_ballType + Name]));
                _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                    .SetBallData(_ballType, false, -1, newSpeed, "-1", -1, -1);
            }

            switch (_stat)
            {
                case Field.POWER:
                    _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                        .SetBallData(_ballType, true, _stepSize, -1, "-1", -1, -1);
                    break;
                case Field.SPEED:
                    _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                        .SetBallData(_ballType, true, -1, _stepSize, "-1", -1, -1);
                    break;
                case Field.RANGE:
                    _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                        .SetBallData(_ballType, true, -1, -1, "-1", -1, _stepSize);
                    break;
                case Field.EXTRA:
                    _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                        .SetBallData(_ballType, true, -1, -1, "-1", -1, _stepSize);
                    break;
                case Field.CLICKX:
                    GlobalData globalData = _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().GetSingleton<GlobalData>();
                    _world.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(
                        new GlobalDataEventArgs{EventType = Field.CLICKX, NewData = globalData.ClickX + _stepSize});
                    break;
            }

            _cost = ReturnCostAsBigInteger(_cost, _increaseAmount);

            data.label.text = Name + "\n" + _currentLevel + " >> " + (_currentLevel + _stepSize);
            data.button.text = (_currentLevel == _maxLevel) ? "SOLD OUT" : "$" + _cost.NumberFormat();
        }
    }

    private bool CanBuy()
    {
        return (_world.GetOrCreateSystem<MoneySystem>().Money >= _cost && _currentLevel < _maxLevel);
    }
    
    private BigInteger ReturnCostAsBigInteger(BigInteger cost, float mult)
    {
        float dCost = (float)cost;
        int result = Mathf.CeilToInt(dCost * mult);
        return new BigInteger(result);
    }

    public void DeleteBall(ClickEvent evt, Action<string> onDelete)
    {
        EntityQuery query;
        int count = 0;

        switch (_ballType)
        {
            case BallType.BasicBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                //onDelete("BasicBall");
                break;
            case BallType.PlasmaBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                
                _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                    .SetBallData(_ballType, false, -1, -1, "-1", count - 1, -1);
                _world.EntityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp)[0]);
                BallShopControl.InvokeBallUpdateEvent(_ballType, -1);
                onDelete("PlasmaBall");
                return;
            case BallType.SniperBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SniperTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;

                break;
            case BallType.ScatterBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                
                _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                    .SetBallData(_ballType, false, -1, -1, "-1", count - 1, -1);
                _world.EntityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp)[0]);
                BallShopControl.InvokeBallUpdateEvent(_ballType, -1);
                onDelete("ScatterBall");
                return;
            case BallType.Cannonball:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CannonballTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                //onDelete("Cannonball");
                break;
            case BallType.PoisonBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PoisonTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                //onDelete("PoisonBall");
                break;
            default:
                Debug.Log("Error deleting ball!");
                return;
        }
        
        _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
            .SetBallData(_ballType, false, -1, -1, "-1", count - 1);
        _world.EntityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp)[0]);
        BallShopControl.InvokeBallUpdateEvent(_ballType, -1);
        onDelete(_ballType.ToString());
    }

    public int GetCurrentLevel()
    {
        return _currentLevel;
    }

    public int GetStepSize()
    {
        return _stepSize;
    }

    public int GetMaxLevel()
    {
        return _maxLevel;
    }

    public BigInteger GetCost()
    {
        return _cost;
    }
}
