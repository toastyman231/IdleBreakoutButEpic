using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class BrickLocationSaver : EditorWindow
{
    private World _world;

    [MenuItem("Tools/BrickLocationSaver")]
    public static void ShowEditorWindow()
    {
        BrickLocationSaver wnd = GetWindow<BrickLocationSaver>();
        wnd.titleContent = new GUIContent("Brick Location Saver");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement saveLabel = new Label("Press to save current Bricks to Scriptable Object asset.");
        saveLabel.name = "saveLabel";
        root.Add(saveLabel);

        TextField saveInput = new TextField("Enter Layout Name:");
        root.Add(saveInput);

        Button saveButton = new Button();
        saveButton.name = "saveButton";
        saveButton.text = "Save Bricks";
        saveButton.clicked += () => SaveLayout(saveInput.value);
        //saveButton.RegisterCallback<ClickEvent, string>(SaveLayout(callback, saveInput.value));
        root.Add(saveButton);

        TextField loadInput = new TextField("Enter Layout Name:");
        root.Add(loadInput);
        
        Button loadButton = new Button();
        loadButton.name = "loadButton";
        loadButton.text = "Load Bricks";
        loadButton.clicked += () => LoadLayout(loadInput.value);
        root.Add(loadButton);
    }

    private void SaveLayout(string layoutName)
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Cannot save layouts outside play mode!");
            return;
        }
        if (layoutName == "")
        {
            Debug.Log("Must enter Layout Name!");
            return;
        }
        
        _world = World.DefaultGameObjectInjectionWorld;
        List<float3> positionsList = new List<float3>();

        BrickPositionData brickPositions = CreateInstance<BrickPositionData>();
        brickPositions.positions = _world.GetOrCreateSystem<LevelControlSystem>().GetBrickPositions(positionsList);
        
        AssetDatabase.CreateAsset(brickPositions, "Assets/Resources/Brick Layouts/" + layoutName + ".asset");
    }

    private void LoadLayout(string layoutName)
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Cannot save layouts outside play mode!");
            return;
        }
        if (layoutName == "")
        {
            Debug.Log("Must enter Layout Name!");
            return;
        }
        
        _world = World.DefaultGameObjectInjectionWorld;
        _world.GetOrCreateSystem<LevelControlSystem>().UnloadLevel();

        BrickPositionData brickPositions = Resources.Load<BrickPositionData>("Brick Layouts/" + layoutName);
        
        if (brickPositions == null)
        {
            Debug.Log("Could not find specified layout!");
            return;
        }
        
        _world.GetOrCreateSystem<LevelControlSystem>().LoadLevel(brickPositions.positions);
    }
}