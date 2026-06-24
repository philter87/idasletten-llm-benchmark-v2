'use strict';
const express = require('express');
const path    = require('path');
const { v4: uuid } = require('uuid');

const app = express();
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));
app.use(express.urlencoded({ extended: true }));
app.use(express.json());

// ── In-memory store ────────────────────────────────────────────────────────────
const db = {
  users: [],
  tournaments: [],
  players: [],   // TournamentPlayer
  matches: [],   // TournamentMatch
  teams: [],     // TournamentTeam
  results: [],   // TournamentTeamMatchResult
};

// ── Seed ───────────────────────────────────────────────────────────────────────
(function seed() {
  const u = (username, name) => ({ id: uuid(), username, name, email: `${username.toLowerCase()}@mjolner.dk` });
  db.users = [
    u('PCH', 'Philip Christensen'),
    u('ASL', 'Anders Sletten'),
    u('MKN', 'Mikkel Knudsen'),
    u('JBA', 'Jonas Bak'),
    u('LMO', 'Lars Møller'),
    u('KBR', 'Kasper Bruun'),
    u('THA', 'Thomas Hansen'),
    u('MAD', 'Martin Dahl'),
  ];
  const [pch, asl, mkn, jba, lmo, kbr, tha, mad] = db.users;
  const t1 = uuid(), t2 = uuid(), t3 = uuid();

  db.tournaments.push(
    { id: t1, name: 'Ragnarök Cup 2025',      teamSize: 2, pointsToWin: 5, scoreSystem: 'Elo',       maxPlayerCount: null, isArchived: false, isPublic: true,  parentTournamentId: null, roundNumber: null, createdAt: new Date('2025-01-15') },
    { id: t2, name: 'Valhalla Winter League', teamSize: 1, pointsToWin: 7, scoreSystem: 'WinCount',  maxPlayerCount: 8,    isArchived: false, isPublic: true,  parentTournamentId: null, roundNumber: null, createdAt: new Date('2025-03-01') },
    { id: t3, name: 'Bifrost Championship 2024', teamSize: 2, pointsToWin: 5, scoreSystem: 'TrueSkill', maxPlayerCount: null, isArchived: true, isPublic: false, parentTournamentId: null, roundNumber: null, createdAt: new Date('2024-10-01') },
  );

  // t1 players (Elo)
  [[pch,1387,9,52,31,+18],[asl,1342,8,48,34,+12],[mkn,1298,7,44,36,+8],[jba,1261,6,41,38,-5],[lmo,1219,5,38,40,-7],[kbr,1187,4,35,43,-11],[tha,1151,3,31,46,-14],[mad,1109,2,27,49,-21]]
    .forEach(([user, score, wins, pg, pl, diff]) =>
      db.players.push({ id: uuid(), userId: user.id, tournamentId: t1, score, winCount: wins, matchCount: 12, loseCount: 12 - wins, lives: 3, pointsWon: pg, pointsLost: pl, scoreDiff: diff }));

  // t2 players (WinCount)
  [pch, asl, mkn, jba, lmo, kbr].forEach((user, i) => {
    const wins = 6 - i;
    db.players.push({ id: uuid(), userId: user.id, tournamentId: t2, score: wins, winCount: wins, matchCount: 10, loseCount: 10 - wins, lives: 3, pointsWon: 35 - i * 2, pointsLost: 20 + i * 2, scoreDiff: 0 });
  });

  // t1 matches
  const mkM = (tid, order, state, p1, p2, g1, g2) => {
    const mid = uuid(), tm1 = uuid(), tm2 = uuid();
    db.matches.push({ id: mid, tournamentId: tid, order, state, createdAt: new Date() });
    db.teams.push({ id: tm1, matchId: mid, players: p1, name: 'Team 1', number: 1 }, { id: tm2, matchId: mid, players: p2, name: 'Team 2', number: 2 });
    if (state === 'Done') {
      db.results.push({ id: uuid(), matchId: mid, teamId: tm1, goalsWon: g1, goalsLost: g2 }, { id: uuid(), matchId: mid, teamId: tm2, goalsWon: g2, goalsLost: g1 });
    }
  };
  mkM(t1,  1, 'Done',    ['PCH','ASL'], ['MKN','JBA'], 5, 3);
  mkM(t1,  2, 'Done',    ['LMO','KBR'], ['THA','MAD'], 5, 2);
  mkM(t1,  3, 'Done',    ['PCH','MKN'], ['ASL','LMO'], 5, 4);
  mkM(t1,  4, 'Done',    ['JBA','THA'], ['KBR','MAD'], 5, 1);
  mkM(t1,  5, 'Done',    ['PCH','JBA'], ['LMO','MKN'], 3, 5);
  mkM(t1,  6, 'Done',    ['ASL','KBR'], ['THA','MAD'], 5, 0);
  mkM(t1,  7, 'Done',    ['PCH','THA'], ['JBA','KBR'], 5, 2);
  mkM(t1,  8, 'Planned', ['ASL','THA'], ['PCH','KBR'], null, null);
  mkM(t1,  9, 'Planned', ['MKN','MAD'], ['JBA','LMO'], null, null);
  mkM(t1, 10, 'Planned', ['PCH','ASL'], ['THA','MAD'], null, null);
}());

