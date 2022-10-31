using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Systems;
using Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class BallShopControl : MonoBehaviour
{
    public static event EventHandler IncreaseMoneyEvent;
    public static event EventHandler<EventData> BallUpdateEvent;
    public static event EventHandler LevelLoadEvent;

    [SerializeField] private UIDocument upgradeDocument;

    private Dictionary<string, string> _ballTypeLookup;
    private Button[] _buyButtons;
    private Button _upgradeButton;
    private VisualElement[] _hoverElements;
    private VisualElement _uiBar;
    private Label _moneyText;
    private string[] _ballTypes;
    private World _world;
    private Queue<Action> _costIncreases;
    private EntityQuery _dataQuery;
    private EntityQuery _ballQuery;
    private MoneySystem _moneySystem;
    private Entity _dataEntity;

    private UIDocument _uiDocument;

    private void OnEnable()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        _costIncreases = new Queue<Action>();
        _uiDocument = GetComponent<UIDocument>();
        _moneySystem = _world.GetOrCreateSystem<MoneySystem>();
        _dataQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GlobalData>());
        _ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<BasicBallSharedData>());
        IncreaseMoneyEvent += UpdateButtons;
        IncreaseMoneyEvent += UpdateMoneyText;
        BallUpdateEvent += OnBallUpdate;
        LevelLoadEvent += OnLevelLoad;
        //_dataEntity = _dataQuery.GetSingletonEntity();

        _ballTypeLookup = new Dictionary<string, string>();
        
        _ballTypes = new [] { "BasicBall", "PlasmaBall", "SniperBall", "ScatterBall", "Cannonball", "PoisonBall" };
        _buyButtons = new Button[_uiDocument.rootVisualElement.Q("BallButtons").childCount];
        _hoverElements = new VisualElement[_buyButtons.Length];
        _uiBar = _uiDocument.rootVisualElement.ElementAt(0);
        _moneyText = _uiBar.Q<GroupBox>("MoneyDisplayContainer").Q<Label>("MoneyText");

        for (int i = 0; i < _buyButtons.Length; i++)
        {
            _buyButtons[i] = _uiBar.Q<GroupBox>("BallButtons").Q<GroupBox>("Buy" + _ballTypes[i] + "Group")
                .Q<Button>("Buy" + _ballTypes[i] + "Button");
            _hoverElements[i] = _uiDocument.rootVisualElement.Q<GroupBox>("HoverGroup")
                .Q<VisualElement>(_ballTypes[i] + "Hover");
            string button = _buyButtons[i].ToString();
            _ballTypeLookup.Add(button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" ")),
                _ballTypes[i]);
            _buyButtons[i].RegisterCallback<ClickEvent>(BuyBall);

            Enum.TryParse<BallType>(_ballTypes[i], out var result);
            int index = i;
            _buyButtons[i].RegisterCallback<MouseOverEvent, EventData>(UpdateTooltip, new EventData(result, index));
            _buyButtons[i].RegisterCallback<MouseOutEvent>((type) =>
            {
                _hoverElements[index].visible = false;
            });
        }

        _upgradeButton = _uiBar.Q<GroupBox>("StoreButtonsContainer").Q<Button>("UpgradesButton");
        _upgradeButton.RegisterCallback<ClickEvent, UIDocument>(ToggleUIPanel, upgradeDocument);

        InvokeIncreaseMoneyEvent();
    }

    private void ToggleUIPanel(ClickEvent evt, UIDocument document)
    {
        document.rootVisualElement.Q("BackgroundPanel").visible =
            !document.rootVisualElement.Q("BackgroundPanel").visible;
    }

    private void OnDestroy()
    {
        IncreaseMoneyEvent -= UpdateButtons;
        IncreaseMoneyEvent -= UpdateMoneyText;
        BallUpdateEvent -= OnBallUpdate;
        LevelLoadEvent -= OnLevelLoad;
        
        _upgradeButton.UnregisterCallback<ClickEvent, UIDocument>(ToggleUIPanel);

        foreach (var button in _buyButtons)
        {
            button.UnregisterCallback<ClickEvent>(BuyBall);
        }
    }

    private bool CanBuy(BallType type, out BigInteger cost)
    {
        int ballCount;
        try
        {
            ballCount = _ballQuery.CalculateEntityCount();
        }
        catch
        {
            ballCount = 0;
            
        }

        BasicBallSharedData ballData;

        switch (type)
        {
            case BallType.BasicBall:
                try
                {
                    ballData = _world.EntityManager.GetComponentData<BasicBallSharedData>(
                        _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>())
                            .ToEntityArray(Allocator.Temp)[0]);
                    cost = BigInteger.Parse(ballData.Cost.ToString());
                }
                catch
                {
                    cost = BigInteger.Parse(_buyButtons[0].text.Substring(1));
                }
                break;
            case BallType.PlasmaBall:
                try
                {
                    ballData = _world.EntityManager.GetComponentData<BasicBallSharedData>(
                        _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>())
                            .ToEntityArray(Allocator.Temp)[0]);
                    cost = BigInteger.Parse(ballData.Cost.ToString());
                }
                catch
                {
                    cost = BigInteger.Parse(_buyButtons[1].text.Substring(1));
                }
                break;
            case BallType.SniperBall:
                try
                {
                    ballData = _world.EntityManager.GetComponentData<BasicBallSharedData>(
                        _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SniperTag>())
                            .ToEntityArray(Allocator.Temp)[0]);
                    cost = BigInteger.Parse(ballData.Cost.ToString());
                }
                catch
                {
                    cost = BigInteger.Parse(_buyButtons[2].text.Substring(1));
                }
                break;
            case BallType.ScatterBall:
                try
                {
                    ballData = _world.EntityManager.GetComponentData<BasicBallSharedData>(
                        _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>())
                            .ToEntityArray(Allocator.Temp)[0]);
                    cost = BigInteger.Parse(ballData.Cost.ToString());
                }
                catch
                {
                    cost = BigInteger.Parse(_buyButtons[3].text.Substring(1));
                }
                break;
        }

        int maxBalls = 50;
        try
        {
            maxBalls = _moneySystem.GetSingleton<GlobalData>().MaxBalls;
        } catch {}

        return BigInteger.Compare(_moneySystem.Money, cost) >= 0 && ballCount < maxBalls;
    }

    private void BuyBall(ClickEvent evt)
    {
        string button = evt.target.ToString();
        string ballType =
            _ballTypeLookup[button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" "))];
        
        Enum.TryParse<BallType>(ballType, out var result);
        if (!CanBuy(result, out var cost)) return;

        _moneySystem.Money = BigInteger.Subtract(_moneySystem.Money, cost);
        InvokeIncreaseMoneyEvent();

        NativeArray<BallType> types = new NativeArray<BallType>(1, Allocator.Temp);
        NativeArray<int> amount = new NativeArray<int>(1, Allocator.Temp);
        amount[0] = 1; // Spawn 1 ball
        
        switch (ballType)
        {
            case "BasicBall":
                types[0] = BallType.BasicBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount, float3.zero);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                InvokeBallUpdateEvent(BallType.BasicBall, 1);
                break;
            case "PlasmaBall":
                types[0] = BallType.PlasmaBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount, float3.zero);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                InvokeBallUpdateEvent(BallType.PlasmaBall, 1);
                break;
            case "SniperBall":
                types[0] = BallType.SniperBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount, float3.zero);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                InvokeBallUpdateEvent(BallType.SniperBall, 1);
                break;
            case "ScatterBall":
                types[0] = BallType.ScatterBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount, float3.zero);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                InvokeBallUpdateEvent(BallType.ScatterBall, 1);
                break;
            default:
                Debug.Log("You fucked something up");
                break;
        }
    }

    private Action UpdateCosts(string ballType)
    {
        string cost = "0";
        BigInteger newCost = BigInteger.Zero;

        switch (ballType)
        {
            case "BasicBall":
                var basicBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<BallTag>());
                var basicBalls = basicBallQuery.ToEntityArray(Allocator.Temp);
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(basicBalls[^1]).Cost.ToString();
                newCost = BigInteger.Add(BigInteger.Parse(cost),
                    ReturnCostAsBigInteger(BigInteger.Parse(cost), 0.5f));
                _buyButtons[0].text = "$" + newCost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.BasicBall, false, -1, -1, newCost.ToString(), -1);
            case "PlasmaBall":
                var plasmaBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlasmaTag>());
                var plasmaBalls = plasmaBallQuery.ToEntityArray(Allocator.Temp);
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(plasmaBalls[^1]).Cost.ToString();
                newCost = BigInteger.Add(BigInteger.Parse(cost),
                    ReturnCostAsBigInteger(BigInteger.Parse(cost), 0.4f));
                _buyButtons[1].text = "$" + newCost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.PlasmaBall, false, -1, -1, newCost.ToString(), -1, -1);
            case "SniperBall":
                var sniperBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<SniperTag>());
                var sniperBalls = sniperBallQuery.ToEntityArray(Allocator.Temp);
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(sniperBalls[^1]).Cost.ToString();
                newCost = ReturnCostAsBigInteger(BigInteger.Parse(cost), 1.35f);
                _buyButtons[2].text = "$" + newCost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.SniperBall, false, -1, -1, newCost.ToString(), -1);
            case "ScatterBall":
                var scatterBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<ScatterTag>());
                var scatterBalls = scatterBallQuery.ToEntityArray(Allocator.Temp);
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(scatterBalls[^1]).Cost.ToString();
                newCost = ReturnCostAsBigInteger(BigInteger.Parse(cost), 1.35f);
                _buyButtons[3].text = "$" + newCost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.ScatterBall, false, -1, -1, newCost.ToString(), -1, -1);
            default:
                return () => Debug.Log("Unrecognized ball type!");
        }
    }

    private BigInteger ReturnCostAsBigInteger(BigInteger cost, float mult)
    {
        float dCost = (float)cost;
        int result = Mathf.CeilToInt(dCost * mult);
        return new BigInteger(result);
    }

    private void UpdateTooltip(MouseOverEvent evt, EventData data)
    {
        BasicBallSharedData ballData;
        EntityQuery ballQuery;
        int count = 0;

        switch (data.type)
        {
            case BallType.BasicBall:
                ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>());
                break;
            case BallType.PlasmaBall:
                ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>());
                break;
            case BallType.SniperBall:
                ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SniperTag>());
                break;
            case BallType.ScatterBall:
                ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>());
                break;
            default:
                Debug.Log("Default case");
                ballQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>());
                break;
        }
        
        count = ballQuery.CalculateEntityCount();
        VisualElement tooltip = _hoverElements[data.index];

        if (count > 0)
        {
            ballData = _world.EntityManager.GetComponentData<BasicBallSharedData>(
                ballQuery.ToEntityArray(Allocator.Temp)[0]);
            
            tooltip.Q<Label>("BallName").text = ballData.BallName.ToString();
            tooltip.Q<Label>("BallDesc").text = ballData.BallDesc.ToString();
            tooltip.Q<Label>("BallStat1").text = "SPEED: " + ballData.Speed;
            tooltip.Q<Label>("BallStat2").text = "POWER: " + ballData.Power;
        }
        
        // Always update count
        tooltip.Q<Label>("Count").text = "You have: " + count;

        tooltip.visible = true;
    }

    // Update is called once per frame
    void Update()
    {
        //_moneyText.text = _moneySystem.Money.NumberFormat();
        
        if (_costIncreases.Count > 0)
        {
            _costIncreases.Dequeue().Invoke();
        }
    }

    private void UpdateButtons(object sender, EventArgs args)
    {
        foreach (var button in _buyButtons)
        {
            Enum.TryParse<BallType>(button.text.Substring(1), out var result);
            if (CanBuy(result, out var cost))
            {
                button.SetEnabled(true);
            }
        }
    }

    private void UpdateMoneyText(object sender, EventArgs args)
    {
        _moneyText.text = _moneySystem.Money.NumberFormat();
    }

    private void OnBallUpdate(object sender, EventData args)
    {
        Label ballText = _uiBar.Q<Label>("BallLabel");
        GlobalData globalData = _moneySystem.GetSingleton<GlobalData>();
        int currentBalls = int.Parse(ballText.text.Substring(0, ballText.text.IndexOf("/")));
        ballText.text = currentBalls + args.index + "/" + globalData.MaxBalls + "\nBalls";
        ballText.style.color = currentBalls + args.index == globalData.MaxBalls ? new StyleColor(Color.red) : new StyleColor(Color.black);
    }

    private void OnLevelLoad(object sender, EventArgs args)
    {
        Label levelText = _uiBar.Q<VisualElement>("LevelContainer").Q<Label>("LevelText");
        GlobalData globalData = _moneySystem.GetSingleton<GlobalData>();

        levelText.text = "Level\n" + (globalData.CurrentLevel + 1);
    }

    public static void InvokeIncreaseMoneyEvent()
    {
        IncreaseMoneyEvent?.Invoke(null, EventArgs.Empty);
    }

    public static void InvokeBallUpdateEvent(BallType type, int amount)
    {
        BallUpdateEvent?.Invoke(null, new EventData(type, amount));
    }

    public static void InvokeLevelLoadEvent()
    {
        LevelLoadEvent?.Invoke(null, EventArgs.Empty);
    }
}

public record EventData(BallType type, int index);

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}
