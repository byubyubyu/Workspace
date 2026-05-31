// 保存先: Assets/Scripts/Minion/MinionFactory.cs （既存を上書き）
using UnityEngine;

public class MinionFactory : MonoBehaviour
{
    // 確定事項: Factory は生成のみ。初期化(Initialize)は呼び出し側(Production)が行う。
    public MinionCore Create(IMinionData data, Vector3 position)
    {
        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        return obj.GetComponent<MinionCore>();
    }
}
