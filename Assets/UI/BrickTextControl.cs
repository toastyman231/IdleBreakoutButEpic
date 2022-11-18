using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class BrickTextControl : MonoBehaviour
{
    [SerializeField] private GameObject textPrefab;

    private List<GameObject> _textObjects;

    public static event EventHandler<TextData> CreateTextEvent;
    public static event EventHandler<TextData> UpdateTextEvent; 
    public static event EventHandler<TextData> DeleteTextEvent; 

    public static BrickTextControl Instance;

    public NativeQueue<TextEventData> EventQueue;
    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EventQueue = new NativeQueue<TextEventData>(Allocator.Persistent);
        CreateTextEvent += CreateBrickLabel;
        UpdateTextEvent += UpdateBrickLabel;
        DeleteTextEvent += DeleteBrickLabel;
        _textObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        while (EventQueue.TryDequeue(out var item))
        {
            if (item.Delete) DeleteTextEvent?.Invoke(this, new TextData(item.Position, item.Text.ToString()));
            else if (item.Update) UpdateTextEvent?.Invoke(this, new TextData(item.Position, item.Text.ToString()));
            else CreateTextEvent?.Invoke(this, new TextData(item.Position, item.Text.ToString()));
        }
    }

    private void OnDestroy()
    {
        EventQueue.Dispose();
        CreateTextEvent -= CreateBrickLabel;
        UpdateTextEvent -= UpdateBrickLabel;
        DeleteTextEvent -= DeleteBrickLabel;
    }

    private void CreateBrickLabel(object sender, TextData args)
    {
        GameObject text = Instantiate(textPrefab, args.position, Quaternion.identity);
        text.GetComponent<TextMeshProUGUI>().text = args.text;
        _textObjects.Add(text);
    }

    private void UpdateBrickLabel(object sender, TextData args)
    {
        foreach (var text in _textObjects)
        {
            if (text.transform.position == args.position)
            {
                text.GetComponent<TextMeshProUGUI>().text = args.text;
            }
        }
    }

    private void DeleteBrickLabel(object sender, TextData args)
    {
        int index = -1;
        foreach (var text in _textObjects)
        {
            if (text.transform.position == args.position)
            {
                index = _textObjects.IndexOf(text);
                Destroy(text);
            }
        }
        
        if (index >= 0) _textObjects.RemoveAt(index);
    }

    public void ClearLabelList()
    {
        foreach (var text in _textObjects)
        {
            Destroy(text);
        }
        _textObjects.Clear();
    }
}

public record TextData(Vector3 position, string text);

public struct TextEventData
{
    public bool Update;
    public bool Delete;
    public float3 Position;
    public int Text;
}
