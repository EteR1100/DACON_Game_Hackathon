const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const { Pool } = require('pg');

const app = express();
const PORT = process.env.PORT || 3000;
const webglPath = path.join(__dirname, '..', 'WebGLBuild');

const pool = new Pool({
    connectionString: process.env.DATABASE_URL,
    ssl: process.env.DATABASE_URL ? { rejectUnauthorized: false } : false
});

app.use(cors());
app.use(express.json());

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

app.use(express.static(webglPath));

function getKstDateString(date = new Date()) {
    return new Date(date.getTime() + 9 * 60 * 60 * 1000).toISOString().split('T')[0];
}

async function initializeDatabase() {
    if (!process.env.DATABASE_URL) {
        throw new Error('DATABASE_URL environment variable is not set.');
    }

    await pool.query(`
        CREATE TABLE IF NOT EXISTS rankings (
            id SERIAL PRIMARY KEY,
            name TEXT UNIQUE NOT NULL,
            score INTEGER NOT NULL DEFAULT 0,
            date TEXT NOT NULL
        )
    `);
}

app.get('/ranking', async (req, res) => {
    const period = req.query.period || 'all';
    const now = new Date();

    try {
        let result;
        if (period === 'daily') {
            const todayStr = getKstDateString(now);
            result = await pool.query(
                'SELECT name, score, date FROM rankings WHERE date = $1 ORDER BY score DESC LIMIT 10',
                [todayStr]
            );
        } else if (period === 'weekly') {
            const oneWeekAgo = getKstDateString(new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000));
            result = await pool.query(
                'SELECT name, score, date FROM rankings WHERE date >= $1 ORDER BY score DESC LIMIT 10',
                [oneWeekAgo]
            );
        } else {
            result = await pool.query(
                'SELECT name, score, date FROM rankings ORDER BY score DESC LIMIT 10'
            );
        }

        res.json(result.rows);
    } catch (err) {
        console.error('Ranking query failed:', err);
        res.status(500).json({ error: 'Ranking query failed.' });
    }
});

app.post('/ranking', async (req, res) => {
    const { name, score } = req.body;

    if (!name || score === undefined) {
        return res.status(400).json({ error: 'Name and score are required.' });
    }

    const parsedScore = parseInt(score, 10);
    if (Number.isNaN(parsedScore)) {
        return res.status(400).json({ error: 'Score must be a number.' });
    }

    const nickname = String(name).trim();
    if (!nickname) {
        return res.status(400).json({ error: 'Name cannot be empty.' });
    }

    const playDate = getKstDateString();

    try {
        const result = await pool.query(
            `INSERT INTO rankings (name, score, date)
             VALUES ($1, $2, $3)
             ON CONFLICT (name)
             DO UPDATE SET
                score = GREATEST(rankings.score, EXCLUDED.score),
                date = CASE
                    WHEN EXCLUDED.score >= rankings.score THEN EXCLUDED.date
                    ELSE rankings.date
                END
             RETURNING name, score, date`,
            [nickname, parsedScore, playDate]
        );

        console.log(`Record saved: ${nickname} - ${parsedScore}`);
        res.status(201).json({ message: 'Success', data: result.rows[0] });
    } catch (err) {
        console.error('Ranking save failed:', err);
        res.status(500).json({ error: 'Ranking save failed.' });
    }
});

app.use((req, res) => {
    const indexPath = path.join(webglPath, 'index.html');
    if (fs.existsSync(indexPath)) {
        res.sendFile(indexPath);
    } else {
        res.status(404).send('Game build not found. Please check WebGLBuild folder.');
    }
});

initializeDatabase()
    .then(() => {
        app.listen(PORT, () => {
            console.log('=========================================');
            console.log(' Balatita Unified Server is now LIVE!');
            console.log(` URL: http://localhost:${PORT}`);
            console.log(` Serving WebGL from: ${webglPath}`);
            console.log(' Ranking DB connected.');
            console.log('=========================================');
        });
    })
    .catch(err => {
        console.error('Failed to initialize database:', err);
        process.exit(1);
    });
