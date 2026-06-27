// GroundColor.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class GroundColor : MonoBehaviour
{
    [Tooltip("该地面的颜色属性（十六进制如 #85FF85）")]
    public Color groundColor = Color.white; // 默认白色
    public string ys; 
    void Reset()
    {
        groundColor = HexToColor(ys);
    }

    /// <summary>
    /// 将十六进制颜色字符串转为 Unity Color
    /// 支持 "#RRGGBB" 或 "RRGGBB"
    /// </summary>
    public static Color HexToColor(string hex)
    {

        if (string.IsNullOrEmpty(hex))
        {
            return Color.white;
        }

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
        {
            Debug.LogError("无效的十六进制颜色: " + hex);
            return Color.white;
        }

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
    }
}