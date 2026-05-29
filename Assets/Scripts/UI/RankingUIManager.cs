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

    void OnEnable()
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

            // 카드 컨테이너 생성
            VisualElement card = new VisualElement();
            card.AddToClassList("rank-item-box"); 
            
            // 순위별 스타일 적용
            if (rankNum == 1) card.AddToClassList("item-gold");
            else if (rankNum == 2) card.AddToClassList("item-silver");
            else if (rankNum == 3) card.AddToClassList("item-bronze");

            // 1. 메달 또는 순위 번호
            if (rankNum <= 3)
            {
                VisualElement medal = new VisualElement();
                string medalClass = rankNum == 1 ? "medal-gold" : (rankNum == 2 ? "medal-silver" : "medal-bronze");
                medal.AddToClassList(medalClass);
                card.Add(medal);
            }
            else
            {
                Label numLabel = new Label($"#{rankNum}");
                numLabel.AddToClassList("rank-number-text");
                card.Add(numLabel);
            }

            // 2. 플레이어 정보 (이름, 점수, 날짜)
            VisualElement infoStack = new VisualElement();
            infoStack.AddToClassList("player-info-stack");

            Label nameLabel = new Label(data.name);
            nameLabel.AddToClassList("nickname-text");
            
            Label scoreLabel = new Label(data.score.ToString("N0"));
            scoreLabel.AddToClassList("score-text");

            Label dateLabel = new Label(data.date);
            dateLabel.AddToClassList("date-text");

            infoStack.Add(nameLabel);
            infoStack.Add(scoreLabel);
            infoStack.Add(dateLabel);
            card.Add(infoStack);

            // 3. 스페이서
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("spacer");
            card.Add(spacer);

            // 리스트에 추가
            _listContainer.Add(card);
        }
    }

    private void OnBackClicked()
    {
        // 메인 씬으로 돌아가기 (씬 이름이 'MainScene'이라고 가정)
        SceneManager.LoadScene("MainScene");
    }
}
