using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;
using System.Collections;

public class RankingUIManager : MonoBehaviour
{
    private Button _btnHome;
    private VisualElement _listContainer;
    private Label _lblDataPackets;

    // My Rank Sticky Bar Elements
    private Label _myRankNumber;
    private Label _myRankName;
    private Label _myRankScore;
    private Label _myRankDate;

    // Tab Elements
    private Button _tabDaily;
    private Button _tabWeekly;
    private Button _tabAll;
    private string _currentPeriod = "all";

    // Refresh Elements
    private Label _lblLastUpdate;
    private Button _btnRefresh;
    private DateTime _lastUpdateTime;

    // Error UI Elements
    private VisualElement _errorArea;
    private Button _btnRetry;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _btnHome = root.Q<Button>("BtnHome");
        _listContainer = root.Q<VisualElement>("RankingListContainer");
        _lblDataPackets = root.Q<Label>("LblDataPackets");

        // My Rank Elements
        _myRankNumber = root.Q<Label>("MyRankNumber");
        _myRankName = root.Q<Label>("MyRankName");
        _myRankScore = root.Q<Label>("MyRankScore");
        _myRankDate = root.Q<Label>("MyRankDate");

        // Tab Elements
        _tabDaily = root.Q<Button>("TabDaily");
        _tabWeekly = root.Q<Button>("TabWeekly");
        _tabAll = root.Q<Button>("TabAll");

        // Refresh Elements
        _lblLastUpdate = root.Q<Label>("LblLastUpdate");
        _btnRefresh = root.Q<Button>("BtnRefresh");

        // Error UI
        _errorArea = root.Q<VisualElement>("ErrorArea");
        _btnRetry = root.Q<Button>("BtnRetry");

        if (_btnHome != null)
        {
            _btnHome.clicked += OnBackClicked;
        }

        if (_tabDaily != null) _tabDaily.clicked += () => SwitchTab("daily");
        if (_tabWeekly != null) _tabWeekly.clicked += () => SwitchTab("weekly");
        if (_tabAll != null) _tabAll.clicked += () => SwitchTab("all");

        if (_btnRefresh != null)
        {
            _btnRefresh.clicked += () => LoadRankings(_currentPeriod);
        }

        if (_btnRetry != null)
        {
            _btnRetry.clicked += () => LoadRankings(_currentPeriod);
        }

        // Initialize My Rank with local data first
        UpdateMyRankLocal();

        // Display current coins
        if (_lblDataPackets != null)
        {
            int coins = PlayerPrefs.GetInt("Coins", 10000);
            _lblDataPackets.text = coins.ToString("N0");
        }

