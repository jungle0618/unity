using UnityEngine;

/// <summary>
/// 地圖標記類別
/// 代表地圖上的一個標記點
/// </summary>
public class MapMarker : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image markerImage;
    [SerializeField] private TMPro.TextMeshProUGUI markerNameText;
    
    private Vector3 worldPosition;
    private string markerName;
    
    public Vector3 WorldPosition => worldPosition;
    public string MarkerName => markerName;
    
    public void SetWorldPosition(Vector3 position)
    {
        worldPosition = position;
    }
    
    public void SetMarkerName(string name)
    {
        markerName = name;
        if (markerNameText != null)
        {
            markerNameText.text = name;
        }
    }
    
    public void SetMarkerColor(Color color)
    {
        if (markerImage != null)
        {
            markerImage.color = color;
        }
    }
}

