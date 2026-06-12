# CLAUDE.md

このファイルは、Claude Code がこのリポジトリで作業するときの指針です。

================================================================
最優先ルール
================================================================

- **すべての返答は日本語で行うこと。**

- **ユーザーが「実装して」と明示するまで、いかなる実装も開始しない。**
  実装＝コード・シーン・アセット・設定の変更、ファイルの生成・配置を含む一切の作成行為。
  「タスクNをやろう」「進めよう」等は着手の意思表示であって実装許可ではない（調査と計画提示までで止まる）。
  気づき・改善案・「これをした方がいい」は歓迎される——ただし会話で伝えるに留め、行動には移さない。

================================================================
応答・進め方のルール
================================================================

基本姿勢
  - 感情表現は使わない。
  - 無駄な言葉を省く。ただし説明は省かない。
  - 相手は前提知識がないものとして、具体例を出して説明する。

作業の順序（絶対遵守）
  - プロジェクトを進める方法は必ず「該当部分の把握 → 実装計画 → 提案 → 実装 → 検証」の順序を厳守する。
  - 「計画・提案」は実装にすぐ行かず、しっかり詰めてから進める。
    進めるにあたってユーザーに判断を絶対に仰ぐこと（承認なしに実装へ進まない）。
      1. 把握 … 該当部分の関連コードと設計ドキュメント（GDD・クラス設計書）を確認する。
      2. 実装計画 … 以下の観点で考える：
                ・疎結合になっているか
                ・コンポーネント指向に沿っているか
                ・SO（ScriptableObject）を使うべきか
                ・GoF やデザインパターンを参考にできる所はする（分からないなら調べること）
      3. 提案 … 方針・選択肢・おすすめを提示し、ユーザーの承認を得る。内容に根拠があると尚よい。
      4. 実装 … 承認された内容だけを実装する。承認範囲の外に手を広げない。
      5. 検証 … 動作確認（コンパイル・Playモード・スクリーンショット等）まで行い、結果を報告する。

確認・判断
  - コードを変更する前に、方針と選択肢（GoF・Unity公式手法を含む）・根拠・おすすめを提示し、
    ユーザーの承認を得てから実装する。黙って書き換えない。
  - 最終判断はユーザーが行う。
  - 選択肢があるときは GoF パターンや Unity 公式手法なども含め、根拠とおすすめを示す。
  - 次の話題に勝手に進まない。

ドキュメント・図
  - ドキュメントのまとめ直し・図の作成は承認なしで行ってよい。
    ただし大規模なものはしない（大きくなりそうなら先に提案する）。

禁止事項（承認なしに行わない）
  - コードを勝手に書かない（上記「作業の順序」「確認・判断」に従う）。

誠実さ
  - 知識に不確かな点があるときは「ここは不確かです」と先に断ってから答える。
  - 推測と事実を区別する。

引き継ぎ・設計
  - 既に決定済みの内容を覆す提案をしない。
  - 設計に関わる判断をするときは、必ず下記の設計ドキュメントを開いて確認する。

================================================================
設計ドキュメント（設計判断のとき必ず参照）
================================================================

このリポジトリのルート（このファイルと同じ階層）に、確定済みの設計ドキュメントがある。
設計・仕様・クラス構造に関わる判断をするときは、必ず該当ファイルを開いて確認すること。

- `GDD.txt`
    ゲーム全体の仕様・設計方針（ゲーム概要、マップ、国、建物、兵士、AI、UI、アイテム等）。
- `クラス設計_アーキテクスチャ設計書.txt`
    クラス構成・アーキテクチャの確定状態（GameObject構成、各クラスの責務、SO構造、
    インターフェース、初期化順序、実装順序等）。

これらは「現時点の最新の確定状態」を表す文書である。記載された決定事項は勝手に覆さない。
コードとこれらの文書が食い違う場合は、その食い違いを指摘し、どちらに合わせるかをユーザーに確認する。

================================================================
プロジェクト概要
================================================================

Unity 6（エディタ `6000.4.6f1`、URP）の自律型領土戦シミュレーション＋アクション。AI が制御する
領土 Base が兵士（Minion）を生産し、固定の Path に沿って移動して隣接 Base を占拠・戦闘・建設する。
プレイヤーは開始時に「人間側／魔族側」を選び（FactionSelectUI）、アクション戦闘・アイテム・売買・
派遣指令で世界に関わる。第三勢力として野生モンスター、非戦闘NPCとして市民（商人含む）が存在する。
プレイヤー＝魂の二層成長（人間：スキル・加齢・世代交代／魔族：捕食・部位進化・転生）を持つ。
レンダーパイプラインは URP `17.4.0`、ナビゲーションは `com.unity.ai.navigation`（NavMesh）、
入力は新 Input System（`com.unity.inputsystem`）。

ゲームプレイのコードはすべて `Assets/Scripts/` にある。
`Assets/Packages/Point-Grass-Renderer/` はサードパーティ製（独自の `.asmdef` と名前空間を持つ
唯一のコード）。指示がない限り触らない。

