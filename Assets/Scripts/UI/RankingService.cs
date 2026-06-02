using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class RankData
{
    public string name;
    public int score;
    public string date;
}

[Serializable]
public class RankListWrapper
{
    public List<RankData> rankings;
}

public class RankingService : MonoBehaviour
{
    public static RankingService Instance { get; private set; }

    // 서버 주소 (로컬 테스트용)
    // 모바일 기기 테스트 시에는 localhost 대신 컴퓨터의 IP 주소를 적어야 합니다.
    private string _baseUrl = "http://localhost:3000/ranking";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1. 랭킹 데이터 가져오기 (GET)
    public void GetRankings(string period, Action<List<RankData>> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRankingsRoutine(period, onSuccess, onError));
    }

    private IEnumerator GetRankingsRoutine(string period, Action<List<RankData>> onSuccess, Action<string> onError)
    {
        string url = _baseUrl + "?period=" + period;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // JSON 파싱 (Array 형태의 JSON을 처리하기 위해 Wrapper나 Helper 필요)
                string jsonResponse = webRequest.downloadHandler.text;
                
                // Unity의 JsonUtility는 루트가 배열인 JSON을 바로 지원하지 않으므로 감싸줍니다.
                string wrappedJson = "{\"rankings\":" + jsonResponse + "}";
                RankListWrapper wrapper = JsonUtility.FromJson<RankListWrapper>(wrappedJson);
                
                onSuccess?.Invoke(wrapper.rankings);
            }
            else
            {
                onError?.Invoke(webRequest.error);
            }
        }
    }

    // 2. 점수 등록하기 (POST)
    public void PostScore(string name, int score, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(PostScoreRoutine(name, score, onSuccess, onError));
    }

    private IEnumerator PostScoreRoutine(string name, int score, Action onSuccess, Action<string> onError)
    {
        RankData newData = new RankData { name = name, score = score };
        string json = JsonUtility.ToJson(newData);

        using (UnityWebRequest webRequest = new UnityWebRequest(_baseUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(webRequest.error);
            }
        }
    }
}
