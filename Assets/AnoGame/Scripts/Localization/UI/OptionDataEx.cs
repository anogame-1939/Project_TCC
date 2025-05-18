using TMPro;
using UnityEngine;

[System.Serializable]
public class OptionDataEx : TMP_Dropdown.OptionData
{
    public TMP_FontAsset font;

    public OptionDataEx(string text, TMP_FontAsset font) : base(text)
    {
        this.font = font;
    }
}
