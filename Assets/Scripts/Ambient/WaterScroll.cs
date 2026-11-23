using UnityEngine;

/// <summary>
/// Slowly scrolls the sea material's texture (base map and its normal map share the same UVs in URP Lit),
/// so the ripples drift and the sun glitter moves across the water.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WaterScroll : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.004f, 0.0025f);
    [SerializeField] private string textureProperty = "_BaseMap";

    private Material waterMaterial;
    private Vector2 startOffset;

    private void Awake()
    {
        // .material makes a runtime copy, so the saved asset is never changed by Play mode.
        waterMaterial = GetComponent<Renderer>().material;
        startOffset = waterMaterial.GetTextureOffset(textureProperty);
    }

    private void Update()
    {
        Vector2 offset = startOffset + scrollSpeed * Time.time;
        offset.x %= 1f;
        offset.y %= 1f;
        waterMaterial.SetTextureOffset(textureProperty, offset);
    }
}
