using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CustomDropdownEx : TMP_Dropdown
{
    [SerializeField]
    public List<OptionDataEx> optionsEx = new List<OptionDataEx>();

    protected override DropdownItem CreateItem(DropdownItem itemTemplate)
    {
        // 親クラスの処理でItemを生成
        DropdownItem itemGO = base.CreateItem(itemTemplate);
        // itemGO 上の TextMeshProUGUI を取得してフォントを適用
        // ただし継承クラスでさらに細かい処理が必要になる

        return itemGO;
    }

    // あるいは RefreshShownValue() をオーバーライドして
    // キャプションテキストのフォントを切り替える処理を入れるなど
}