        SyncLocalBestScoreThenLoadRankings();
        StartCoroutine(UpdateRefreshTimeRoutine());
    }

    private void SyncLocalBestScoreThenLoadRankings()
    {
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        string nickname = PlayerPrefs.GetString("Nickname", "").Trim();

        if (bestScore <= 0 || string.IsNullOrEmpty(nickname) || RankingService.Instance == null)
        {
            LoadRankings(_currentPeriod);
            return;
        }

        RankingService.Instance.PostScore(
            nickname,
            bestScore,
            onSuccess: () => LoadRankings(_currentPeriod),
            onError: error => {
                Debug.LogError("Local best score sync failed: " + error);
                LoadRankings(_currentPeriod);
            }
        );
    }

    private IEnumerator UpdateRefreshTimeRoutine()
    {
        while (true)
        {
            UpdateRefreshTimeText();
            yield return new WaitForSeconds(60f); // Update every minute
        }
    }

    private void UpdateRefreshTimeText()
    {
        if (_lblLastUpdate == null || _lastUpdateTime == DateTime.MinValue) return;

        TimeSpan diff = DateTime.Now - _lastUpdateTime;
        if (diff.TotalSeconds < 60)
        {
            _lblLastUpdate.text = "LAST UPDATED: JUST NOW";
        }
        else
        {
            _lblLastUpdate.text = $"LAST UPDATED: {Mathf.FloorToInt((float)diff.TotalMinutes)} MIN AGO";
        }
    }

    private void SwitchTab(string period)
    {
        if (_currentPeriod == period) return;

        _currentPeriod = period;
        
        // Update Tab Visuals
        _tabDaily?.EnableInClassList("tab-active", _currentPeriod == "daily");
        _tabWeekly?.EnableInClassList("tab-active", _currentPeriod == "weekly");
        _tabAll?.EnableInClassList("tab-active", _currentPeriod == "all");

        LoadRankings(_currentPeriod);
    }

    private void UpdateMyRankLocal()
    {
        if (_myRankName != null) _myRankName.text = PlayerPrefs.GetString("Nickname", "USER");
        if (_myRankScore != null) _myRankScore.text = PlayerPrefs.GetInt("BestScore", 0).ToString("N0");
        if (_myRankNumber != null) _myRankNumber.text = "#--";
        if (_myRankDate != null)
        {
            string lastPlayDate = PlayerPrefs.GetString("LastPlayDate", "");
            _myRankDate.text = string.IsNullOrEmpty(lastPlayDate) ? "LAST PLAY: ----" : "LAST PLAY: " + lastPlayDate;
        }
    }

    private void LoadRankings(string period)
    {
        if (RankingService.Instance == null)
        {
            Debug.LogError("RankingService instance was not found.");
            return;
        }

        // Show loading state on retry button if error area is visible
        if (_errorArea != null && _errorArea.style.display == DisplayStyle.Flex)
        {
            if (_btnRetry != null)
            {
                _btnRetry.text = "ESTABLISHING CONNECTION...";
                _btnRetry.SetEnabled(false);
            }
        }

        RankingService.Instance.GetRankings(
            period,
            onSuccess: rankings => {
                _lastUpdateTime = DateTime.Now;
                UpdateRefreshTimeText();
                UpdateRankingUI(rankings);
                UpdateMyRankFromServer(rankings);
                if (_errorArea != null) _errorArea.style.display = DisplayStyle.None;
                
                // Reset button state
                if (_btnRetry != null)
                {
                    _btnRetry.text = "RE-ESTABLISH CONNECTION";
                    _btnRetry.SetEnabled(true);
                }
            },
            onError: error => {
                Debug.LogError("Ranking load failed: " + error);
                if (_errorArea != null) _errorArea.style.display = DisplayStyle.Flex;
                if (_listContainer != null) _listContainer.Clear();

                // Reset button state on failure so user can try again
                if (_btnRetry != null)
                {
                    _btnRetry.text = "RE-ESTABLISH CONNECTION";
                    _btnRetry.SetEnabled(true);
                }
            }
        );
    }

    private void UpdateMyRankFromServer(List<RankData> rankings)
    {
        string myName = PlayerPrefs.GetString("Nickname", "");
        if (string.IsNullOrEmpty(myName)) return;

        int myRank = -1;
        RankData myData = null;

        for (int i = 0; i < rankings.Count; i++)
        {
            if (rankings[i].name == myName)
            {
                myRank = i + 1;
                myData = rankings[i];
                break;
            }
        }

        if (myRank != -1 && myData != null)
        {
            if (_myRankNumber != null) _myRankNumber.text = "#" + myRank;
            if (_myRankName != null) _myRankName.text = myData.name;
            if (_myRankScore != null) _myRankScore.text = myData.score.ToString("N0");
            if (_myRankDate != null) _myRankDate.text = "RECORDED: " + myData.date;
        }
        else
        {
            // If not in top rankings
            if (_myRankNumber != null) _myRankNumber.text = "#--";
            if (_myRankDate != null)
            {
                string lastPlayDate = PlayerPrefs.GetString("LastPlayDate", "");
                _myRankDate.text = string.IsNullOrEmpty(lastPlayDate) ? "LAST PLAY: ----" : "LAST PLAY: " + lastPlayDate;
            }
        }
    }

    private void UpdateRankingUI(List<RankData> rankings)
    {
        if (_listContainer == null)
        {
            return;
        }

        _listContainer.Clear();

        if (rankings.Count == 0)
        {
            Label emptyLabel = new Label("NO DATA FOUND FOR THIS PERIOD");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.color = new Color(1, 1, 1, 0.5f);
            emptyLabel.style.marginTop = 50;
            _listContainer.Add(emptyLabel);
            return;
        }

        for (int i = 0; i < rankings.Count; i++)
        {
            _listContainer.Add(CreateRankCard(rankings[i], i + 1));
        }
    }

    private VisualElement CreateRankCard(RankData data, int rankNum)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("rank-card");

        if (rankNum == 1)
        {
            card.AddToClassList("card-gold");
        }
        else if (rankNum == 2)
        {
            card.AddToClassList("card-silver");
        }
        else if (rankNum == 3)
        {
            card.AddToClassList("card-bronze");
        }
        else if (rankNum % 2 == 0)
        {
            card.AddToClassList("rank-card-alt");
        }

        VisualElement leftGroup = new VisualElement();
        leftGroup.AddToClassList("rank-left-group");

        Label rankLabel = new Label("#" + rankNum);
        rankLabel.AddToClassList("rank-number");
        rankLabel.AddToClassList(GetRankToneClass(rankNum));
        leftGroup.Add(rankLabel);

        VisualElement avatar = new VisualElement();
        avatar.AddToClassList("rank-avatar");
        avatar.AddToClassList(GetAvatarClass(rankNum));
        leftGroup.Add(avatar);

        VisualElement info = new VisualElement();
        info.AddToClassList("rank-info");

        Label nameLabel = new Label(data.name);
        nameLabel.AddToClassList("rank-name-text");
        nameLabel.AddToClassList(GetRankToneClass(rankNum));
        info.Add(nameLabel);

        Label dateLabel = new Label(data.date);
        dateLabel.AddToClassList("rank-date-text");
        info.Add(dateLabel);
        leftGroup.Add(info);

        VisualElement scoreGroup = new VisualElement();
        scoreGroup.AddToClassList("rank-score-group");

        Label scoreCaption = new Label("RUN SCORE");
        scoreCaption.AddToClassList("rank-score-caption");
        scoreGroup.Add(scoreCaption);

        Label scoreLabel = new Label(data.score.ToString("N0"));
        scoreLabel.AddToClassList("rank-score-text");
        scoreLabel.AddToClassList(GetRankToneClass(rankNum));
        scoreGroup.Add(scoreLabel);

        Label coinLabel = new Label("DATA +" + Mathf.Max(data.score / 25, 0).ToString("N0"));
        coinLabel.AddToClassList("rank-coin-text");
        scoreGroup.Add(coinLabel);

        card.Add(leftGroup);
        card.Add(scoreGroup);

        return card;
    }

    private string GetRankToneClass(int rankNum)
    {
        if (rankNum == 1) return "rank-tone-gold";
        if (rankNum == 2) return "rank-tone-silver";
        if (rankNum == 3) return "rank-tone-bronze";
        return "rank-tone-blue";
    }

    private string GetAvatarClass(int rankNum)
    {
        if (rankNum == 1) return "avatar-rainbow";
        if (rankNum == 2) return "avatar-yellow";
        if (rankNum == 3) return "avatar-purple";
        return "avatar-cyan";
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("MainScene");
    }
}
