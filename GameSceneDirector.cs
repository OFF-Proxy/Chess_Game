using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameSceneDirector : MonoBehaviour
{
    // ゲーム設定
    public const int TILE_X = 8;
    public const int TILE_Y = 8;
    const int PLAYER_MAX = 2;

    // タイルのプレハブ
    public GameObject[] prefabTile;
    // カーソルのプレハブ
    public GameObject prefabCursor;

    // 内部データ
    GameObject[,] tiles;
    UnitController[,] units;

    // ユニットのプレハブ（色ごと）
    public List<GameObject> prefabWhiteUnits;
    public List<GameObject> prefabBlackUnits;
    //　1 = ポーン 2 = ルーク 3 = ナイト 4 = ビショップ 5 = クイーン 6 = キング
    public int[,] unitType = 
    {
        { 2, 1, 0, 0, 0, 0, 11, 12 },
        { 3, 1, 0, 0, 0, 0, 11, 13 },
        { 4, 1, 0, 0, 0, 0, 11, 14 },
        { 5, 1, 0, 0, 0, 0, 11, 15 },
        { 6, 1, 0, 0, 0, 0, 11, 16 },
        { 4, 1, 0, 0, 0, 0, 11, 14 },
        { 3, 1, 0, 0, 0, 0, 11, 13 },
        { 2, 1, 0, 0, 0, 0, 11, 12 },
    };

    // UI関連
    GameObject txtTurnInfo;
    GameObject txtResultInfo;
    GameObject btnApply;
    GameObject btnCancel;

    //選択中のユニット
    UnitController selectUnit;

    //移動関連
    List<Vector2Int> movableTiles;
    List<GameObject> cursors;

    //モード
    enum MODE
    {
        NONE,
        CHECK_MATE,
        NOMAL,
        STATUS_UPDATE,
        TURN_CHANGE,
        RESULT,
    }

    MODE nowMode, nextMode;
    int nowPlayer;

    // Start is called before the first frame update
    void Start()
    {
        //UIオブジェクト取得
        txtTurnInfo = GameObject.Find("TextTurnInfo");
        txtResultInfo = GameObject.Find("TextResultInfo");
        btnApply = GameObject.Find("ButtonApply");
        btnCancel = GameObject.Find("ButtonCancel");

        //リザルト系は消しとく
        btnApply.SetActive(false);
        btnCancel.SetActive(false);

        //移動関連
        cursors = new List<GameObject>();

        // 内部データ
        tiles = new GameObject[TILE_X, TILE_Y];
        units = new UnitController[TILE_X, TILE_Y];

        for (int i = 0; i < TILE_X; i++)
        {
            for (int j = 0; j < TILE_Y; j++)
            {
                // タイルとユニットのポジション
                float x = i - TILE_X / 2.0f;
                float y = j - TILE_Y / 2.0f;


                Vector3 pos = new Vector3(x, 0, y);

                // 作成
                int idx = (i + j) % 2;
                GameObject tile = Instantiate(prefabTile[idx], pos, Quaternion.identity);

                tiles[i, j] = tile;

                //ユニットの作成
                int type    = unitType[i, j] % 10;
                int player  = unitType[i, j] / 10;

                GameObject prefab = getPrefabUnit(player, type);
                GameObject unit = null;
                UnitController ctrl = null;

                if(null == prefab) continue;

                pos.y += 1.5f;
                unit = Instantiate(prefab);

                //初期設定
                ctrl = unit.GetComponent<UnitController>();
                ctrl.SetUnit(player, (UnitController.TYPE)type, tile);

                //内部データセット
                units[i, j] = ctrl;
            }
        }

        //初期モード
        nowPlayer = -1;
        nowMode   = MODE.NONE;
        nextMode  = MODE.TURN_CHANGE;
    }

    // Update is called once per frame
    void Update()
    {
        if(MODE.CHECK_MATE == nowMode)
        {
            checkMateMode();
        }
        else if(MODE.NOMAL == nowMode)
        {
            normalMode();
        }
        else if( MODE.STATUS_UPDATE == nowMode)
        {
            statusUpdateMode();
        }
        else if(MODE.TURN_CHANGE == nowMode)
        {
            turnChangeMode();
        }

        //モード変更
        if(MODE.NONE != nextMode)
        {
            nowMode = nextMode;
            nextMode = MODE.NONE;
        }

    }

    void checkMateMode()
    {
        nextMode = MODE.NOMAL;
    }

    void normalMode()
    {
        GameObject tile= null;
        UnitController unit = null;

        //プレイヤー
        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //ユニットにもあたり判定があるのでヒットした全てのオブジェクト情報を取得
            foreach(RaycastHit hit in Physics.RaycastAll(ray))
            {
                if(hit.transform.name.Contains("Tile"))
                {
                    tile = hit.transform.gameObject;
                    break;
                }
            }
        }

        //タイルが押されていなければ処理しない
        if(null == tile) return;

        //選んだタイルからユニットを取得
        Vector2Int tilepos = new Vector2Int(
            (int)tile.transform.position.x + TILE_X / 2,
            (int)tile.transform.position.z + TILE_Y / 2);

        //タイルに乗ってるユニット
        unit = units[tilepos.x, tilepos.y];

        //ユニット選択
        if(     null        != unit
            &&  selectUnit  != unit
            &&  nowPlayer   == unit.Player)
        {
            //移動可能範囲をセット
            movableTiles = new List<Vector2Int>();
            movableTiles = getMovableTiles(unit);

            //選択不可能
            if(1 > movableTiles.Count) return;

            setSelectCursors(unit);
        }
        //移動
        else if( null != selectUnit && movableTiles.Contains(tilepos))
        {
            moveUnit(selectUnit, tilepos);
            nextMode = MODE.STATUS_UPDATE;
        }
    }

    //移動後の処理
    void statusUpdateMode()
    {
        // TODO キャスリング

        // TODO アンパッサン

        // TODO プロモーション

        //ターン経過
        foreach(var v in getUnits(nowPlayer))
        {
            v.ProgressTurn();
        }

        //カーソル
        setSelectCursors();

        nextMode = MODE.TURN_CHANGE;
    }

    //相手のターン変更
    void turnChangeMode()
    {
        //ターン処理
        nowPlayer = getNextPlayer();

        //Info更新
        txtTurnInfo.GetComponent<Text>().text = "" + (nowPlayer + 1) + "Pの番です";

        // TODO 経過ターン（1P側に来たら+1）
        if(0 == nowPlayer)
        {

        }

        nextMode = MODE.CHECK_MATE;
    }

    int getNextPlayer()
    {
        int next = nowPlayer + 1;
        if (PLAYER_MAX <= next) next = 0;

        return next;
    }

    //指定されたプレイヤーのユニット取得
    List<UnitController> getUnits(int player = -1)
    {
        List<UnitController> ret = new List<UnitController>();

        foreach(var v in units)
        {
            if(null == v)continue;

            if(player == v.Player)
            {
                ret.Add(v);
            }
            else if(0 > player)
            {
                ret.Add(v);
            }
        }

        return ret;
    }

    //移動可能範囲取得
    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        //通常移動可能範囲を返す
        return unit.GetMovableTiles(units);
    }

    void setSelectCursors(UnitController unit=null, bool setunit = true)
    {
        // TODO カーソル解除
        foreach(var v in cursors)
        {
            Destroy(v);
        }

        //選択ユニットの非選択状態
        if(null != selectUnit)
        {
            selectUnit.SelectUnit(false);
            selectUnit = null;
        }

        //何もセットしないなら終わり
        if(null == unit) return;

        // カーソル作成
        foreach (var v in getMovableTiles(unit))
        {
            Vector3 pos = tiles[v.x, v.y].transform.position;
            pos.y += 0.51f;
            GameObject obj = Instantiate(prefabCursor, pos, Quaternion.identity);
            cursors.Add(obj);
        }

        //選択状態
        if(setunit)
        {
            selectUnit = unit;
            selectUnit.SelectUnit(setunit);
        }
    }

    bool moveUnit(UnitController unit, Vector2Int tilepos)
    {
        Vector2Int unitpos = unit.Pos;

        // まず、移動先のタイルに既に別の駒があるかチェックし、あれば消去
        if (units[tilepos.x, tilepos.y] != null && units[tilepos.x, tilepos.y] != unit)
        {
            Destroy(units[tilepos.x, tilepos.y].gameObject);
        }

        // 次に、新しい場所へユニットを移動
        unit.MoveUnit(tiles[tilepos.x, tilepos.y]);

        // 配列データの更新（元の場所を空にする）
        units[unitpos.x, unitpos.y] = null;

        // 配列データの更新（新しい場所にユニットをセット）
        units[tilepos.x, tilepos.y] = unit;

        return true;
    }


    // ユニットのプレハブを返す
    GameObject getPrefabUnit(int player, int type)
    {
        int idx = type -1;
        if (0 > idx) return null;
        GameObject prefab = prefabWhiteUnits[idx];
        if( 1 == player ) prefab = prefabBlackUnits[idx];

        return prefab;
    }
}
