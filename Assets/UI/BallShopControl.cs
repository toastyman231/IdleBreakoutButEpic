using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BallShopControl : MonoBehaviour
{
    private Dictionary<string, string> ballTypeLookup;
    private Button[] _buyButtons;
    private string[] ballTypes;

    private UIDocument _uiDocument;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();

        ballTypeLookup = new Dictionary<string, string>();
        
        ballTypes = new [] { "BasicBall", "PlasmaBall", "SniperBall", "ScatterBall", "Cannonball", "PoisonBall" };
        _buyButtons = new Button[_uiDocument.rootVisualElement.Q("BallButtons").childCount];

        for (int i = 0; i < _buyButtons.Length; i++)
        {
            _buyButtons[i] = _uiDocument.rootVisualElement.Q<GroupBox>("Buy" + ballTypes[i] + "Group").Q<Button>("Buy" + ballTypes[i] + "Button");
            string button = _buyButtons[i].ToString();
            ballTypeLookup.Add(button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" ")), ballTypes[i]);
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
        //TODO: Check max ball limit and cash amount
        switch (ballTypeLookup[button.Substring(button.IndexOf(" "), button.IndexOf("(") - button.IndexOf(" "))])
        {
            case "BasicBall":
                NativeArray<BallType> types = new NativeArray<BallType>(1, Allocator.Temp);
                types[0] = BallType.BasicBall;
                NativeArray<int> amount = new NativeArray<int>(1, Allocator.Temp);
                amount[0] = 1;
                Unity.Entities.World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BallSpawnSystem>().SpawnBalls(ref types, ref amount);
                break;
            default:
                Debug.Log("You fucked something up");
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
