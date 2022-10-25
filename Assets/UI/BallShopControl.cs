using System;
using System.Collections;
using System.Collections.Generic;
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
        int cost = 0;

        switch (ballType)
        {
            case "BasicBall":
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<BallTag>()).ToEntityArray(Allocator.Temp)[0]).Cost;
                cost = Mathf.CeilToInt(cost + cost * 0.5f);
                //Debug.Log(cost.NumberFormat());
                _buyButtons[0].text = "$" + cost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.BasicBall, false, -1, -1, cost, -1);
            case "PlasmaBall":
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<PlasmaTag>()).ToEntityArray(Allocator.Temp)[0]).Cost;
                cost = Mathf.CeilToInt(cost + cost * 0.4f);
                _buyButtons[1].text = "$" + cost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.PlasmaBall, false, -1, -1, cost, -1, -1);
            case "SniperBall":
                cost = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<BasicBallSharedData>(
                    World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<SniperTag>()).ToEntityArray(Allocator.Temp)[0]).Cost;
                cost = (int) Mathf.Floor(cost * 1.35f) + 1;
                //Debug.Log(cost.NumberFormat());
                _buyButtons[2].text = "$" + cost.NumberFormat();
                return () => _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().SetBallData(BallType.SniperBall, false, -1, -1, cost, -1);
            default:
                return () => Debug.Log("Unrecognized ball type!");
        }
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
