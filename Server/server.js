const express = require('express');
const cors = require('cors');
const app = express();
const PORT = process.env.PORT || 3000;

// 미들웨어 설정
app.use(cors()); // 유니티(다른 도메인/포트)에서 오는 요청을 허용
app.use(express.json());

// 임시 데이터 저장소 (서버를 끄면 초기화됩니다)
let rankings = [
    { name: "PRO_GAMER", score: 15420, date: "2026-05-25" },
    { name: "GRAVITY_MASTER", score: 12850, date: "2026-05-25" },
    { name: "FLIP_KING", score: 10230, date: "2026-05-24" }
];

// 1. 랭킹 데이터 가져오기 (GET)
// Unity에서 호출 예: http://localhost:3000/ranking
app.get('/ranking', (req, res) => {
    console.log("Ranking data requested.");
    // 점수 높은 순으로 정렬하여 상위 10개 반환
    const sortedRankings = rankings.sort((a, b) => b.score - a.score).slice(0, 10);
    res.json(sortedRankings);
});

// 2. 새로운 점수 등록하기 (POST)
// Unity에서 호출 예: http://localhost:3000/ranking (JSON 데이터 포함)
app.post('/ranking', (req, res) => {
    const { name, score } = req.body;
    
    if (!name || score === undefined) {
        return res.status(400).json({ error: "Name and Score are required." });
    }

    const newEntry = {
        name,
        score: parseInt(score),
        date: new Date().toISOString().split('T')[0]
    };

    rankings.push(newEntry);
    console.log(`New record saved: ${name} - ${score}`);
    
    res.status(201).json({ message: "Success", data: newEntry });
});

// 루트 경로 확인용
app.get('/', (req, res) => {
    res.send("Balatita Ranking Server is running!");
});

app.listen(PORT, () => {
    console.log(`=========================================`);
    console.log(` Balatita Ranking Server is now LIVE!`);
    console.log(` URL: http://localhost:${PORT}`);
    console.log(`=========================================`);
});
