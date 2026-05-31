const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const app = express();
const PORT = process.env.PORT || 3000;

// WebGL 빌드 경로 설정
const webglPath = path.join(__dirname, '..', 'WebGLBuild');

// 미들웨어 설정
app.use(cors());
app.use(express.json());

// Unity WebGL 특수 파일 처리를 위한 미들웨어 (정적 파일 서비스 전)
app.use((req, res, next) => {
    if (req.url.endsWith('.unityweb')) {
        res.set('Content-Encoding', 'br');
        if (req.url.endsWith('.wasm.unityweb')) {
            res.set('Content-Type', 'application/wasm');
        } else if (req.url.endsWith('.js.unityweb')) {
            res.set('Content-Type', 'application/javascript');
        } else {
            res.set('Content-Type', 'application/octet-stream');
        }
    }
    next();
});

// 정적 파일 서비스 (WebGL 게임)
app.use(express.static(webglPath));

// 랭킹 데이터 파일 경로
const DATA_FILE = path.join(__dirname, 'rankings.json');

// 데이터 로드 함수
function loadRankings() {
    try {
        if (fs.existsSync(DATA_FILE)) {
            const data = fs.readFileSync(DATA_FILE, 'utf8');
            return JSON.parse(data);
        }
    } catch (err) {
        console.error("Error loading rankings:", err);
    }
    // 파일이 없거나 에러 시 기본값 반환
    return [
        { name: "PRO_GAMER", score: 15420, date: "2026-05-25" },
        { name: "GRAVITY_MASTER", score: 12850, date: "2026-05-25" },
        { name: "FLIP_KING", score: 10230, date: "2026-05-24" }
    ];
}

// 데이터 저장 함수
function saveRankings(data) {
    try {
        fs.writeFileSync(DATA_FILE, JSON.stringify(data, null, 2), 'utf8');
    } catch (err) {
        console.error("Error saving rankings:", err);
    }
}

// 초기 로드
let rankings = loadRankings();

// 1. 랭킹 데이터 가져오기 (GET)
app.get('/ranking', (req, res) => {
    console.log("Ranking data requested.");
    const sortedRankings = rankings.sort((a, b) => b.score - a.score).slice(0, 10);
    res.json(sortedRankings);
});

// 2. 새로운 점수 등록하기 (POST)
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
    saveRankings(rankings); // 파일에 저장
    console.log(`New record saved: ${name} - ${score}`);
    
    res.status(201).json({ message: "Success", data: newEntry });
});

// 랭킹 API와 정적 파일에서 처리되지 않은 모든 요청은 index.html로 보냅니다.
app.use((req, res) => {
    const indexPath = path.join(webglPath, 'index.html');
    if (fs.existsSync(indexPath)) {
        res.sendFile(indexPath);
    } else {
        res.status(404).send("Game build not found. Please check WebGLBuild folder.");
    }
});

app.listen(PORT, () => {
    console.log(`=========================================`);
    console.log(` Balatita Unified Server is now LIVE!`);
    console.log(` URL: http://localhost:${PORT}`);
    console.log(` Serving WebGL from: ${webglPath}`);
    console.log(`=========================================`);
});
