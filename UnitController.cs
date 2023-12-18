using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    // このユニットのプレイヤー番号
    public int Player;
    // ユニットの種類
    public TYPE Type;
    // 置いてからの経過ターン
    public int ProgressTurnCount;
    // 置いてる場所
    public Vector2Int Pos, OldPos;
    // 移動状態
    public List<STATUS> Status;

    // 1 = ポーン 2 = ルーク 3 = ナイト 4 = ビショップ 5 = クイーン 6 = キング
    public enum TYPE
    {
        NONE = -1,
        PAWN = 1,
        ROOK,
        KNIGHT,
        BISHOP,
        QUEEN,
        KING,
    }

    // 移動状態
    public enum STATUS
    {
        NONE= -1,
        QSIDE_CASTLING=1,
        KSIDE_CASTLING,
        EN_PASSANT,
        CHECK,
    }

    // Start is called before the first frame update
    void Start()
    {
        ProgressTurnCount = -1;
        Status = new List<STATUS>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //初期設定
    public void SetUnit(int player, TYPE type, GameObject tile)
    {
        Player = player;
        Type = type;
        MoveUnit(tile);
        ProgressTurnCount = -1; //初動に戻す
    }

    //行動可能範囲の取得
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkking = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //クイーン
        if(TYPE.QUEEN == Type)
        {
            //ルークとビショップの動きを合成
            //ret = GetMovableTiles(units, TYPE.ROOK);
            //ret += GetMovableTiles(units, TYPE.BISHOP);
        }
        //キング
        else if(TYPE.KING == Type)
        {

        }
        else
        {
            ret = GetMovableTiles(units, Type);
        }

        return ret;
    }

    // 指定されたタイプの移動可能範囲を返す
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, TYPE type)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //ポーン
        if(TYPE.PAWN == type)
        {
            int dir = 1;
            if (1 == Player) dir = -1;

            //前方2マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                //dirで反対側の前方に直す
                new Vector2Int(0, 1 * dir),
                new Vector2Int(0, 2 * dir),
            };

            //2回目以降は1マスしか進めない
            if (-1 < ProgressTurnCount) vec.RemoveAt(vec.Count - 1);

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;
                //他の駒があったら進めない
                if(null != units[checkpos.x, checkpos.y]) break;

                ret.Add(checkpos);
            }

            //取れる時だけ斜めに進める
            vec = new List<Vector2Int>()
            {
                //dirで反対側の前方に直す
                new Vector2Int(-1, 1 * dir),
                new Vector2Int(1, 1 * dir),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;

                // TODO アンパッサン（追加して終了）

                //なにもしない
                if (null == units[checkpos.x, checkpos.y]) continue;

                //自軍のユニットは無視
                if(Player ==units[checkpos.x, checkpos.y].Player) continue;

                //ここまできたら追加
                ret.Add(checkpos);
            }
        }

        return ret;
    }

    bool isCheckable(UnitController[,] ary, Vector2Int idx)
    {
        if(     idx.x < 0 || ary.GetLength(0) <= idx.x
            ||  idx.y < 0 || ary.GetLength(1) <= idx.y)
        {
            return false;
        }

        return true;
    }

    //選択された時の処理
    public void SelectUnit(bool select = true)
    {
        Vector3 pos = transform.position;
        pos.y += 2;
        GetComponent<Rigidbody>().isKinematic = true;

        //選択解除
        if (!select)
        {
            pos.y = 1.35f;
            GetComponent<Rigidbody>().isKinematic = false;
        }

        transform.position = pos;
    }

    //移動処理
    public void MoveUnit(GameObject tile)
    {
        //移動時は非選択状態
        SelectUnit(false);

        //タイルのポジションから配列の番号に戻す
        Vector2Int idx = new Vector2Int(
            (int)tile.transform.position.x + GameSceneDirector.TILE_X / 2,
            (int)tile.transform.position.z + GameSceneDirector.TILE_Y / 2);

        //新しい場所へ
        Vector3 pos = tile.transform.position;
        pos.y = 1.35f;
        transform.position = pos;

        //移動状態をリセット
        Status.Clear();

        //あんぱっさん等の処理
        if(TYPE.PAWN == Type)
        {
            //縦に2タイル進んだ時
            if(1 < Mathf.Abs(idx.y - Pos.y))
            {
                Status.Add(STATUS.EN_PASSANT);
            }

            //移動した一歩前に残像が残る
            int dir = -1;
            if(1 == Player) dir = 1;

            Pos.y = idx.y + dir;
        }
        //キャスリング
        else if( TYPE.KING == Type)
        {
            //横に2タイル進んだら
            if(1 < idx.x - Pos.x)
            {
                Status.Add(STATUS.KSIDE_CASTLING);
            }
            else if( -1 < idx.x - Pos.x)
            {
                Status.Add(STATUS.QSIDE_CASTLING);
            }
        }

        //インデックスの更新
        OldPos = Pos;
        Pos = idx;

        //置いてからの経過ターンをリセット
        ProgressTurnCount = 0;
    }

    //前回移動してからのターンをカウントする
    public void ProgressTurn()
    {
        //初動は無視
        if(0 > ProgressTurnCount) return;

        //アンパッサンフラグチェック
        if(Type == TYPE.PAWN)
        {
            if( 1 < ProgressTurnCount)
            {
                Status.Remove(STATUS.EN_PASSANT);
            }
        }
    }
}
