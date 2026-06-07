# CLAUDE.md

このファイルは、Claude Code がこのリポジトリで作業するときの指針です。

================================================================
最優先ルール
================================================================

- **すべての返答は日本語で行うこと。**

================================================================
応答・進め方のルール
================================================================

基本姿勢
  - 感情表現は使わない。
  - 無駄な言葉を省く。ただし説明は省かない。
  - 相手は前提知識がないものとして、具体例を出して説明する。

確認・判断
  - コードを変更する前に、方針と選択肢（GoF・Unity公式手法を含む）・根拠・おすすめを提示し、
    ユーザーの承認を得てから実装する。黙って書き換えない。
  - 最終判断はユーザーが行う。
  - 選択肢があるときは GoF パターンや Unity 公式手法なども含め、根拠とおすすめを示す。
  - 次の話題に勝手に進まない。

禁止事項（承認なしに行わない）
  - ドキュメントを勝手にまとめ直さない。
  - 図を勝手に作らない。
  - コードを勝手に書かない（上記「確認・判断」に従う）。

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

Unity 6（エディタ `6000.4.6f1`、URP）のリアルタイム戦略ゲーム。AI が制御する領土 Base が
兵士（Minion）を生産し、固定の Path に沿って移動して隣接 Base を占拠・戦闘・建設する。
プレイヤー操作キャラクターも存在する。
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
  （`MinionCore`、`BuildingCore`、`PlayerCombatCore`）。振る舞いは別々の MonoBehaviour
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
アーキテクチャ
================================================================

----------------------------------------------------------------
起動（Bootstrapping）
----------------------------------------------------------------

`GameManager`（`Base/GameManager.cs`）が唯一のエントリーポイント。初期化は順序が重要で、
意図的に並べられている：`InitializeBases()` → `PlaceInitialBuildings()` → `InitializeNeighborTeams()`
→ `StartGameLoop()`。初期建物は隣接 Team 初期化より**前**に配置される。これにより、どの Base も
隣の Team を問い合わせる前に、各 `Cityhall` が自分を通知（`Base.AnnounceCityhall()`）できる。
起動部分に触れるときはこの順序を保つこと。`World` がシーンの `Base`・`Path` のリストを持つ。

----------------------------------------------------------------
Base とマネージャ（`Base/`）
----------------------------------------------------------------

`Base` はグリッドベースの領土（`gridSize`/`cellSize`、`GridToWorld()`）。各 Base は
`BuildingManager`（グリッド占有の辞書、配置クエリ、`CountByType` による上限、`GetCityhall`）と
`BaseAI` を持つ。`MinionManager` は生存中の兵士を追跡するだけ。

----------------------------------------------------------------
兵士システム（`Minion/`）
----------------------------------------------------------------

`MinionCore` がハブ。`IBattleInfo`（Position/Team/TakeDamage）と `IHealth`（ゲージ用）を実装し、
`Health` と `StateMachine` を持つ。毎フレーム `vision.Refresh()` の後に `stateMachine.Update()` を呼ぶ。

`StateMachine` は**優先度ベース**。毎フレーム各 `IState.CanEnter()` を尋ね、有効な状態のうち
`Priority` が最大のものを選び、遷移時に `Exit()`/`Enter()` を実行、現在の状態だけ `Tick()` する。
各 State は同じ GameObject 上の MonoBehaviour：

| State | 優先度 | 入る条件 |
|---|---|---|
| `MovingState` | 0 | 常に（フォールバック。Waypoint 移動を再開） |
| `BuildingState` | 10 | 建設対象がある |
| `CombatState` | 20 | 視界に敵がいる |
| `StaggerState` | 30 | ひるみ中（攻撃をキャンセルし固まる） |
| `DeadState` | 40 | HP 0（`Die()` を起動） |

`CombatState` が戦闘ループを駆動する：`ITargetingStrategy` でターゲットを選び、`CombatContext`
（target/inRange/canAct）を作り、`ICombatStrategy.Decide()` に `CombatAction`（Approach/Attack/Wait）を
尋ね、それを移動・攻撃の呼び出しに変換する。

`Movement` は `NavMeshAgent` をラップし `IDasher` を実装する（これにより `Dodge` は「兵士かプレイヤーか」を
意識しなくて済む）。`Attack` は windup→active→recovery の 3 フェーズタイマーで、active フェーズの間だけ
`Hitbox` を有効にする。`Occupier` は到着後の占拠挙動（中立 Cityhall の建設／敵 Cityhall の攻撃、
必要に応じた再交戦）を担う。

