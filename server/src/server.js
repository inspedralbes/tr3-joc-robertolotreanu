const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const dotenv = require('dotenv');
const cors = require('cors'); // Afegeix això (npm install cors)

dotenv.config();
const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(cors()); // Molt important per a Unity!
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

let activeRooms = []; // Memòria temporal de sales (3.2.3)

// --- RUTAS API (3.2.1) ---
const userRoutes = require('./routes/userRoutes');
app.use('/api/users', userRoutes);


// Crear sala
app.post('/api/rooms/create', (req, res) => {
    const { roomName, hostName, maxPlayers } = req.body;
    const newRoom = {
        id: Date.now().toString(),
        name: roomName,
        host: hostName,
        players: 1,
        playersList: [hostName],
        max: parseInt(maxPlayers) || 4
    };
    activeRooms.push(newRoom);
    console.log("Sala creada:", newRoom);
    res.status(201).send(newRoom);
});

// Unir-se a sala
app.post('/api/rooms/join', (req, res) => {
    const { roomName, playerName } = req.body;
    const room = activeRooms.find(r => r.name === roomName);
    if (room && room.players < room.max) {
        room.players++;
        room.playersList.push(playerName);
        console.log(`${playerName} s'ha unit a ${roomName}`);
        res.status(200).send(room);
    } else {
        res.status(400).send({ message: "Sala plena o no trobada" });
    }
});

//Borrar sales 
app.post('/api/rooms/clear', (req, res) => {
    rooms = []; // Vacía la lista de salas
    console.log("Salas limpiadas por el cliente");
    res.send("OK");
});

// Llistar sales (Per al botó Recarregar de Unity)
app.get('/api/rooms', (req, res) => {
    res.json(activeRooms);
});

// --- WEBSOCKETS (3.2.2) ---
io.on('connection', (socket) => {
    console.log('Jugador connectat al socket:', socket.id);

    socket.on('playerAction', (data) => {
        socket.broadcast.emit('updateGameState', data);
    });

    socket.on('disconnect', () => {
        console.log('Jugador desconnectat');
    });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`🚀 Servidor a http://localhost:${PORT}`);
});