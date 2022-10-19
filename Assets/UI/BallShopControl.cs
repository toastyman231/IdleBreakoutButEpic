using System;
using System.Collections;
using System.Collections.Generic;
using Systems;
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

    private UIDocument _uiDocument;

    private void OnEnable()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        _costIncreases = new Queue<Action>();
        _uiDocument = GetComponent<UIDocument>();

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
        switch (ballType)
        {
            case "BasicBall":
                NativeArray<BallType> types = new NativeArray<BallType>(1, Allocator.Temp);
                types[0] = BallType.BasicBall;
                NativeArray<int> amount = new NativeArray<int>(1, Allocator.Temp);
                amount[0] = 1;
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
        switch (ballType)
        {
            case "BasicBall":
                return () =>
                {
                    BasicBallSharedData currentData =
                        _world.GetOrCreateSystem<BallSpawnSystem>().GetSingleton<BasicBallSharedData>();
                    _world.GetOrCreateSystem<BallSharedDataUpdateSystem>().InvokeUpdateSharedDataEvent(
                        currentData.Power,
                        currentData.Speed, 
                        currentData.Cost + Mathf.CeilToInt((float)currentData.Cost / 2),
                        currentData.Count);
                };
            default:
                return () =>
                {
                    Debug.Log("Error passing ball type when updating costs");
                };
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
