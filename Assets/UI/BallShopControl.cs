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

public class BallShopControl : MonoBehaviour
{
    private Dictionary<string, string> _ballTypeLookup;
    private Button[] _buyButtons;
    private string[] _ballTypes;
    private World _world;
    private Queue<Action> _costIncreases;
    private EntityQuery _dataQuery;
    private Entity _dataEntity;

    private UIDocument _uiDocument;

    private void OnEnable()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        _costIncreases = new Queue<Action>();
        _uiDocument = GetComponent<UIDocument>();
        _dataQuery = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GlobalData>());
        //_dataEntity = _dataQuery.GetSingletonEntity();

        _ballTypeLookup = new Dictionary<string, string>();
        
        _ballTypes = new [] { "BasicBall", "PlasmaBall", "SniperBall", "ScatterBall", "Cannonball", "PoisonBall" };
        _buyButtons = new Button[_uiDocument.rootVisualElement.Q("BallButtons").childCount];

        for (int i = 0; i < _buyButtons.Length; i++)
        {
            _buyButtons[i] = _uiDocument.rootVisualElement.Q<GroupBox>("Buy" + _ballTypes[i] + "Group").Q<Button>("Buy" + _ballTypes[i] + "Button");
            string button = _buyButtons[i].ToString();
            _ballTypeLookup.Add(button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" ")), _ballTypes[i]);
            _buyButtons[i].RegisterCallback<ClickEvent>(BuyBall);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _buyButtons.Length; i++)
        {
            _buyButtons[i].UnregisterCallback<ClickEvent>(BuyBall);
        }
    }

    private void BuyBall(ClickEvent evt)
    {
        
        string button = evt.target.ToString();
        string ballType =
            _ballTypeLookup[button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" "))];
        //TODO: Check max ball limit and cash amount

        NativeArray<BallType> types = new NativeArray<BallType>(1, Allocator.Temp);
        NativeArray<int> amount = new NativeArray<int>(1, Allocator.Temp);
        amount[0] = 1; // Spawn 1 ball
        
        switch (ballType)
        {
            case "BasicBall":
                types[0] = BallType.BasicBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                break;
            case "PlasmaBall":
                types[0] = BallType.PlasmaBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount);
                _costIncreases.Enqueue(UpdateCosts(ballType));
                break;
            case "SniperBall":
                types[0] = BallType.SniperBall;
                _world.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount);
                _costIncreases.Enqueue(UpdateCosts(ballType));
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

    // Update is called once per frame
    void Update()
    {
        if (_costIncreases.Count > 0)
        {
            _costIncreases.Dequeue().Invoke();
        }
    }
}
