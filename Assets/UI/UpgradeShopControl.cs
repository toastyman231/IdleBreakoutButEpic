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
    private Dictionary<string, Label> _labels;
    private Dictionary<string, Upgrade> _upgrades;
    private Dictionary<string, Button> _buttons;
    private List<VisualElement> _upgradeContainers;

    private GroupBox _upgradesPanel;

    private UIDocument _uiDocument;

    private void Start()
    {
        _labels = new Dictionary<string, Label>();
        _buttons = new Dictionary<string, Button>();
        _upgrades = new Dictionary<string, Upgrade>();
        _upgradeContainers = new List<VisualElement>();
        _uiDocument = GetComponent<UIDocument>();

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
                    
                    upgrade.RegisterCallback<ClickEvent, Action>(_upgrades[delKey].DeleteBall, OnUIShow);
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
        _upgrades.Add("ClickX", new Upgrade("ClickX", BallType.BasicBall, 50, 0.2f, Field.CLICKX, 1, int.MaxValue));

        _labels.Add("ClickX", _upgradesPanel.Q<GroupBox>("OtherUpgrades").Q<GroupBox>("ClickXUpgradeContainer").Q<Label>("UpgradeLabel"));
        _buttons.Add("ClickX", _upgradesPanel.Q<GroupBox>("OtherUpgrades").Q<GroupBox>("ClickXUpgradeContainer").Q<Button>("ClickXBuyButton"));
        _buttons["ClickX"].RegisterCallback<ClickEvent, UpgradeEventData>(_upgrades["ClickX"].LevelUp, new UpgradeEventData(_buttons["ClickX"], _labels["ClickX"]));
        
        _uiDocument.rootVisualElement.Q<Button>("CloseButton").RegisterCallback<ClickEvent>((type) =>
        {
            _uiDocument.rootVisualElement.Q<GroupBox>("BackgroundPanel").visible = false;
            OnUIHide();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BrickClickSystem>().CanClick = true;
        });
    }

    public void OnUIShow()
    {
        foreach (var upgradeBox in _upgradeContainers)
        {
            ToggleUpgradeBox(upgradeBox.name.Substring(0, upgradeBox.name.IndexOf("Upgrade")), upgradeBox);
        }
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
    }
}

public record UpgradeEventData(Button button, Label label);

public struct Upgrade
{
    private static Dictionary<string, int> _numUpgrades;
    
    private readonly string _name;
    private readonly BallType _ballType;
    private BigInteger _cost;
    private readonly float _increaseAmount;
    private readonly Field _stat;
    private int _currentLevel;
    private readonly int _maxLevel;
    private readonly int _stepSize;

    private World _world;

    public Upgrade(string name, BallType ballType, int cost, float increaseAmount, Field stat, int startLevel, int maxLevel, int stepSize = 1)
    {
        _name = name;
        _ballType = ballType;
        _cost = new BigInteger(cost);
        _increaseAmount = increaseAmount;
        _stat = stat;
        _currentLevel = startLevel;
        _maxLevel = maxLevel;
        _world = World.DefaultGameObjectInjectionWorld;
        _stepSize = stepSize;

        _numUpgrades ??= new Dictionary<string, int>();
        _numUpgrades.TryAdd(_ballType + name, 0);
    }

    public void LevelUp(ClickEvent evt, UpgradeEventData data)
    {
        if (CanBuy())
        {
            _currentLevel += _stepSize;
            _numUpgrades[_ballType + _name] += 1;

            if (_ballType == BallType.PlasmaBall)
            {
                // Slow plasma balls with more upgrades
                var plasmaBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlasmaTag>());
                var plasmaBalls = plasmaBallQuery.ToEntityArray(Allocator.Temp);
                int newSpeed = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(plasmaBalls[^1]).Speed;
                
                newSpeed = Mathf.CeilToInt(newSpeed * (0.9f / _numUpgrades[_ballType + _name]));
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

            data.label.text = _name + "\n" + _currentLevel + " >> " + (_currentLevel + _stepSize);
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

    public void DeleteBall(ClickEvent evt, Action onDelete)
    {
        EntityQuery query;
        int count = 0;
        
        switch (_ballType)
        {
            case BallType.BasicBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                break;
            case BallType.PlasmaBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;
                
                _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
                    .SetBallData(_ballType, false, -1, -1, "-1", count - 1, -1);
                _world.EntityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp)[0]);
                BallShopControl.InvokeBallUpdateEvent(_ballType, -1);
                onDelete();
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
                onDelete();
                return;
            case BallType.Cannonball:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CannonballTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;

                break;
            case BallType.PoisonBall:
                query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PoisonTag>());
                count = query.CalculateEntityCount();
                if (count == 0) return;

                break;
            default:
                Debug.Log("Error deleting ball!");
                return;
        }
        
        _world.GetOrCreateSystem<BallSharedDataUpdateSystem>()
            .SetBallData(_ballType, false, -1, -1, "-1", count - 1);
        _world.EntityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp)[0]);
        BallShopControl.InvokeBallUpdateEvent(_ballType, -1);
        onDelete();
    }
}
