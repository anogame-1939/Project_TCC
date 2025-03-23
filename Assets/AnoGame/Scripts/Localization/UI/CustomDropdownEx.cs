using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CustomDropdownEx : TMP_Dropdown
{
    [SerializeField]
    public List<string> aaa;
    [SerializeField]
    public List<OptionDataEx> optionsEx = new List<OptionDataEx>();

    protected override DropdownItem CreateItem(DropdownItem itemTemplate)
    {
        var item = base.CreateItem(itemTemplate);
        // item が親に追加された後に index を調べたい
        Transform parent = item.transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) == item.transform)
            {
                int index = i - 1; // 先頭に何らかのテンプレートがある場合などで調整
                Debug.Log($"CreateItem index = {index}");
                break;
            }
        }
        return item;
    }

    // あるいは RefreshShownValue() をオーバーライドして
    // キャプションテキストのフォントを切り替える処理を入れるなど
}
