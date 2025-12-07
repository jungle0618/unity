using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Area Manager - Manages Guard Areas and Safe Areas
/// Guard Area: Enemies always attack on sight
/// Safe Area: Enemies only attack if player has weapon equipped or danger level is triggered
/// </summary>
public class AreaManager : MonoBehaviour
{
    public static AreaManager Instance { get; private set; }
    
    [Header("Guard Areas")]
    [SerializeField] private List<GuardAreaDefinition> guardAreas = new List<GuardAreaDefinition>();
    
    [Header("Predefined Areas")]
    [SerializeField] private bool usePredefinedAreas = true;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color guardAreaColor = new Color(1f, 0f, 0f, 0.3f); // Red transparent
    
    [Header("Map UI Visualization")]
    [SerializeField] private bool showOnMapUI = true;
    
    [Header("Player Area Tracking")]
    [SerializeField] private bool showPlayerAreaInConsole = true;
    [SerializeField] private float areaCheckInterval = 1f; // Check every second
    
    private List<GameObject> mapAreaMarkers = new List<GameObject>();
    private Player player;
    private string lastAreaType = "";
    private float lastAreaCheckTime = 0f;
    private bool lastGuardAreaSystemState = true; // Track if guard area system was enabled
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[AreaManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Add predefined guard areas if enabled
        if (usePredefinedAreas && guardAreas.Count == 0)
        {
            InitializePredefinedAreas();
        }
        
        // Visualize on map UI (only if guard area system is enabled)
        if (showOnMapUI && IsGuardAreaSystemEnabled())
        {
            VisualizeAreasOnMap();
        }
        
        // Find player - try multiple methods
        FindPlayer();
    }
    
