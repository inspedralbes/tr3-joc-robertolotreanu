const express    = require('express');
const http       = require('http');
const { Server } = require('socket.io');
const WebSocket  = require('ws');
const dotenv     = require('dotenv');
const cors       = require('cors');
const mongoose   = require('mongoose');

dotenv.config();
const app    = express();
const server = http.createServer(app);

// ── Connexió a Base de Dades (MongoDB) ─────────────────────────────────────
if (process.env.MONGO_URI) {
    mongoose.connect(process.env.MONGO_URI)
        .then(() => console.log('🍃 Connectat a MongoDB'))
        .catch(err => console.error('❌ Error connectant a MongoDB:', err));
}

// ── Socket.io (per a altres integracions futures) ─────────────────────────
const io = new Server(server, { path: '/socket.io' });
io.on('connection', socket => {
    console.log('[socket.io] Client connectat:', socket.id);
    socket.on('playerAction', data => socket.broadcast.emit('updateGameState', data));
    socket.on('disconnect',  () => console.log('[socket.io] Client desconnectat'));
});

// ── WebSocket natiu (per a events de partida des de Unity) ─────────────────
//    Unity es connecta a  ws://localhost:3000/gs
const wss = new WebSocket.Server({ noServer: true });

server.on('upgrade', (request, socket, head) => {
    if (request.url === '/gs') {
        wss.handleUpgrade(request, socket, head, ws => wss.emit('connection', ws, request));
    }
    // socket.io gestiona les seves propies actualitzacions automaticament
});

wss.on('connection', ws => {
    console.log('[WS] Client Unity connectat per events de partida');

    ws.on('message', raw => {
        try {
            const evt = JSON.parse(raw.toString());
            console.log(`[WS] Esdeveniment: ${evt.type}`, evt);
            // Reenviar a tots els clients WebSocket de la mateixa sala
            wss.clients.forEach(client => {
                if (client !== ws && client.readyState === WebSocket.OPEN)
                    client.send(raw.toString());
            });
        } catch (e) {
            console.warn('[WS] Missatge no parsejable:', raw.toString());
        }
    });

    ws.on('close', () => console.log('[WS] Client Unity desconnectat'));
    ws.on('error', err => console.error('[WS] Error:', err.message));
});

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

let activeRooms = [];

// ── Rutas API ─────────────────────────────────────────────────────────────
const userRoutes = require('./routes/userRoutes');
app.use('/api/users', userRoutes);

app.post('/api/rooms/create', (req, res) => {
    const { roomName, hostName, maxPlayers, port } = req.body;
    let ip = req.headers['x-forwarded-for'] || req.socket.remoteAddress;
    if (ip && ip.includes(',')) ip = ip.split(',')[0].trim();
    if (ip && ip.startsWith('::ffff:')) ip = ip.substring(7);

    const newRoom = {
        id: Date.now().toString(),
        name: roomName, host: hostName,
        players: 1, playersList: [hostName],
        max: parseInt(maxPlayers) || 4,
        port: parseInt(port) || 7777,
        ip: ip
    };
    activeRooms.push(newRoom);
    console.log(`Sala creada: ${newRoom.name} a la IP ${newRoom.ip}:${newRoom.port}`);
    res.status(201).send(newRoom);
});

app.post('/api/rooms/join', (req, res) => {
    const { roomName, playerName } = req.body;
    const room = activeRooms.find(r => r.name === roomName);
    if (room && room.players < room.max) {
        room.players++;
        room.playersList.push(playerName);
        console.log(`${playerName} s'ha unit a ${roomName}`);
        res.status(200).send(room);
    } else {
        res.status(400).send({ message: 'Sala plena o no trobada' });
    }
});

app.post('/api/rooms/clear', (req, res) => {
    activeRooms = [];
    res.send('OK');
});

app.delete('/api/rooms/delete/:hostName', (req, res) => {
    activeRooms = activeRooms.filter(r => r.host !== req.params.hostName);
    console.log(`Sala del host ${req.params.hostName} eliminada`);
    res.status(200).send('OK');
});

app.get('/api/rooms', (req, res) => res.json(activeRooms));

const PORT = process.env.PORT || 3000;
server.listen(PORT, () =>
    console.log(`🚀 Servidor a http://localhost:${PORT}  |  WS natius: ws://localhost:${PORT}/gs`)
);