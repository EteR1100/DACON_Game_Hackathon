using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class RankingUIManager : MonoBehaviour
{
    private Button _btnHome;
    private VisualElement _listContainer;
    private Label _bestScoreLabel;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _btnHome = root.Q<Button>("BtnHome");
        _listContainer = root.Q<VisualElement>("RankingListContainer");
        _bestScoreLabel = root.Q<Label>("BestScoreLabel");

        if (_btnHome != null)
        {
            _btnHome.clicked += OnBackClicked;
        }

        LoadRankings();
    }

    private void LoadRankings()
    {
        if (RankingService.Instance == null)
        {
            Debug.LogError("RankingService instance was not found.");
            return;
        }

        RankingService.Instance.GetRankings(
            onSuccess: UpdateRankingUI,
            onError: error => Debug.LogError("Ranking load failed: " + error)
        );
    }

    private void UpdateRankingUI(List<RankData> rankings)
    {
        if (_listContainer == null)
        {
            return;
        }

        _listContainer.Clear();

        for (int i = 0; i < rankings.Count; i++)
        {
            _listContainer.Add(CreateRankCard(rankings[i], i + 1));
        }

        if (_bestScoreLabel != null)
        {
            int best = PlayerPrefs.GetInt("BestScore", 0);
            _bestScoreLabel.text = best.ToString("N0");
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