================================================================
ビルド / 実行 / テスト
================================================================

CLI でのビルド設定はない。開発・実行は Unity エディタから行う。
  - 実行：Unity 6（`6000.4.6f1`）でプロジェクトを開き、`Assets/Scenes/SampleScene.unity` を開いて Play。
  - テスト：テストフレームワーク（`com.unity.test-framework`）は入っているが、現状 `Assets/` に
    テストアセンブリも `*Tests.cs` も存在しない。テストを追加した場合は、エディタの Test Runner
    （Window → General → Test Runner）から実行する。

`Assets/Scripts/` に `.asmdef` ファイルは存在しない。ゲームプレイコードはすべて既定の
`Assembly-CSharp` にコンパイルされる。asmdef を追加するとコンパイル・参照範囲が変わるため、
安易に追加しない。

================================================================
コーディング規約
================================================================

- **名前空間（namespace）を使わない。** ゲームプレイのクラスはすべてグローバル。整理はフォルダのみ
  （`Assets/Scripts/<システム名>/`）で行う。これに合わせ、namespace を導入しない。

- **Core / Component / Data の三点セット。** 各エンティティは `*Core` MonoBehaviour をハブに持つ
  （`MinionCore`、`BuildingCore`、`PlayerCombatCore`、`DemonCore`、`CitizenCore`）。振る舞いは別々の MonoBehaviour
  **コンポーネント**（`Movement`、`Vision`、`Attack`、`Health`、`Stamina`、`Dodge`、`Stagger`、
  `Builder`、`Production` …）に分割し、同じ GameObject にアタッチする。各コンポーネントは対になる
  **`*Data` ScriptableObject**（`MovementData`、`AttackData`、`VisionData` …）で設定され、
  この SO はシリアライズフィールドのみを持ちロジックを持たない。

- **セットアップは Awake/Start ではなく Initialize()。** Factory（`MinionFactory`、`BuildingFactory`）は
  prefab を `Instantiate` するだけ。その後、**呼び出し側**が `Initialize(...)` を呼んでデータを渡す。
  `MinionCore.Initialize(IMinionData)` が各 sub-data SO を担当コンポーネントへ配る。コンポーネントを
  追加するときも、コンポーネント側が自分でデータを読み込むのではなく、この「データを押し込む」方式に従う。

- **マスター SO ＋ サブ SO。** `MinionData`（`[CreateAssetMenu]` 付きで `IMinionData` を実装する SO）は
  約 7 個の専用サブ SO を参照し、兵士種別をまたいで再利用できるようにする。建物も同様で、
  `IBuildingData` 実装（`CityhallData`、`BarrackData`）が共有の `BuildingStatData` / `BuildStrategy`
  SO を参照する。

- **`Resource` が共通の数値プリミティブ。** `Common/Resource.cs` は MonoBehaviour ではない、
  Current/Max をクランプする入れ物で、`Add`/`Consume`/`CanAfford`/`IsFull`/`IsEmpty` を持つ。
  `Health`、`Stamina`、`CostPool`、`Construction` はそれぞれ `Resource` をラップしてドメイン固有の
  ロジックを足している。min/max の計算を作り直さず、これを再利用する。

- **差し替え可能なロジックは Strategy インターフェース。** `ICombatStrategy.Decide(CombatContext) → CombatAction`、
  `ITargetingStrategy.SelectTarget(...)`、`BuildStrategy`（Auto/Manual）。新しい挙動は呼び出し側に
  分岐を足すのではなく、新しい実装として追加する。

- **システム間の疎結合はイベント。** 例：`Cityhall.OnTeamChanged → BaseAI`、`Construction.OnCompleted`、
  生産 → `MinionManager.AddMinion`。

================================================================
システム索引
================================================================

詳細解説はここには書かない（二重管理によるドリフト防止）。
「設計書」＝`クラス設計_アーキテクスチャ設計書.txt`。作業前に該当する節（§）を必ず開くこと。

- 起動           `GameManager` が唯一のエントリーポイント。初期化順序は意図的
                 （Base初期化→初期建物→隣接Team→ゲームループ。**順序を崩さない**） → 設計書§8
- World/Base/Path グリッド領土＋固定ルートのグラフ。BuildingManager / MinionManager /
                 CitizenManager / BaseAI が Base 付属 → 設計書§2・GDD§2
- 建物           `BuildingCore`＋`Construction`（BuildStrategy: Auto/Manual）＋`CostPool`＋
                 `CityhallBehavior`（破壊で全建物消滅・OnTeamChanged） → 設計書§3・GDD§4
- 兵士           `MinionCore` がハブ。優先度StateMachine（Dead > Stagger > Combat > Building >
                 Moving/Wander）。vision.Refresh()→stateMachine.Update() の更新順 → 設計書§4・GDD§5
- 戦闘           Hitbox→Hurtbox→TakeDamage、防御は受け手（`DamageCalculator`）。
                 技＝`AttackMove`(SO)＋motionId連動モーション。部位＝`PartHurtbox`＋`PartData`。
                 蓄積ひるみ＝`AccumulatedStagger` → 設計書§4・§15、GDD§5