// ── Helpers ────────────────────────────────────────────────────────────────────
const findUser     = (init) => db.users.find(u => u.username === init.toUpperCase());
const getOrMkUser  = (init, name) => { let u = findUser(init); if (!u) { u = { id: uuid(), username: init.toUpperCase(), name: name || init.toUpperCase(), email: null }; db.users.push(u); } return u; };
const getOrMkPlayer = (userId, tid) => { let p = db.players.find(p => p.userId === userId && p.tournamentId === tid); if (!p) { p = { id: uuid(), userId, tournamentId: tid, score: 1000, winCount: 0, matchCount: 0, loseCount: 0, lives: 3, pointsWon: 0, pointsLost: 0, scoreDiff: 0 }; db.players.push(p); } return p; };
const enrichPlayers = (tid) => db.players.filter(p => p.tournamentId === tid).map(p => ({ ...p, user: db.users.find(u => u.id === p.userId) })).sort((a, b) => b.score - a.score);
const enrichMatches = (tid) => db.matches.filter(m => m.tournamentId === tid).sort((a, b) => a.order - b.order).map(m => ({ ...m, teams: db.teams.filter(t => t.matchId === m.id).map(t => ({ ...t, result: db.results.find(r => r.teamId === t.id) })) }));

// ── Session (prototype-only single-user) ───────────────────────────────────────
let currentUser = null;
app.use((req, res, next) => { res.locals.currentUser = currentUser; next(); });

// ── Routes ─────────────────────────────────────────────────────────────────────

// Home
app.get('/', (req, res) => {
  const tournaments = db.tournaments
    .filter(t => !t.isArchived && t.isPublic && !t.parentTournamentId)
    .map(t => ({ ...t, playerCount: db.players.filter(p => p.tournamentId === t.id).length, matchCount: db.matches.filter(m => m.tournamentId === t.id && m.state === 'Done').length }));
  res.render('home', { tournaments });
});

// All tournaments
app.get('/tournaments', (req, res) => {
  const showAll = req.query.all === '1';
  const list = db.tournaments
    .filter(t => showAll || !t.parentTournamentId)
    .map(t => ({ ...t, playerCount: db.players.filter(p => p.tournamentId === t.id).length, matchCount: db.matches.filter(m => m.tournamentId === t.id && m.state === 'Done').length }));
  res.render('tournaments', { tournaments: list, showAll });
});

// Create tournament
app.get('/tournaments/create', (_req, res) => res.render('create-tournament'));
app.post('/tournaments/create', (req, res) => {
  const { name, teamSize, pointsToWin, scoreSystem, maxPlayerCount, isPublic } = req.body;
  const id = uuid();
  db.tournaments.push({ id, name: name.trim(), teamSize: +teamSize || 2, pointsToWin: +pointsToWin || 5, scoreSystem: scoreSystem || 'Elo', maxPlayerCount: maxPlayerCount ? +maxPlayerCount : null, isArchived: false, isPublic: isPublic === 'on', parentTournamentId: null, roundNumber: null, createdAt: new Date() });
  res.redirect(`/tournaments/${id}`);
});

// Tournament detail
app.get('/tournaments/:id', (req, res) => {
  const t = db.tournaments.find(t => t.id === req.params.id);
  if (!t) return res.status(404).render('404');
  const all = enrichMatches(t.id);
  res.render('tournament', { tournament: t, players: enrichPlayers(t.id), planned: all.filter(m => m.state === 'Planned').slice(0, 5), recent: all.filter(m => m.state === 'Done').reverse().slice(0, 5) });
});

// Add player from tournament detail modal
app.post('/tournaments/:id/add-player', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).send('Not found');
  const user = getOrMkUser((req.body.initials || '').trim(), (req.body.name || '').trim() || undefined);
  getOrMkPlayer(user.id, t.id);
  res.redirect(`/tournaments/${t.id}`);
});

// Players page
app.get('/tournaments/:id/players', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).render('404');
  res.render('players', { tournament: t, players: enrichPlayers(t.id), otherTournaments: db.tournaments.filter(x => x.id !== t.id) });
});
app.post('/tournaments/:id/players', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).send('Not found');
  const user = getOrMkUser((req.body.initials || '').trim(), (req.body.name || '').trim() || undefined);
  getOrMkPlayer(user.id, t.id);
  res.redirect(`/tournaments/${t.id}/players`);
});

// Matches page
app.get('/tournaments/:id/matches', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).render('404');
  const all = enrichMatches(t.id);
  res.render('matches', { tournament: t, planned: all.filter(m => m.state === 'Planned'), done: all.filter(m => m.state === 'Done').reverse(), players: enrichPlayers(t.id) });
});