----------------------------------------------------------------
戦闘（`Minion/` ＋ `Common/`）
----------------------------------------------------------------

`IBattleInfo`（`MinionCore`、`BuildingCore`、`PlayerCombatCore` が実装）が、戦闘に参加する者の共通型。
ダメージは `Hitbox`（攻撃側。active フェーズ中だけ有効なトリガーコライダー。1 振りごとに多段ヒットを除去）
→ `Hurtbox`（被攻撃側。自分の所有者 `IBattleInfo` を保持。回避の i-frame 中は無効化）
→ `Owner.TakeDamage(BattleInfo)` と流れる。`BattleInfo` は `attackPower` ＋ `staggerDuration` を運ぶ。
**受け手**が自分の防御力を静的メソッド `DamageCalculator.Calc(power, defense)` で適用する。
`PriorityTargetingStrategy` は、より高優先度の `TargetCategory`（Minion > Building）が現れない限り、
現在のターゲットを維持する。

----------------------------------------------------------------
建物システム（`Building/`）
----------------------------------------------------------------

`BuildingCore`（こちらも `IBattleInfo`/`IHealth`）が `Construction`、`CityhallBehavior`、`Hurtbox` を
組み立てる。`Construction` は建設ポイントの `Resource` をラップし `OnCompleted` を発火する。
ポイントの溜まり方は `BuildStrategy` による（`AutoBuildStrategy` ＝毎秒加算、`ManualBuildStrategy` ＝
`Builder` 兵士から渡される）。`Production`（Barrack）は建設完了後、クールダウンで `MinionFactory` を
使って兵士を生産する。`CostPool` は回復する `Resource`（Cityhall の経済）で、アトミックな
`Consume`/`CanAfford` を持つ。`CityhallBehavior` がコストプールと `OnTeamChanged` を所有し、
隣接 Base に自分を通知する。

----------------------------------------------------------------
AI（`AI/`）
----------------------------------------------------------------

`BaseAI` が領土ごとの頭脳。一定間隔で `DecideBuilding`（`BuildingPriorityData` による種別上限を守りつつ、
空きセルに最も優先度の高い建てられる建物を置く）と `DecideMinion`（生産可能な各 Barrack について、
まず**プレイヤーの派遣指令**＝対象 Base ＋兵種ごとのノルマ＋有効期限タイマー、を優先的に充足し、
なければランダムな敵／中立の隣接 Base・兵種・数を選ぶ。`CostPool.Consume` でアトミックに消費）を実行する。
`ResolvePath` は目的地への `Path` を見つけ、その Waypoint 列を返す（必要なら反転）。

----------------------------------------------------------------
UI / ゲージ（`UI/`）
----------------------------------------------------------------

汎用ゲージシステム：エンティティ上の `GaugeAttacher` が `GaugeType`（Hp/Build/Stamina）ごとに
`StatBar` prefab を生成する。各 `StatBar` は親階層を探索して一致する型の `IGaugeSource`
（`HpGaugeSource`、`BuildGaugeSource`、`StaminaGaugeSource` ＝ `IHealth`/`Construction`/`Stamina` の
薄いアダプタ）を見つけてデータを解決し、fill 画像を更新、Team で色付け、カメラへビルボードする。
ゲージを追加するには：`GaugeType` を足し、`*GaugeSource : IGaugeSource` を足し、アタッチャを設定する。
`MinimapController` はワールド空間のオルソカメラを RenderTexture に描画し、プレイヤーの
クリック派遣フロー（指示元 → 派遣先 → ノルマ → 発令）を持つ。

----------------------------------------------------------------
その他のシステム
----------------------------------------------------------------

- `Path/` の `Path` ＋ `Waypoint`：Base 同士を結ぶ固定ルート。兵士は Waypoint 列を辿る。
- `Common/` の enum・値型：`Team`（None/Red/Blue）、`BuildingType`、`GaugeType`、`TargetCategory`、
  および各インターフェース定義。
- `Player/`：`PlayerCombatCore`（プレイヤーを `IBattleInfo` として扱い、`Attack`/`Dodge`/`Stamina` を再利用）、
  `PlayerMovement`（CharacterController）、`TPSCamera`（右ドラッグ回転・ホイールズーム）。
- `Camera/CameraDistanceCulling`：レイヤーごとの描画距離（物理・AI は動かしたまま見た目だけカリング）。
- `Item/`：瓶／インベントリの仕組み（`IItemEffect`、`BottleData`、`BottleItemCore`、`InventorySystem` 等）
  ＝独立した、軽めのサブシステム。
- `Editor/GrassVariationTool`：エディタ専用ツール。