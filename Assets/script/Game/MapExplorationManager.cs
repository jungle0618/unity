using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地圖探索管理器
/// 記錄玩家探索過的區域，並在未探索區域顯示灰色遮罩
/// </summary>
[DefaultExecutionOrder(250)] // 在 GameManager 之後執行
public class MapExplorationManager : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap mainTilemap; // 主地圖
    [SerializeField] private Tilemap fogTilemap; // 霧層（未探索區域）
    [SerializeField] private int fogLayer = 31; // 霧層使用的 Layer（預設為 Layer 31，通常不被主相機渲染）
    
    [Header("Fog Camera")]
    [SerializeField] private Camera fogCamera; // 專門渲染霧層的相機（用於小地圖）
    
    [Header("Exploration Settings")]
    [SerializeField] private float explorationUpdateInterval = 0.2f; // 探索更新間隔（秒）
    [SerializeField] private Color unexploredColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 未探索區域顏色（灰色）
    [SerializeField] private bool persistExploration = true; // 是否持久化探索數據
    
    [Header("Player Detection")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool useEntityManager = true; // 是否使用 EntityManager 獲取 Player
    
    private Player player;
    private PlayerDetection playerDetection;
    private EntityManager entityManager;
    private float lastExplorationUpdate = 0f;
    
    // 探索狀態：使用 HashSet 記錄已探索的 tile 位置（Vector3Int）
    private HashSet<Vector3Int> exploredTiles = new HashSet<Vector3Int>();
    
    // 用於顯示未探索區域的 Tile
    private TileBase fogTile;
    
    // 持久化相關
    private const string EXPLORATION_DATA_KEY = "MapExplorationData";
    
    // Tilemap 邊界（用於優化）
    private BoundsInt tilemapBounds;
    private bool hasInitializedBounds = false;
    
    private void Awake()
    {
        // 嘗試獲取 EntityManager 引用
        if (useEntityManager)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
    }
    
    private void Start()
    {
        if (mainTilemap == null)
        {
            Debug.LogError("MapExplorationManager: mainTilemap 未設定！請在 Inspector 中指定 mainTilemap。");
            return;
        }
        
        // 創建霧層 Tilemap（如果沒有）
        if (fogTilemap == null)
        {
            CreateFogTilemap();
        }
        
        // 創建灰色 Tile
        CreateFogTile();
        
        // 初始化邊界
        UpdateTilemapBounds();
        
        // 尋找玩家
        if (useEntityManager && entityManager != null)
        {
            player = entityManager.Player;
            if (player == null)
            {
                entityManager.OnPlayerReady += HandlePlayerReady;
            }
        }
        else if (autoFindPlayer)
        {
            player = FindFirstObjectByType<Player>();
        }
        
        if (player != null)
        {
            playerDetection = player.GetComponent<PlayerDetection>();
        }
        
        // 加載已探索的區域
        if (persistExploration)
        {
            LoadExplorationData();
        }
        
        // 初始化霧層
        UpdateFogLayer();
    }
    
    /// <summary>
    /// 處理 Player 準備就緒事件
    /// </summary>
    private void HandlePlayerReady()
    {
        if (entityManager != null)
        {
            player = entityManager.Player;
            if (player != null)
            {
                playerDetection = player.GetComponent<PlayerDetection>();
                entityManager.OnPlayerReady -= HandlePlayerReady;
            }
        }
    }
    
    private void Update()
    {
        if (player == null || playerDetection == null || mainTilemap == null) return;
        
        // 定期更新探索狀態
        if (Time.time - lastExplorationUpdate >= explorationUpdateInterval)
        {
            UpdateExploration();
            lastExplorationUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// 更新 Tilemap 邊界
    /// </summary>
    private void UpdateTilemapBounds()
    {
        if (mainTilemap == null) return;
        
        mainTilemap.CompressBounds();
        tilemapBounds = mainTilemap.cellBounds;
        hasInitializedBounds = true;
        
        Debug.Log($"MapExplorationManager: Tilemap 邊界已更新 - {tilemapBounds}");
    }
    
    /// <summary>
    /// 更新探索狀態
    /// </summary>
    private void UpdateExploration()
    {
        Vector2 playerPos = player.transform.position;
        float viewRange = playerDetection.ViewRange;
        
        // 獲取玩家位置對應的 tile 座標
        Vector3Int playerTilePos = mainTilemap.WorldToCell(playerPos);
        
        // 計算需要檢查的範圍（以 viewRange 為半徑的圓形區域）
        int rangeInTiles = Mathf.CeilToInt(viewRange);
        
        // 第一步：先找到所有距離 < viewRange 且尚未解鎖的 tile
        List<Vector3Int> candidateTiles = new List<Vector3Int>();
        
        for (int x = -rangeInTiles; x <= rangeInTiles; x++)
        {
            for (int y = -rangeInTiles; y <= rangeInTiles; y++)
            {
                Vector3Int tilePos = playerTilePos + new Vector3Int(x, y, 0);
                
                // 檢查該 tile 是否已經探索過
                if (exploredTiles.Contains(tilePos)) continue;
                
                // 檢查主地圖在該位置是否有 tile（只探索有 tile 的位置）
                if (mainTilemap.GetTile(tilePos) == null) continue;
                
                // 計算 tile 的世界座標（tile 的中心點）
                Vector3 tileWorldPos = mainTilemap.CellToWorld(tilePos);
                Vector3 cellSize = mainTilemap.cellSize;
                Vector2 tileCenter = new Vector2(
                    tileWorldPos.x + cellSize.x * 0.5f,
                    tileWorldPos.y + cellSize.y * 0.5f
                );
                
                // 檢查距離（圓形範圍）
                float distance = Vector2.Distance(playerPos, tileCenter);
                if (distance > viewRange) continue;
                
                // 符合條件的 tile 加入候選列表
                candidateTiles.Add(tilePos);
            }
        }
        
        // 第二步：檢查候選 tile 是否可以被玩家看到
        bool hasNewExploration = false;
        
        foreach (Vector3Int tilePos in candidateTiles)
        {
            // 計算 tile 的中心點世界座標
            Vector3 tileWorldPos = mainTilemap.CellToWorld(tilePos);
            Vector3 cellSize = mainTilemap.cellSize;
            Vector2 tileCenter = new Vector2(
                tileWorldPos.x + cellSize.x * 0.5f,
                tileWorldPos.y + cellSize.y * 0.5f
            );
            
            // 檢查玩家是否能看到這個 tile 的中心點
            if (playerDetection.CanSeeTarget(tileCenter))
            {
                exploredTiles.Add(tilePos);
                hasNewExploration = true;
            }
        }
        
        // 如果有新的探索，更新霧層並保存
        if (hasNewExploration)
        {
            UpdateFogLayer();
            
            if (persistExploration)
            {
                SaveExplorationData();
            }
        }
    }
    
    /// <summary>
    /// 更新霧層（顯示未探索區域）
    /// </summary>
    private void UpdateFogLayer()
    {
        if (fogTilemap == null || mainTilemap == null || !hasInitializedBounds || fogTile == null)
        {
            if (fogTile == null)
            {
                Debug.LogWarning("MapExplorationManager: fogTile 為 null，無法更新霧層");
            }
            return;
        }
        
        // 清除霧層
        fogTilemap.ClearAllTiles();
        
        int fogTileCount = 0;
        
        // 為未探索的 tile 添加霧層
        foreach (Vector3Int pos in tilemapBounds.allPositionsWithin)
        {
            // 如果主地圖在該位置有 tile，且該位置未被探索
            if (mainTilemap.GetTile(pos) != null && !exploredTiles.Contains(pos))
            {
                fogTilemap.SetTile(pos, fogTile);
                // 設置該 tile 的顏色（用於顯示灰色遮罩）
                fogTilemap.SetColor(pos, unexploredColor);
                fogTileCount++;
            }
        }
        
        Debug.Log($"MapExplorationManager: 霧層已更新 - 已探索: {exploredTiles.Count}, 未探索(霧層): {fogTileCount}");
    }
    
    /// <summary>
    /// 創建霧層 Tilemap
    /// </summary>
    private void CreateFogTilemap()
    {
        if (mainTilemap == null) return;
        
        GameObject fogObject = new GameObject("FogTilemap");
        
        // 確保霧層與主地圖使用相同的 Grid（如果主地圖有 Grid 父物件）
        if (mainTilemap.transform.parent != null)
        {
            Grid grid = mainTilemap.transform.parent.GetComponent<Grid>();
            if (grid != null)
            {
                // 如果主地圖有 Grid 父物件，將霧層也設置為同一個 Grid 的子物件
                fogObject.transform.SetParent(mainTilemap.transform.parent);
                Debug.Log("MapExplorationManager: 霧層已設置為與主地圖相同的 Grid 子物件");
            }
            else
            {
                fogObject.transform.SetParent(transform);
            }
        }
        else
        {
            fogObject.transform.SetParent(transform);
        }
        
        fogTilemap = fogObject.AddComponent<Tilemap>();
        
        // 設置 Tilemap 屬性與主地圖一致
        fogTilemap.tileAnchor = mainTilemap.tileAnchor;
        fogTilemap.orientation = mainTilemap.orientation;
        fogTilemap.animationFrameRate = mainTilemap.animationFrameRate;
        
        // 設置霧層為專用的 Layer（預設為 Layer 31，通常不被主相機渲染）
        // 這樣霧層只會在地圖相機中顯示，不會在遊戲場景中顯示
        fogObject.layer = fogLayer;
        
        // 不添加 TilemapRenderer，使用專用的相機來渲染霧層
        
        // 確保主相機不渲染霧層
        EnsureMainCameraExcludesFogLayer();
        
        // 創建專用的霧層相機（用於小地圖渲染）
        CreateFogCamera();
        
        Debug.Log($"MapExplorationManager: 已創建霧層 Tilemap (Layer: {LayerMask.LayerToName(fogObject.layer)}/{fogLayer}, 無 TilemapRenderer，使用專用相機渲染)");
    }
    
    /// <summary>
    /// 確保主相機不渲染霧層
    /// </summary>
    private void EnsureMainCameraExcludesFogLayer()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera != null)
        {
            // 從主相機的 cullingMask 中排除霧層的 Layer
            LayerMask mainCameraMask = mainCamera.cullingMask;
            mainCameraMask &= ~(1 << fogLayer); // 排除霧層 Layer
            mainCamera.cullingMask = mainCameraMask;
            
            Debug.Log($"MapExplorationManager: 主相機已排除霧層 Layer ({LayerMask.LayerToName(fogLayer)})");
        }
    }
    
    /// <summary>
    /// 創建專用的霧層相機（用於小地圖渲染）
    /// </summary>
    private void CreateFogCamera()
    {
        if (fogTilemap == null) return;
        
        // 創建霧層相機 GameObject
        GameObject fogCameraObj = new GameObject("FogCamera");
        fogCameraObj.transform.SetParent(transform);
        fogCamera = fogCameraObj.AddComponent<Camera>();
        
        // 設置相機屬性
        fogCamera.orthographic = true;
        fogCamera.clearFlags = CameraClearFlags.Nothing; // 不清除，疊加在主地圖上
        fogCamera.cullingMask = 1 << fogLayer; // 只渲染霧層
        fogCamera.depth = 1; // 比主地圖相機深度高，後渲染
        fogCamera.enabled = false; // 預設禁用，由 TilemapMapUIManager 控制
        
        Debug.Log($"MapExplorationManager: 已創建霧層相機 (Layer: {LayerMask.LayerToName(fogLayer)})");
    }
    
    /// <summary>
    /// 獲取霧層相機
    /// </summary>
    public Camera GetFogCamera()
    {
        return fogCamera;
    }
    
    /// <summary>
    /// 創建灰色霧層 Tile
    /// </summary>
    private void CreateFogTile()
    {
        // 創建一個帶有灰色 Sprite 的 Tile
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        
        // 創建一個較大的白色 Sprite（顏色會通過 Tilemap.SetColor 來設置）
        // 使用 64x64 的紋理，確保在 tile 大小下能正確顯示
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        // 填充整個紋理為白色
        Color[] colors = new Color[textureSize * textureSize];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();
        
        // 創建 Sprite，使用與主地圖相同的 pixels per unit
        float pixelsPerUnit = 100f; // 默認值，如果主地圖有 tile，可以從中獲取
        if (mainTilemap != null && mainTilemap.GetTile(Vector3Int.zero) != null)
        {
            TileBase sampleTile = mainTilemap.GetTile(Vector3Int.zero);
            if (sampleTile is Tile)
            {
                Tile sampleTileTyped = sampleTile as Tile;
                if (sampleTileTyped.sprite != null)
                {
                    pixelsPerUnit = sampleTileTyped.sprite.pixelsPerUnit;
                }
            }
        }
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        tile.sprite = sprite;
        
        fogTile = tile;
        
        Debug.Log($"MapExplorationManager: 已創建霧層 Tile (Sprite size: {textureSize}x{textureSize}, Pixels per unit: {pixelsPerUnit})");
    }
    
    /// <summary>
    /// 保存探索數據
    /// </summary>
    private void SaveExplorationData()
    {
        if (exploredTiles.Count == 0) return;
        
        // 將 Vector3Int 列表轉換為可序列化的格式
        List<string> tilePositions = new List<string>();
        foreach (var pos in exploredTiles)
        {
            tilePositions.Add($"{pos.x},{pos.y},{pos.z}");
        }
        
        // 使用 JSON 保存（因為 PlayerPrefs 有大小限制）
        SerializableList<string> data = new SerializableList<string>(tilePositions);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(EXPLORATION_DATA_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"[MapExplorationManager] 保存了 {exploredTiles.Count} 個已探索的 tile");
    }
    
    /// <summary>
    /// 加載探索數據
    /// </summary>
    private void LoadExplorationData()
    {
        if (!PlayerPrefs.HasKey(EXPLORATION_DATA_KEY)) return;
        
        string json = PlayerPrefs.GetString(EXPLORATION_DATA_KEY);
        if (string.IsNullOrEmpty(json)) return;
        
        try
        {
            SerializableList<string> data = JsonUtility.FromJson<SerializableList<string>>(json);
            
            exploredTiles.Clear();
            foreach (var posStr in data.items)
            {
                string[] parts = posStr.Split(',');
                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[0], out int x) &&
                        int.TryParse(parts[1], out int y) &&
                        int.TryParse(parts[2], out int z))
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        exploredTiles.Add(pos);
                    }
                }
            }
            
            Debug.Log($"[MapExplorationManager] 加載了 {exploredTiles.Count} 個已探索的 tile");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapExplorationManager] 加載探索數據失敗: {e.Message}");
        }
    }
    
    /// <summary>
    /// 清除所有探索數據
    /// </summary>
    public void ClearExplorationData()
    {
        exploredTiles.Clear();
        UpdateFogLayer();
        
        if (persistExploration)
        {
            PlayerPrefs.DeleteKey(EXPLORATION_DATA_KEY);
            PlayerPrefs.Save();
        }
        
        Debug.Log("[MapExplorationManager] 已清除所有探索數據");
    }
    
    /// <summary>
    /// 設定 Tilemap
    /// </summary>
    public void SetTilemap(Tilemap tilemap)
    {
        mainTilemap = tilemap;
        if (mainTilemap != null)
        {
            UpdateTilemapBounds();
            UpdateFogLayer();
        }
    }
    
    /// <summary>
    /// 獲取已探索的 tile 數量
    /// </summary>
    public int GetExploredTileCount()
    {
        return exploredTiles.Count;
    }
    
    /// <summary>
    /// 檢查指定位置是否已探索
    /// </summary>
    public bool IsTileExplored(Vector3Int tilePosition)
    {
        return exploredTiles.Contains(tilePosition);
    }
    
    /// <summary>
    /// 檢查指定世界座標是否已探索
    /// </summary>
    public bool IsPositionExplored(Vector3 worldPosition)
    {
        if (mainTilemap == null) return false;
        Vector3Int tilePos = mainTilemap.WorldToCell(worldPosition);
        return exploredTiles.Contains(tilePos);
    }
}

// 輔助類：可序列化的列表
[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
    
    public SerializableList()
    {
        items = new List<T>();
    }
    
    public SerializableList(List<T> list)
    {
        items = list ?? new List<T>();
    }
}

