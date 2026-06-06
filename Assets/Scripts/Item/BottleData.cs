// 保存先: Assets/Scripts/Item/BottleData.cs
// 瓶の種類SO。瓶の内側サイズを持つ。
//   初期は固定1種類だが、将来の可変・アップグレード（大きい瓶・口の狭い瓶）に備えてSOで外に出す。
//   壁の厚みは持たない（ゲーム性に影響しない技術値のため実装側で固定。物理は分厚く・見た目は薄い絵を被せる）。
//   口は初期は縁なし（上の開口部のみ）。縁の広さ・形は将来ここに追加する。
using UnityEngine;

[CreateAssetMenu(fileName = "BottleData", menuName = "Project/Item/BottleData")]
public class BottleData : ScriptableObject
{
    [Header("瓶の内側サイズ（2D物理空間内・ローカル単位）")]
    [SerializeField] private float innerWidth = 4f;   // 内側の幅
    [SerializeField] private float innerHeight = 6f;  // 内側の高さ（やや縦長〜正方形寄りを基本）

    public float InnerWidth => innerWidth;
    public float InnerHeight => innerHeight;
}