// Plan a single match
app.post('/tournaments/:id/matches/plan', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).send('Not found');
  const p = (f) => { const v = req.body[f]; return (Array.isArray(v) ? v : [v]).map(s => (s || '').trim().toUpperCase()).filter(Boolean); };
  const maxOrder = Math.max(0, ...db.matches.filter(m => m.tournamentId === t.id).map(m => m.order));
  const mid = uuid(), tm1 = uuid(), tm2 = uuid();
  db.matches.push({ id: mid, tournamentId: t.id, order: maxOrder + 1, state: 'Planned', createdAt: new Date() });
  db.teams.push({ id: tm1, matchId: mid, players: p('team1'), name: 'Team 1', number: 1 }, { id: tm2, matchId: mid, players: p('team2'), name: 'Team 2', number: 2 });
  res.redirect(`/tournaments/${t.id}/matches`);
});

// Create / record match
app.get('/tournaments/:id/create-match', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).render('404');
  let prefill = null;
  if (req.query.matchId) {
    const m = db.matches.find(x => x.id === req.query.matchId);
    if (m) prefill = { match: m, teams: db.teams.filter(x => x.matchId === m.id) };
  }
  res.render('create-match', { tournament: t, players: enrichPlayers(t.id), prefill });
});

app.post('/tournaments/:id/create-match', (req, res) => {
  const t = db.tournaments.find(x => x.id === req.params.id);
  if (!t) return res.status(404).send('Not found');
  const split = (v) => (Array.isArray(v) ? v : (v || '').split(/[\s,]+/)).map(s => s.trim().toUpperCase()).filter(Boolean);
  const t1arr = split(req.body.team1);
  const t2arr = split(req.body.team2);
  const g1 = parseInt(req.body.team1goals) || 0;
  const g2 = parseInt(req.body.team2goals) || 0;

  let match = req.body.matchId ? db.matches.find(m => m.id === req.body.matchId) : null;
  if (match) {
    const oldIds = db.teams.filter(x => x.matchId === match.id).map(x => x.id);
    db.results = db.results.filter(r => !oldIds.includes(r.teamId));
    db.teams   = db.teams.filter(x => x.matchId !== match.id);
    match.state = 'Done';
  } else {
    const maxOrder = Math.max(0, ...db.matches.filter(m => m.tournamentId === t.id).map(m => m.order));
    match = { id: uuid(), tournamentId: t.id, order: maxOrder + 1, state: 'Done', createdAt: new Date() };
    db.matches.push(match);
  }

  const tm1 = uuid(), tm2 = uuid();
  db.teams.push({ id: tm1, matchId: match.id, players: t1arr, name: 'Team 1', number: 1 }, { id: tm2, matchId: match.id, players: t2arr, name: 'Team 2', number: 2 });
  db.results.push({ id: uuid(), matchId: match.id, teamId: tm1, goalsWon: g1, goalsLost: g2 }, { id: uuid(), matchId: match.id, teamId: tm2, goalsWon: g2, goalsLost: g1 });

  const won1 = g1 >= t.pointsToWin;
  [...t1arr.map(i => ({ i, onT1: true })), ...t2arr.map(i => ({ i, onT1: false }))].forEach(({ i, onT1 }) => {
    const user   = getOrMkUser(i);
    const player = getOrMkPlayer(user.id, t.id);
    const won    = onT1 ? won1 : !won1;
    const gf     = onT1 ? g1 : g2;
    const ga     = onT1 ? g2 : g1;
    player.matchCount++; player.pointsWon += gf; player.pointsLost += ga;
    if (won) {
      player.winCount++;
      if (t.scoreSystem === 'Elo')       { player.score += 25;  player.scoreDiff = +25; }
      if (t.scoreSystem === 'TrueSkill') { player.score += 30;  player.scoreDiff = +30; }
      if (t.scoreSystem === 'WinCount')  { player.score++;      player.scoreDiff = +1;  }
    } else {
      player.loseCount++;
      if (t.scoreSystem === 'Elo')       { player.score = Math.max(0, player.score - 20); player.scoreDiff = -20; }
      if (t.scoreSystem === 'TrueSkill') { player.score = Math.max(0, player.score - 25); player.scoreDiff = -25; }
      if (t.scoreSystem === 'Lives')     { player.lives  = Math.max(0, player.lives - 1); }
    }
  });
  res.redirect(`/tournaments/${t.id}`);
});

// User profile
app.get('/users/:id', (req, res) => {
  const user = db.users.find(u => u.id === req.params.id);
  if (!user) return res.status(404).render('404');
  const playerRecords = db.players.filter(p => p.userId === user.id).map(p => ({ ...p, tournament: db.tournaments.find(t => t.id === p.tournamentId) }));
  res.render('user', { user, playerRecords });
});

// Login
app.get('/login', (_req, res) => res.render('login'));
app.post('/login/test', (req, res) => {
  currentUser = getOrMkUser((req.body.initials || '').trim());
  res.redirect('/');
});
app.post('/logout', (_req, res) => { currentUser = null; res.redirect('/'); });

// 404
app.use((_req, res) => res.status(404).render('404'));

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`⚡ Idasletten → http://localhost:${PORT}`));
