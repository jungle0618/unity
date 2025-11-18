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
    [SerializeField] private Color mapGuardAreaColor = new Color(1f, 0f, 0f, 0.5f); // Red for map
    
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
    /// Find player using multiple methods
    /// </summary>
    private void FindPlayer()
    {
        // Method 1: Try FindFirstObjectByType
        player = FindFirstObjectByType<Player>();
        
        if (player != null)
        {
            Debug.Log($"[AreaManager] ✓ Player found via FindFirstObjectByType: {player.name}");
            return;
        }
        
        // Method 2: Try FindObjectOfType (older Unity versions)
        player = FindObjectOfType<Player>();
        
        if (player != null)
        {
            Debug.Log($"[AreaManager] ✓ Player found via FindObjectOfType: {player.name}");
            return;
        }
        
        // Method 3: Try finding by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                Debug.Log($"[AreaManager] ✓ Player found via tag: {player.name}");
                return;
            }
        }
        
        // Method 4: Search by name
        playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                Debug.Log($"[AreaManager] ✓ Player found via name: {player.name}");
                return;
            }
        }
        
        // If all methods fail, delay and try again
        Debug.LogWarning("[AreaManager] Player not found immediately - will retry in 0.5 seconds");
        Invoke(nameof(RetryFindPlayer), 0.5f);
    }
    
    /// <summary>
    /// Retry finding player after delay
    /// </summary>
    private void RetryFindPlayer()
    {
        player = FindFirstObjectByType<Player>();
        
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        if (player != null)
        {
            Debug.Log($"[AreaManager] ✓ Player found on retry: {player.name}");
        }
        else
        {
            Debug.LogError("[AreaManager] ❌ Player STILL not found after retry! Area tracking will not work.");
            Debug.LogError("[AreaManager] Make sure there is a GameObject with 'Player' component in the scene.");
        }
    }
    
    private void Update()
    {
        // Check if guard area system state changed
        bool currentGuardAreaSystemState = IsGuardAreaSystemEnabled();
        if (currentGuardAreaSystemState != lastGuardAreaSystemState)
        {
            lastGuardAreaSystemState = currentGuardAreaSystemState;
            
            if (currentGuardAreaSystemState)
            {
                // System enabled - show visualizations
                Debug.Log("[AreaManager] Guard area system enabled - showing visualizations");
                if (showOnMapUI)
                {
                    // If we have existing markers, show them; otherwise create new ones
                    if (mapAreaMarkers.Count > 0)
                    {
                        ShowAllVisualization();
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
                Debug.Log("[AreaManager] Guard area system disabled - hiding visualizations");
                HideAllVisualization();
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
                    Debug.Log($"[AreaManager] ✓ Player in SAFE AREA at {playerPos}");
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
            center = new Vector2(67f, 24f),
            size = new Vector2(20f, 16f)
        });
        
        // Guard Area 2: North Checkpoint
        guardAreas.Add(new GuardAreaDefinition
        {
            enabled = true,
            areaName = "North Checkpoint",
            center = new Vector2(80f, 50f),
            size = new Vector2(12f, 12f)
        });
        
        // Guard Area 3: Target Escape Route (near escape point)
        guardAreas.Add(new GuardAreaDefinition
        {
            enabled = true,
            areaName = "Meeting Room",
            center = new Vector2(44f, 11f),
            size = new Vector2(6f, 10f)
        });
        
        Debug.Log($"[AreaManager] Initialized {guardAreas.Count} predefined guard areas");
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
        
        Debug.Log($"[AreaManager] Found MapUIManager, will create markers for {guardAreas.Count} guard areas");
        
        // Clear old markers
        foreach (var marker in mapAreaMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        mapAreaMarkers.Clear();
        
        // Create visual markers for each guard area
        int totalMarkersCreated = 0;
        foreach (var area in guardAreas)
        {
            if (area.enabled)
            {
                int beforeCount = mapAreaMarkers.Count;
                CreateMapAreaMarker(area, mapUI);
                int afterCount = mapAreaMarkers.Count;
                int markersForThisArea = afterCount - beforeCount;
                totalMarkersCreated += markersForThisArea;
                Debug.Log($"[AreaManager] Created {markersForThisArea} markers for '{area.areaName}'");
            }
        }
        
        Debug.LogWarning($"[AreaManager] ✓ Visualization complete! Total markers created: {totalMarkersCreated} (tracked: {mapAreaMarkers.Count})");
    }
    
    /// <summary>
    /// Create a visual marker for a guard area on the map UI
    /// </summary>
    private void CreateMapAreaMarker(GuardAreaDefinition area, MapUIManager mapUI)
    {
        // Create a simple semi-transparent rectangle overlay
        GameObject areaOverlay = new GameObject($"GuardArea_{area.areaName}");
        
        // Try to find the correct container - look for MarkersContainer first
        Transform container = null;
        
        // Try multiple ways to find the markers container
        Transform markersContainerTransform = mapUI.transform.Find("MarkersContainer");
        if (markersContainerTransform == null)
        {
            markersContainerTransform = mapUI.transform.Find("markersContainer");
        }
        
        // Check if MapUIManager has a markersContainer field via reflection
        if (markersContainerTransform == null)
        {
            var field = mapUI.GetType().GetField("markersContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Transform fieldValue = field.GetValue(mapUI) as Transform;
                if (fieldValue != null)
                {
                    markersContainerTransform = fieldValue;
                }
            }
        }
        
        if (markersContainerTransform != null)
        {
            container = markersContainerTransform;
            Debug.Log($"[AreaManager] Using MarkersContainer for guard area overlay");
        }
        else
        {
            // Fallback to mapContainer
            var mapContainerField = mapUI.GetType().GetField("mapContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (mapContainerField != null)
            {
                RectTransform mapContainerRect = mapContainerField.GetValue(mapUI) as RectTransform;
                if (mapContainerRect != null)
                {
                    container = mapContainerRect;
                    Debug.Log($"[AreaManager] Using mapContainer for guard area overlay");
                }
            }
        }
        
        if (container == null)
        {
            // Last resort: use mapUI transform itself
            container = mapUI.transform;
            Debug.LogWarning($"[AreaManager] Could not find container, using MapUIManager transform");
        }
        
        areaOverlay.transform.SetParent(container, false);
        
        // Add RectTransform
        RectTransform rectTransform = areaOverlay.AddComponent<RectTransform>();
        
        // Add Image component for visual
        UnityEngine.UI.Image image = areaOverlay.AddComponent<UnityEngine.UI.Image>();
        
        // Use white sprite (Unity's default)
        image.sprite = null; // Unity will use default white sprite
        image.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red (lower opacity for better visibility)
        image.raycastTarget = false; // Don't block clicks
        
        // Convert world position to map position using MapUIManager's method
        Vector2 mapPos = ConvertWorldToMapPosition(area.center, mapUI);
        Vector2 mapSize = ConvertWorldToMapSize(area.size, mapUI);
        
        // Set anchors to stretch (0,0) to (0,0) so positioning is relative to parent
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Set position and size
        rectTransform.anchoredPosition = mapPos;
        rectTransform.sizeDelta = mapSize;
        
        // Track for cleanup
        mapAreaMarkers.Add(areaOverlay);
        
        Debug.LogWarning($"[AreaManager] Created guard area overlay for '{area.areaName}' at map pos {mapPos} with size {mapSize}");
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
                Vector2 result = (Vector2)method.Invoke(mapUI, new object[] { new Vector3(worldPos.x, worldPos.y, 0f) });
                Debug.Log($"[AreaManager] Converted world {worldPos} to map {result} using MapUIManager method");
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AreaManager] Failed to use MapUIManager's WorldToMapPosition: {e.Message}");
            }
        }
        
        // Fallback: simple conversion
        Debug.LogWarning($"[AreaManager] Using fallback conversion for position");
        return worldPos * 2f; // Adjust multiplier based on your map scale
    }
    
    /// <summary>
    /// Convert world size to map UI size
    /// </summary>
    private Vector2 ConvertWorldToMapSize(Vector2 worldSize, MapUIManager mapUI)
    {
        // Size conversion is typically just scaling
        // Use the same scale factor as position conversion
        return worldSize * 2f; // Adjust multiplier based on your map scale
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
        Debug.Log($"[AreaManager] Added guard area: {areaName} at {center} with size {size}");
    }
    
    /// <summary>
    /// Remove all guard areas
    /// </summary>
    public void ClearGuardAreas()
    {
        guardAreas.Clear();
        Debug.Log("[AreaManager] All guard areas cleared");
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
    /// Hide all guard area visualizations
    /// </summary>
    private void HideAllVisualization()
    {
        foreach (var marker in mapAreaMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(false);
            }
        }
        Debug.Log("[AreaManager] All guard area visualizations hidden");
    }
    
    /// <summary>
    /// Show all guard area visualizations
    /// </summary>
    private void ShowAllVisualization()
    {
        foreach (var marker in mapAreaMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(true);
            }
        }
        Debug.Log("[AreaManager] All guard area visualizations shown");
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
