using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class GameController : MonoBehaviour
{
    public List<Text> myCardText;
    public Text scoreText;
    public Text fieldCardText;
    public Text remCardText;
    public Text useCardText;
    public Text nextPlayerText;
    public GameObject cardCover;
    public GameObject code5Obj;
    public GameObject scorePrefab;

    [Header("追加UI分")]
    public GameObject card_Parent;
    public GameObject card_Content;
    public Color[] cardColor;
    public Text scoreAddDisplayText;
    public GameObject score_Parent;
    public GameObject score_Content;
    public GameObject result_Parent;
    public GameObject result_Content;

    public GameObject game;
    public GameObject result;

    private int gameTurn;
    private bool isDisplay;
    private int deckCount;
    private int fieldValue;

    private int Turn
    {
        get { return turn; }
        set
        {
            if (value == 0) value = peopleCount;
            this.turn = value;
        }
    }//自分が回答者になるまでのターン
    private int myNumber;//1から
    private int turn;
    private int score;
    private int usedCardCount;//0から
    private int emptyCardNumber;
    private int startCardCount;
    private int peopleCount;
    private int myTurnCount;
    private int code5;

    public int allCardCount = 80;
    private const int getScooreCount = 10;

    private bool isTurn = false;
    private bool deckEnd = false;
    private bool isMaster;

    private List<int> allDeckCount = new List<int>() { 7, 8, 8, 8, 8, 5, 8, 8, 8, 8, 4 };
    private List<int> deck = new List<int>();
    private List<int> myCard = new List<int>();
    private List<int> fieldCard = new List<int>();
    private Dictionary<int, int> scoreList = new Dictionary<int, int>();

    private List<Text> scoreTextList = new List<Text>();
    private PhotonView view;
    private void Awake()
    {
        if (!PhotonNetwork.InRoom)//ルームにいなければば
        {
            SceneManager.LoadScene("TitleScene");
        }
    }
    private void Start()
    {
        view = GetComponent<PhotonView>();
        isMaster = PhotonNetwork.IsMasterClient;
        myNumber = PhotonNetwork.PlayerList.ToList().IndexOf(PhotonNetwork.LocalPlayer) + 1;
        peopleCount = PhotonNetwork.PlayerList.Length;
        print(myNumber);
        Turn = myNumber;


        for (int i = 1; i < peopleCount+1; i++)//部屋の人数分必要なものの初期化
        {
            scoreList.Add(i, 0);
        }
        for (int i = 0; i < PhotonNetwork.PlayerListOthers.Length; i++)//自分以外の人数分必要な物
        {
            scoreTextList.Add(Instantiate(scorePrefab.transform).GetComponentInChildren<Text>());//自分以外のスコア表示用テキスト
            scoreTextList[i].text = $"{PhotonNetwork.PlayerListOthers[i].NickName} : 0";
        }
        //マスターだけの処理
        if (isMaster)
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < allDeckCount[i]; j++)
                {
                    deck.Add(i);
                }
            }
            deck = deck.Select(i => i).OrderBy(i => Guid.NewGuid()).ToList();
            for (int i = 1; i < peopleCount + 1; i++)//自分含むのプレイヤーに
            {
                for (int j = 0; j < 3; j++)//3枚
                {
                    view.RPC("GiveCard", RpcTarget.AllViaServer, deck[j], i);//カードを与える
                    deck.RemoveAt(j);
                    usedCardCount++;
                }
            }
            isTurn = true;
            view.RPC("SetNextPlayerText", RpcTarget.Others, PhotonNetwork.NickName);
            print("ますたー");
        }
        cardCover.SetActive(!isTurn);
        scoreText.text = "0";
        fieldCardText.text = "";

        foreach (Transform obj in card_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        foreach (Transform obj in score_Parent.transform)
        {
            Destroy(obj.gameObject);
        }
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            var content = Instantiate(score_Content, score_Parent.transform);

            content.transform.Find("Text_Name").GetComponent<Text>().text = PhotonNetwork.PlayerList[i].NickName + "：";
            content.transform.Find("Text_Score").GetComponent<Text>().text = 0 + "点";
        }

        gameTurn = 0;
        scoreAddDisplayText.gameObject.SetActive(false);
        isDisplay = false;
        deckCount = deck.Count;
        remCardText.text = $"{allCardCount - usedCardCount}";
    }

    //シーンから呼び出す
    public async void UseCard(int index)
    {
        emptyCardNumber = index;
        myCardText[index].text = "";
        myTurnCount++;
        if (myCard[index] == 5 && fieldValue >= 5)
        {
            var pos = code5Obj.transform.localPosition;
            switch (index)
            {
                case 0:
                    pos.x = -260;
                    break;
                case 1:
                    pos.x = 0;
                    break;
                case 2:
                    pos.x = 260;
                    break;
            }
            code5Obj.transform.localPosition = pos;

            code5Obj.SetActive(true);
            cardCover.SetActive(true);
            await UniTask.WaitUntil(() => code5 != 0);
            myCard[index] *= code5;
            code5 = 0;
        }

        gameTurn++;
        view.RPC("OneGameTurnValueShare", RpcTarget.AllViaServer, gameTurn);
        view.RPC("PutPlay", RpcTarget.MasterClient, myCard[index]);
    }
    public void PlusOrMinus(int code)// +1 or -1
    {
        code5 = code;
        code5Obj.SetActive(false);
    }
    [PunRPC]
    private async void TurnEnd(Array fieldCardArr, int usedCardCount)
    {
        List<int> fieldCard = new List<int>();
        foreach (var v in fieldCardArr)
        {
            fieldCard.Add((int)v);
        }
        fieldCardText.text = string.Join("+", fieldCard);

        //カードオブジェクト追加
        foreach (Transform obj in card_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        for(int c = 0; c < fieldCard.Count; c++)
        {
            if(gameTurn - 1 < c)
            {
                await UniTask.Delay(500);
            }

            var content = Instantiate(card_Content, card_Parent.transform);

            var background = content.transform.Find("BackGround");

            background.Find("Text_Num1").GetComponent<Text>().text = fieldCard[c].ToString();
            background.Find("Text_Num2").GetComponent<Text>().text = fieldCard[c].ToString();

            int value = 0;
            for (int cardValue = 0; cardValue - 1 < c; cardValue++)
            {
                value += fieldCard[cardValue];
            }

            background.Find("Text_Num3").GetComponent<Text>().text = value.ToString();

            if (gameTurn - 1 > c)
            {
                content.GetComponent<Animator>().enabled = false;
            }
            else
            {
                background.Find("Text_Num3").GetComponent<Text>().color = new Color32(255, 79, 73, 255);
            }

            background.GetComponent<Image>().color = cardColor[c % 2];
        }


        if (Turn == 1 && deckEnd)//自分のターンだった人の処理
        {
            myCardText[emptyCardNumber].text = "";
        }
        Turn--;
        if (Turn == 1)//自分のターンになった人の処理
        {
            isTurn = true;
            nextPlayerText.text = $"自分のターンです";
            view.RPC("SetNextPlayerText", RpcTarget.Others, PhotonNetwork.NickName);
        }//カードを選択できるようにする
        else
        {
            isTurn = false;
        }//カードを選択できないようにする
        cardCover.SetActive(!isTurn);
        remCardText.text = $"{allCardCount - usedCardCount}";
        useCardText.text = $"{usedCardCount + 1}ターン経過";
    }
    [PunRPC]
    private void SetNextPlayerText(string playerName)
    {
        nextPlayerText.text = $"現在は{playerName}さんです";
    }
    //カードを与える処理
    [PunRPC]
    private void GiveCard(int value, int turn)
    {
        if (Turn == turn)
        {
            Text cardText;
            if (startCardCount < 3)
            {
                cardText = myCardText[startCardCount];
                myCard.Add(value);
                startCardCount++;
            }
            else
            {
                cardText = myCardText[emptyCardNumber];
                myCard[emptyCardNumber] = value;
            }
            cardText.text = value.ToString();
        }
    }
    [PunRPC]
    private async void AddScore(int score, int turn, Array fieldCardArr)
    {
        cardCover.SetActive(true);

        List<int> fieldCard = new List<int>();
        foreach (var v in fieldCardArr)
        {
            fieldCard.Add((int)v);
        }
        fieldCardText.text = string.Join("+", fieldCard);

        //カードオブジェクト追加
        foreach (Transform obj in card_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        for (int c = 0; c < fieldCard.Count; c++)
        {
            if (gameTurn - 1 < c)
            {
                await UniTask.Delay(500);
            }

            var content = Instantiate(card_Content, card_Parent.transform);

            var background = content.transform.Find("BackGround");

            background.Find("Text_Num1").GetComponent<Text>().text = fieldCard[c].ToString();
            background.Find("Text_Num2").GetComponent<Text>().text = fieldCard[c].ToString();

            int value = 0;
            for (int cardValue = 0; cardValue - 1 < c; cardValue++)
            {
                value += fieldCard[cardValue];
            }

            background.Find("Text_Num3").GetComponent<Text>().text = value.ToString();

            if (gameTurn - 1 > c)
            {
                content.GetComponent<Animator>().enabled = false;
            }
            else
            {
                background.Find("Text_Num3").GetComponent<Text>().color = new Color32(255, 79, 73, 255);
            }

            background.GetComponent<Image>().color = cardColor[c % 2];
        }

        await UniTask.Delay(2500);
        foreach (Transform obj in card_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        if (Turn == turn)
        {
            this.score += score;
            scoreText.text = this.score.ToString();
            view.RPC("AddScoreDisplay", RpcTarget.AllViaServer, score, myNumber);
            view.RPC("ScoreListUpdate", RpcTarget.AllViaServer, score, myNumber);
        }
    }
    [PunRPC]
    private void ScoreListUpdate(int score, int playerId)
    {
        foreach(Transform obj in score_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        scoreList[playerId] += score;//スコア加算
        //List<int> otherScoreList = new List<int>(scoreList
        //    .Where(i=>i.Key!=myNumber)//自分以外の
        //    .Select(i => i.Value)//Valueだけの
        //    .ToList());//リスト
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            var content = Instantiate(score_Content, score_Parent.transform);

            content.transform.Find("Text_Name").GetComponent<Text>().text = PhotonNetwork.PlayerList[i].NickName + "：";
            content.transform.Find("Text_Score").GetComponent<Text>().text = scoreList[i + 1].ToString() + "点";

            if(deckCount <= 12)
            {
                content.transform.Find("Text_Score").GetComponent<Text>().text = "？点";
            }
        }

        print("デッキカウント:"+deckCount);
    }
    [PunRPC]
    private async void AddScoreDisplay(int _score, int myNumber)
    {
        scoreAddDisplayText.gameObject.SetActive(true);
        scoreAddDisplayText.text = $"{PhotonNetwork.PlayerList[myNumber - 1].NickName}の得点です！\n{_score}ポイント追加されました。";
        await UniTask.Delay(1000);
        scoreAddDisplayText.gameObject.SetActive(false);
        isDisplay = true;
    }
    //山札がなくなた時
    [PunRPC]
    private void DeckEnd()
    {
        deckEnd = true;
    }
    [PunRPC]
    private void GameEnd()
    {
        print("おわた");

        game.SetActive(false);
        result.SetActive(true);

        int maxPoint = 0;
        List<int> winnerId = new List<int>();
        for(int i = 1; i < scoreList.Count + 1; i++)
        {
            if(maxPoint == scoreList[i])
            {
                winnerId.Add(i);
                maxPoint = scoreList[i];
            }
            else if(maxPoint < scoreList[i])
            {
                winnerId.Clear();
                winnerId.Add(i);
                maxPoint = scoreList[i];
            }
        }

        foreach(Transform obj in result_Parent.transform)
        {
            Destroy(obj.gameObject);
        }

        foreach (var kvp in scoreList)
        {
            print($"プレイヤー{kvp.Key}：{kvp.Value}");

            var content = Instantiate(result_Content, result_Parent.transform);

            content.transform.Find("Text_Name").GetComponent<Text>().text = PhotonNetwork.PlayerList[kvp.Key - 1].NickName + "：";
            content.transform.Find("Text_Score").GetComponent<Text>().text = kvp.Value + "点";

            if (winnerId.Contains(kvp.Key))
            {
                content.transform.Find("Image").gameObject.SetActive(true);
            }
            else
            {
                content.transform.Find("Image").gameObject.SetActive(false);
            }
        }
        cardCover.SetActive(true);

        var wait = StartCoroutine(WaitAns());
        var notWait = StartCoroutine(NotWaitAns());
        IEnumerator WaitAns()//他プレイヤーの継続可否待ち
        {
            yield return new WaitUntil(() => nextGamePeople == ConnectingScript.peopleStaticNum);//人数が揃ったら
            nextGamePeople = notNextGamePeople = 0;
            print("waitAns");
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);//MainSceneLoad
        }
        IEnumerator NotWaitAns()
        {
            yield return new WaitUntil(() => 0 < notNextGamePeople);//続ける人が一人でもいなかったら
            //続ける人が一人でもいないことを伝えて
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadSceneAsync("TitleScene");//タイトル画面に戻る
        }
    }

    //MasterOnly

    //場に出した時に呼ぶ
    [PunRPC]
    private async void PutPlay(int value)
    {
        //得点処理

        fieldCard.Add(value);
        int sum = fieldCard.Sum();
        //if (fieldCard.Contains(0))//0が含まれていたら
        //{
        //    sum = fieldCard
        //        .Skip(fieldCard.LastIndexOf(0))//0以降を
        //        .Sum();//足す
        //}
        if (getScooreCount <= sum)//誰かの得点になったら
        {
            int scoreGetPlayer = 1;//今行動したプレイヤー
            if (getScooreCount < sum)//バーストしていたら
            {
                scoreGetPlayer = PhotonNetwork.PlayerList.Length;//前回行動したプレイヤー
            }
            view.RPC("AddScore", RpcTarget.AllViaServer, fieldCard.Count, scoreGetPlayer, fieldCard.ToArray());//スコアを付与する
            fieldCard.Clear();//場のカードをなくす

            await UniTask.WaitUntil(() => isDisplay == true);
            isDisplay = false;
            gameTurn = 0;
        }

        view.RPC("OneGameTurnValueShare", RpcTarget.AllViaServer, gameTurn);
        view.RPC("FieldValueShare", RpcTarget.AllViaServer, fieldCard.Sum());

        usedCardCount++;

        //次のターンへの処理
        if (usedCardCount == allCardCount)//全てのカードが使われたら
        {
            //終了処理
            view.RPC("GameEnd", RpcTarget.AllViaServer);
        }
        else
        {
            //カード付与
            if (deck.Count != 0)//カードが山札にあれば
            {
                view.RPC("GiveCard", RpcTarget.AllViaServer, deck[0], 1);
                deck.RemoveAt(0);

                deckCount = allCardCount - usedCardCount;
                view.RPC("DeckShare", RpcTarget.AllViaServer, deckCount);
            }
            else
            {
                view.RPC("", RpcTarget.AllViaServer);
            }
            //次のターンの処理
            view.RPC("TurnEnd", RpcTarget.AllViaServer, fieldCard.ToArray(), usedCardCount);
        }
    }

    //一ゲームターン数共有
    [PunRPC]
    private void OneGameTurnValueShare(int turnValue)
    {
        gameTurn = turnValue;
    }
    //デッキ内容共有
    [PunRPC]
    private void DeckShare(int count)
    {
        deckCount = count;
    }
    //フィールド合計値共有
    [PunRPC]
    private void FieldValueShare(int value)
    {
        fieldValue = value;
    }

    public GameObject restartButton;
    private int nextGamePeople;//次のゲームに進む人数
    private int notNextGamePeople;//次のゲームに進まない人数

    //リスタート
    public void ReStart(bool isReStart)
    {
        view.RPC("NextGameSignal", RpcTarget.All, isReStart);
        restartButton.SetActive(false);
    }

    [PunRPC]
    private void NextGameSignal(bool isNextGame)
    {
        if (isNextGame)
        {
            nextGamePeople++;
        }
        else
        {
            notNextGamePeople++;
            ConnectingScript.connect = false;
        }
    }
}