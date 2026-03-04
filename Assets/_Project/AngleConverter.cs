using Unity.VisualScripting;
using UnityEngine;

[UnitCategory("Custom/Angles")]
[UnitTitle("Snap Angle To 45")]
[UnitSurtitle("Angle")]
[TypeIcon(typeof(float))]
public class AngleSnapNode : Unit
{
    [DoNotSerialize]
    [PortLabelHidden]
    public ValueInput inputAngle { get; private set; }

    [DoNotSerialize]
    [PortLabelHidden]
    public ValueOutput snappedAngle { get; private set; }

    protected override void Definition()
    {
        // 入力ポートを定義（デフォルト値0度）
        inputAngle = ValueInput<float>("Input Angle", 0f);
        
        // 出力ポートを定義
        snappedAngle = ValueOutput<float>("Snapped Angle", (flow) =>
        {
            // 入力角度を取得
            float angle = flow.GetValue<float>(inputAngle);
            
            // 角度を45度刻みに変換
            float result = SnapTo45Degrees(angle);
            
            return result;
        });

        // 依存関係を定義
        Requirement(inputAngle, snappedAngle);
    }

    private float SnapTo45Degrees(float angle)
    {
        // 入力角度を-180から180の範囲に正規化
        angle = Mathf.Repeat(angle + 180f, 360f) - 180f;
        
        // 45で割って四捨五入し、その後45を掛けて戻す
        float snapped = Mathf.Round(angle / 45f) * 45f;
        
        // 結果を-180から180の範囲に収める
        if (snapped > 180f)
        {
            snapped -= 360f;
        }
        else if (snapped <= -180f)
        {
            snapped += 360f;
        }
        
        return snapped;
    }
}