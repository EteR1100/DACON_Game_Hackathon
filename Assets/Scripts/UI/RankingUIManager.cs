using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class RankingUIManager : MonoBehaviour
{
    private VisualElement _root;
    private Button _btnHome;
    private VisualElement _listContainer;
    private Label _bestScoreLabel;

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        // UI 요소 찾기
        _btnHome = _root.Q<Button>("BtnHome");
        _listContainer = _root.Q<VisualElement>("RankingListContainer");
        _bestScoreLabel = _root.Q<Label>("BestScoreLabel");

        if (_btnHome != null)
        {
            _btnHome.clicked += OnBackClicked;
        }

        // 랭킹 데이터 가져오기 시작
        LoadRankings();
    }

    private void LoadRankings()
    {
        if (RankingService.Instance == null)
        {
            Debug.LogError("RankingService Instance를 찾을 수 없습니다.");
            return;
        }

        RankingService.Instance.GetRankings(
            onSuccess: (rankings) => {
                UpdateRankingUI(rankings);
            },
            onError: (error) => {
                Debug.LogError("랭킹 로드 실패: " + error);
            }
        );
    }

    private void UpdateRankingUI(List<RankData> rankings)
    {
        if (_listContainer == null) return;

        // 기존 리스트 비우기
        _listContainer.Clear();

        for (int i = 0; i < rankings.Count; i++)
        {
            var data = rankings[i];
            int rankNum = i + 1;

            // 카드 컨테이너 생성 (USS 클래스명: rank-card)
            VisualElement card = new VisualElement();
            card.AddToClassList("rank-card"); 
            
            // 순위별 스타일 적용 (card-gold, card-silver, card-bronze)
            if (rankNum == 1) card.AddToClassList("card-gold");
            else if (rankNum == 2) card.AddToClassList("card-silver");
            else if (rankNum == 3) card.AddToClassList("card-bronze");

            // 1. 메달 또는 순위 번호
            if (rankNum <= 3)
            {
                VisualElement medal = new VisualElement();
                medal.AddToClassList("rank-medal");
                string medalClass = rankNum == 1 ? "gold" : (rankNum == 2 ? "silver" : "bronze");
                medal.AddToClassList(medalClass);
                card.Add(medal);
            }
            else
            {
                Label numLabel = new Label($"#{rankNum}");
                numLabel.AddToClassList("rank-num-simple");
                card.Add(numLabel);
            }

            // 1.5 아바타 (디자인 일관성을 위해 추가)
            VisualElement avatar = new VisualElement();
            avatar.AddToClassList("rank-avatar");
            if (rankNum == 1) avatar.AddToClassList("avatar-rainbow");
            else if (rankNum == 2) avatar.AddToClassList("avatar-yellow");
            else if (rankNum == 3) avatar.AddToClassList("avatar-purple");
            else avatar.AddToClassList("avatar-cyan");
            card.Add(avatar);

            // 2. 플레이어 정보 (이름, 점수, 날짜)
            VisualElement infoStack = new VisualElement();
            infoStack.AddToClassList("rank-info");

            Label nameLabel = new Label(data.name);
            nameLabel.AddToClassList("rank-name-text");
            
            Label scoreLabel = new Label(data.score.ToString("N0"));
            scoreLabel.AddToClassList("rank-score-text");

            Label dateLabel = new Label(data.date);
            dateLabel.AddToClassList("rank-date-text");

            infoStack.Add(nameLabel);
            infoStack.Add(scoreLabel);
            infoStack.Add(dateLabel);
            card.Add(infoStack);

            // 3. 스페이서
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("flex-spacer");
            card.Add(spacer);

            // 리스트에 추가
            _listContainer.Add(card);
        }

        // 베스트 스코어 업데이트
        if (_bestScoreLabel != null)
        {
            int best = PlayerPrefs.GetInt("BestScore", 0);
            _bestScoreLabel.text = best.ToString("N0");
        }
    }

    private void OnBackClicked()
    {
        // 메인 씬으로 돌아가기 (씬 이름이 'MainScene'이라고 가정)
        SceneManager.LoadScene("MainScene");
    }
}
