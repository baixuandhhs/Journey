// Assets/Scripts/AttributeBlock.cs
using UnityEngine;

public class AttributeBlock : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("AttributeBlock ÐèÒª SpriteRenderer£¡");
            enabled = false;
        }
    }

  
    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }


    public Color GetColor()
    {
        return spriteRenderer.color;
    }
}