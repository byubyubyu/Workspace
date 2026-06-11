// 保存先: Assets/Scripts/Demon/BodyCatalog.cs
// 素体カタログ（マスターSO）。転生で選べる素体（BodyData）の一覧を束ねる。
//   素体ID＝このリストの番号（マルチ方針：全クライアント同梱の静的データを番号で参照）。
//   DemonCore・転生UIの双方がこのカタログを読む。
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyCatalog", menuName = "Project/Demon/BodyCatalog")]
public class BodyCatalog : ScriptableObject
{
    [SerializeField] private List<BodyData> bodies = new List<BodyData>();

    public IReadOnlyList<BodyData> Bodies => bodies;
}