    /// <summary>
    /// Find player reference
    /// </summary>
    private void FindPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogWarning("[AreaManager] Player not found - will retry in Update");
            }
        }
    }
    
    private void Update()
    {
        // Try to find player if not found
        if (player == null)
        {
            FindPlayer();
        }
        
        // Check if guard area system state changed
        bool currentGuardAreaSystemState = IsGuardAreaSystemEnabled();
        if (currentGuardAreaSystemState != lastGuardAreaSystemState)
        {
            lastGuardAreaSystemState = currentGuardAreaSystemState;
            
            if (currentGuardAreaSystemState)
            {
                // System enabled - show visualizations
                if (showOnMapUI)
                {
                    if (mapAreaMarkers.Count > 0)
                    {
                        SetVisualizationVisibility(true);
                    }
                    else
                    {
                        VisualizeAreasOnMap();
                    }
                }
            }
            else
            {
                // System disabled - hide visualizations
                SetVisualizationVisibility(false);
            }
        }
        
        // Show player's current area in console
        if (showPlayerAreaInConsole && player != null && Time.time >= lastAreaCheckTime + areaCheckInterval)
        {
            lastAreaCheckTime = Time.time;
            
            Vector3 playerPos = player.transform.position;
            string currentAreaType = GetAreaTypeName(playerPos);
            
            // Only log when area changes
            if (currentAreaType != lastAreaType)
            {
                if (IsInGuardArea(playerPos))
                {
                    Debug.LogWarning($"[AreaManager] ⚠️ Player entered GUARD AREA at {playerPos}");
                }
                else
                {
                    //Debug.Log($"[AreaManager] ✓ Player in SAFE AREA at {playerPos}");
                }
                lastAreaType = currentAreaType;
            }
        }
    }
    
    /// <summary>
    /// Initialize predefined guard areas based on game map
    /// </summary>
    private void InitializePredefinedAreas()
    {
        // Clear existing areas
        guardAreas.Clear();
        
        // Define guard areas based on typical game coordinates
        // Adjust these based on your actual map layout
        
        // Guard Area 1: Central Guard Zone
        guardAreas.Add(new GuardAreaDefinition
        {
            enabled = true,
            areaName = "Central Guard Zone",
            center = new Vector2(74f, 28f),
            size = new Vector2(46f, 54f)
        });
        
        //Debug.Log($"[AreaManager] Initialized {guardAreas.Count} predefined guard areas");
    }
    
    /// <summary>
    /// Visualize guard areas on the map UI
    /// </summary>
    private void VisualizeAreasOnMap()
    {
        MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
        if (mapUI == null)
        {
            Debug.LogWarning("[AreaManager] MapUIManager not found - cannot visualize areas on map");
            return;
        }
        
        // Clear old markers
        foreach (var marker in mapAreaMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        mapAreaMarkers.Clear();
        
        // Create visual markers for each guard area
        foreach (var area in guardAreas)
        {
            if (area.enabled)
            {
                CreateMapAreaMarker(area, mapUI);
            }
        }
    }
    
    /// <summary>
    /// Create a visual marker for a guard area on the map UI
    /// </summary>
    private void CreateMapAreaMarker(GuardAreaDefinition area, MapUIManager mapUI)
    {
        GameObject areaOverlay = new GameObject($"GuardArea_{area.areaName}");
        
        // Find container - try common names first
        Transform container = mapUI.transform.Find("MarkersContainer") 
                           ?? mapUI.transform.Find("markersContainer")
                           ?? FindContainerViaReflection(mapUI);
        
        if (container == null)
        {
            container = mapUI.transform;
        }
        
        areaOverlay.transform.SetParent(container, false);
        
        // Add RectTransform
        RectTransform rectTransform = areaOverlay.AddComponent<RectTransform>();
        
        // Add Image component for visual
        UnityEngine.UI.Image image = areaOverlay.AddComponent<UnityEngine.UI.Image>();
        image.sprite = null; // Unity will use default white sprite
        image.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
        image.raycastTarget = false; // Don't block clicks
        
        // Convert world position to map position
        Vector2 mapPos = ConvertWorldToMapPosition(area.center, mapUI);
        // Vector2 mapSize = area.size * 2f; // Same scale as position conversion
        // Get the corners of the area in world space
        Vector2 worldMin = area.center - area.size / 2f;
        Vector2 worldMax = area.center + area.size / 2f;

        // Convert both corners to map space
        Vector2 mapMin = ConvertWorldToMapPosition(worldMin, mapUI);
        Vector2 mapMax = ConvertWorldToMapPosition(worldMax, mapUI);

        // Calculate the size in map space
        Vector2 mapSize = new Vector2(
            Mathf.Abs(mapMax.x - mapMin.x),
            Mathf.Abs(mapMax.y - mapMin.y)
        );
        // Set anchors and pivot
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Set position and size
        rectTransform.anchoredPosition = mapPos;
        rectTransform.sizeDelta = mapSize;
        
        // Track for cleanup
        mapAreaMarkers.Add(areaOverlay);
    }
    
    /// <summary>
    /// Find container via reflection (fallback method)
    /// </summary>
    private Transform FindContainerViaReflection(MapUIManager mapUI)
    {
        // Try markersContainer field
        var markersField = mapUI.GetType().GetField("markersContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (markersField != null)
        {
            Transform container = markersField.GetValue(mapUI) as Transform;
            if (container != null) return container;
        }
        
        // Try mapContainer field
        var mapContainerField = mapUI.GetType().GetField("mapContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (mapContainerField != null)
        {
            RectTransform container = mapContainerField.GetValue(mapUI) as RectTransform;
            if (container != null) return container;
        }
        
        return null;
    }
    
    /// <summary>
    /// Convert world position to map UI position
    /// </summary>
    private Vector2 ConvertWorldToMapPosition(Vector2 worldPos, MapUIManager mapUI)
    {
        // Try to use MapUIManager's WorldToMapPosition method via reflection
        var method = mapUI.GetType().GetMethod("WorldToMapPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            try
            {
                return (Vector2)method.Invoke(mapUI, new object[] { new Vector3(worldPos.x, worldPos.y, 0f) });
            }
            catch (System.Exception)
            {
                // Fall through to fallback
            }
        }
        // Fallback: simple conversion (adjust multiplier based on your map scale)
        return worldPos * 2f;
    }
    
    /// <summary>
    /// Check if a position is in a guard area
    /// </summary>
    public bool IsInGuardArea(Vector3 position)
    {
        foreach (var guardArea in guardAreas)
        {
            if (guardArea.enabled && guardArea.ContainsPoint(position))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Check if a position is in a safe area (not in any guard area)
    /// </summary>
    public bool IsInSafeArea(Vector3 position)
    {
        return !IsInGuardArea(position);
    }
    
    /// <summary>
    /// Add a guard area dynamically
    /// </summary>
    public void AddGuardArea(Vector2 center, Vector2 size, string areaName = "Guard Area")
    {
        GuardAreaDefinition newArea = new GuardAreaDefinition
        {
            enabled = true,
            areaName = areaName,
            center = center,
            size = size
        };
        guardAreas.Add(newArea);
    }
    
    /// <summary>
    /// Remove all guard areas
    /// </summary>
    public void ClearGuardAreas()
    {
        guardAreas.Clear();
    }
    
    /// <summary>
    /// Get area type name for a position
    /// </summary>
    public string GetAreaTypeName(Vector3 position)
    {
        return IsInGuardArea(position) ? "Guard Area" : "Safe Area";
    }
    
    /// <summary>
    /// Check if guard area system is enabled in GameSettings
    /// </summary>
    private bool IsGuardAreaSystemEnabled()
    {
        if (GameSettings.Instance != null)
        {
            return GameSettings.Instance.UseGuardAreaSystem;
        }
        // Default to enabled if settings not found
        return true;
    }
    
    /// <summary>
    /// Set visibility of all guard area visualizations
    /// </summary>
    private void SetVisualizationVisibility(bool visible)
    {
        foreach (var marker in mapAreaMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(visible);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw guard areas
        Gizmos.color = guardAreaColor;
        foreach (var guardArea in guardAreas)
        {
            if (guardArea.enabled)
            {
                // Draw filled rectangle
                Vector3 center3D = new Vector3(guardArea.center.x, guardArea.center.y, 0);
                Vector3 size3D = new Vector3(guardArea.size.x, guardArea.size.y, 0.1f);
                Gizmos.DrawCube(center3D, size3D);
                
                // Draw border
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center3D, size3D);
                Gizmos.color = guardAreaColor;
            }
        }
    }
}

/// <summary>
/// Guard Area Definition - Defines a rectangular guard area
/// </summary>
[System.Serializable]
public class GuardAreaDefinition
{
    public bool enabled = true;
    public string areaName = "Guard Area";
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(10f, 10f);
    
    /// <summary>
    /// Check if a point is inside this guard area (rectangular bounds)
    /// </summary>
    public bool ContainsPoint(Vector3 point)
    {
        Vector2 point2D = new Vector2(point.x, point.y);
        
        float minX = center.x - size.x / 2f;
        float maxX = center.x + size.x / 2f;
        float minY = center.y - size.y / 2f;
        float maxY = center.y + size.y / 2f;
        
        return point2D.x >= minX && point2D.x <= maxX &&
               point2D.y >= minY && point2D.y <= maxY;
    }
    
    /// <summary>
    /// Get bounds of this guard area
    /// </summary>
    public Bounds GetBounds()
    {
        return new Bounds(center, size);
    }
}


