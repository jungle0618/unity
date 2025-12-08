using UnityEngine;
using TMPro;

/// <summary>
/// Neon Text Effect - Creates animated neon glow with jumping shadows
/// Perfect for main menu titles and dramatic text displays
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class NeonTextEffect : MonoBehaviour
{
    [Header("Neon Colors")]
    [SerializeField] private Color neonColor = new Color(0f, 1f, 1f, 1f); // Cyan neon
    [SerializeField] private Color shadowColor = new Color(1f, 0f, 1f, 0.8f); // Magenta shadow
    
    [Header("Glow Animation")]
    [SerializeField] private bool animateGlow = true;
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float minGlowIntensity = 0.5f;
    [SerializeField] private float maxGlowIntensity = 1.5f;
    
    [Header("Shadow Jump Animation")]
    [SerializeField] private bool animateShadow = true;
    [SerializeField] private float jumpSpeed = 3f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDistance = 3f;
    
    [Header("Shadow Settings")]
    [SerializeField] private int numberOfShadows = 3; // Multiple shadows for more dramatic effect
    [SerializeField] private float shadowSpacing = 2f; // Spacing between shadow layers
    
    private TextMeshProUGUI mainText;
    private TextMeshProUGUI[] shadowTexts;
    private Material materialInstance;
    private float glowTimer = 0f;
    private float jumpTimer = 0f;
    
    private void Awake()
    {
        mainText = GetComponent<TextMeshProUGUI>();
        
        // Create material instance to avoid affecting other texts
        materialInstance = new Material(mainText.fontMaterial);
        mainText.fontMaterial = materialInstance;
        
        CreateShadowLayers();
    }
    
    private void Start()
    {
        // Apply initial neon color
        mainText.color = neonColor;
        
        // Enable glow if material supports it
        if (materialInstance.HasProperty("_GlowPower"))
        {
            materialInstance.SetFloat("_GlowPower", maxGlowIntensity);
        }
    }
    
    /// <summary>
    /// Create shadow text layers behind the main text
    /// </summary>
    private void CreateShadowLayers()
    {
        shadowTexts = new TextMeshProUGUI[numberOfShadows];
        
        for (int i = 0; i < numberOfShadows; i++)
        {
            // Create a new GameObject for each shadow
            GameObject shadowObj = new GameObject($"Shadow_{i + 1}");
            shadowObj.transform.SetParent(transform, false);
            
            // Copy the TextMeshProUGUI component
            TextMeshProUGUI shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
            shadowTexts[i] = shadowText;
            
            // Copy all properties from main text
            shadowText.font = mainText.font;
            shadowText.fontSize = mainText.fontSize;
            shadowText.text = mainText.text;
            shadowText.alignment = mainText.alignment;
            shadowText.enableAutoSizing = mainText.enableAutoSizing;
            shadowText.fontSizeMin = mainText.fontSizeMin;
            shadowText.fontSizeMax = mainText.fontSizeMax;
            
            // Copy overflow settings to prevent text mismatch
            shadowText.overflowMode = mainText.overflowMode;
            shadowText.enableWordWrapping = mainText.enableWordWrapping;
            shadowText.horizontalMapping = mainText.horizontalMapping;
            shadowText.verticalMapping = mainText.verticalMapping;
            
            // Copy margin settings
            shadowText.margin = mainText.margin;
            
            // Copy additional text settings
            shadowText.fontStyle = mainText.fontStyle;
            shadowText.characterSpacing = mainText.characterSpacing;
            shadowText.wordSpacing = mainText.wordSpacing;
            shadowText.lineSpacing = mainText.lineSpacing;
            shadowText.paragraphSpacing = mainText.paragraphSpacing;
            
            // Set shadow color (darker for layers further back)
            float alpha = shadowColor.a * (1f - (i * 0.2f));
            shadowText.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);
            
            // Position shadow behind main text
            RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
            RectTransform mainRect = mainText.GetComponent<RectTransform>();
            
            // Copy ALL RectTransform properties
            shadowRect.anchorMin = mainRect.anchorMin;
            shadowRect.anchorMax = mainRect.anchorMax;
            shadowRect.sizeDelta = mainRect.sizeDelta;
            shadowRect.pivot = mainRect.pivot;
            shadowRect.anchoredPosition = mainRect.anchoredPosition;
            shadowRect.anchoredPosition3D = mainRect.anchoredPosition3D;
            shadowRect.offsetMin = mainRect.offsetMin;
            shadowRect.offsetMax = mainRect.offsetMax;
            shadowRect.localScale = mainRect.localScale;
            shadowRect.localRotation = mainRect.localRotation;
            
            // Move shadow to back in hierarchy
            shadowRect.SetAsFirstSibling();
        }
        
        // Ensure main text is on top in hierarchy
        mainText.GetComponent<RectTransform>().SetAsLastSibling();
    }
    
    private void Update()
    {
        // Animate glow
        if (animateGlow)
        {
            AnimateGlow();
        }
        
        // Animate shadow jumping
        if (animateShadow && shadowTexts != null)
        {
            AnimateShadowJump();
        }
    }
    
    /// <summary>
    /// Animate the neon glow pulsing effect
    /// </summary>
    private void AnimateGlow()
    {
        glowTimer += Time.deltaTime * glowSpeed;
        
        // Pulse the glow intensity
        float glowIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, 
            (Mathf.Sin(glowTimer) + 1f) * 0.5f);
        
        // Apply to material if supported
        if (materialInstance != null && materialInstance.HasProperty("_GlowPower"))
        {
            materialInstance.SetFloat("_GlowPower", glowIntensity);
        }
        
        // Also pulse the main text alpha slightly
        Color currentColor = mainText.color;
        currentColor.a = Mathf.Lerp(0.9f, 1f, (Mathf.Sin(glowTimer * 0.5f) + 1f) * 0.5f);
        mainText.color = currentColor;
    }
    
    /// <summary>
    /// Animate the shadow jumping effect
    /// </summary>
    private void AnimateShadowJump()
    {
        jumpTimer += Time.deltaTime * jumpSpeed;
        
        for (int i = 0; i < shadowTexts.Length; i++)
        {
            if (shadowTexts[i] == null) continue;
            
            RectTransform shadowRect = shadowTexts[i].GetComponent<RectTransform>();
            
            // Calculate offset for this shadow layer (stagger the animation)
            float phaseOffset = i * 0.3f;
            float time = jumpTimer + phaseOffset;
            
            // Jumping offset using sine waves
            float xOffset = Mathf.Sin(time) * jumpDistance * (i + 1) * shadowSpacing;
            float yOffset = Mathf.Sin(time * 2f) * jumpHeight * (i + 1) * shadowSpacing;
            
            // Apply the animated offset
            shadowRect.anchoredPosition = new Vector2(xOffset, yOffset);
            
            // Optional: Slightly rotate the shadow for more dramatic effect
            float rotation = Mathf.Sin(time * 0.5f) * 2f;
            shadowRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
    }
    
    /// <summary>
    /// Update shadow text when main text changes
    /// </summary>
    private void LateUpdate()
    {
        // Sync shadow text content and properties with main text
        if (shadowTexts != null)
        {
            foreach (var shadow in shadowTexts)
            {
                if (shadow != null)
                {
                    // Update text content
                    if (shadow.text != mainText.text)
                    {
                        shadow.text = mainText.text;
                    }
                    
                    // Sync font size if auto-sizing is enabled
                    if (mainText.enableAutoSizing && shadow.fontSize != mainText.fontSize)
                    {
                        shadow.fontSize = mainText.fontSize;
                    }
                    
                    // Sync overflow mode
                    if (shadow.overflowMode != mainText.overflowMode)
                    {
                        shadow.overflowMode = mainText.overflowMode;
                    }
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
    
    /// <summary>
    /// Set the neon color at runtime
    /// </summary>
    public void SetNeonColor(Color color)
    {
        neonColor = color;
        if (mainText != null)
        {
            mainText.color = color;
        }
    }
    
    /// <summary>
    /// Set the shadow color at runtime
    /// </summary>
    public void SetShadowColor(Color color)
    {
        shadowColor = color;
        if (shadowTexts != null)
        {
            for (int i = 0; i < shadowTexts.Length; i++)
            {
                if (shadowTexts[i] != null)
                {
                    float alpha = color.a * (1f - (i * 0.2f));
                    shadowTexts[i].color = new Color(color.r, color.g, color.b, alpha);
                }
            }
        }
    }
}