- AI             `BaseAI`（分散型・定期判断・argmax建設・プレイヤー派遣指令を優先充足） → 設計書§2・GDD§6
- プレイヤー(人間) `PlayerCombatCore`（実効値再計算のハブ）＋`PlayerHandState`（左クリック司令塔・
                 手ぶら/抜刀/武器構え/アイテム所持）＋`EquipmentHolder` → 設計書§2・§13C・GDD§7
- アイテム/瓶    物理瓶インベントリ。`InventoryHolder` が中身を所有者ごとに分離＝死体（`Corpse`）漁り・
                 商人売買と共通基盤 → 設計書§13・GDD§11
- 市民/商人      非戦闘NPC（タグ"Citizen"）。売買は priceItem×priceCount の物々交換一本化
                 （コインも普通のアイテム） → 設計書§14・GDD§12
- モンスター     新Core は作らず `MinionCore` 再利用＋Team.None＋`WanderState` → 設計書§15・GDD§13
- 魔族           `DemonCore`＝素体(`BodyData`)＋部位(`PartData`)。捕食(F)→部位進化、死亡→転生
                 （魂ポイントで素体解放）。入力は `DemonInputController`（人間と混ぜない） → 設計書§16・GDD§14
- 魂システム     人間：`PlayerSkills`(UO型スキル)/`Age`(加齢)/`Family`(婚活・世代交代)。
                 魔族：`DemonSoul`。実効値は `ModifiableStat` に集約 → 設計書§17・GDD§15
- UI             ゲージ（`StatBar`/`IGaugeSource`）・ミニマップ・画面群 I/C/M/E/P（相互排他・
                 裏空間3D→RenderTexture方式で統一）・`ActivePlayer`（操作中プレイヤー供給） → 設計書§2・GDD§8
- 検出系         E/F近接＝自己申告レジストリ＋`NearestFinder`（物理検索全廃）。
                 視線遮蔽＝`SightBlocker`（木の位置データを自前判定） → 設計書§4・§13
- その他         `Camera/CameraDistanceCulling`（描画距離カリング）／`Editor/GrassVariationTool`／
                 数値プリミティブ＝`Resource`・`ModifiableStat`・`NearestFinder`（Common/）

================================================================
導入済みMCP（このプロジェクトで使える外部ツール）
================================================================

- funplay     … Unityエディタ操作（Play制御・ヒエラルキー・コンポーネント編集・execute_code・
                スクショ・入力シミュレート）。Unity検証の本命。詳細は下記Funplayセクション。
- unity-docs  … Unity公式APIドキュメントの検索・参照。
- blender     … Blender自動操作。PolyHaven/Sketchfabアセット取得、AIによる3Dモデル生成→インポート。
                仮プリミティブの置き換え（モンスター・武器・アイテムのモデル制作）に使う。
- comfyui     … 画像生成（テクスチャ・UI素材・ポートレート等）。
- open-design … ローカルのデザインワークスペース（UIモックの作成・共有）。

# Funplay Unity MCP Project Guidance

This file is managed by Funplay MCP for Unity for Claude Code.

## Installed skills

- `unity-mcp-workflow` - Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects.

## Preferred workflow

- Use Funplay MCP tools for Unity editor state and automation.
- Use `execute_code` for non-trivial Unity orchestration. For new snippets, implement `IFunplayCommand` and use `ctx.RegisterObjectCreation` / `RegisterObjectModification` / `DestroyObject` so changes participate in Undo and `ctx.Log` for traceable output.
- Inspect Unity objects through MCP before changing user-named scene or prefab targets. Carry the returned `instanceId` into follow-up calls (`find_method=by_id`) instead of re-resolving by name.
- Tool returns are structured JSON (`{success, message, data}` / `{success: false, code, error, data}`). Branch on `code`, not free-form text.
- Set component fields with `set_component_property(ies)` — it picks up `[SerializeField] private` fields and accepts Object references as `{"fileID": <instanceId>}` or `{"assetPath": "Assets/..."}`.
- Read editor state through `get_selection`, `get_prefab_stage`, `get_tags`, `get_layers`, `get_build_settings`; try `execute_menu_item` before writing ad-hoc `execute_code`.
- Save only the scene or prefab assets intentionally modified, then read back exact values.
- With default `core` exposure, use the focused workflow tools. With default `full` exposure, prefer specific MCP tools for simple editor operations.
- `execute_code` refreshes assets and waits for compilation before running. For other tools that depend on freshly compiled code, still call `request_recompile` after external script edits.
- `request_recompile` is rejected while Unity is in Play Mode. Call `exit_play_mode` first, then retry.
- After `enter_play_mode`, the HTTP server briefly drops while Unity reloads the domain. Poll `tools/list` or `get_reload_recovery_status` until it responds again before issuing the next tool call.
- If domain reload interrupts a request, follow with `get_reload_recovery_status`.
- Additional installed skills are available under `.claude/skills/`.

## Project

- Project root: `C:\game\Workspace`
- Product name: `Workspace`
