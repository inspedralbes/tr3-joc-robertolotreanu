// src/server.js
const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const dotenv = require('dotenv');

// Carregar variables d'entorn (.env)
dotenv.config();

const app = express();
const server = http.createServer(app);
const io = new Server(server);

const PORT = process.env.PORT || 3000;

// --- MIDDLEWARES ---
app.use(express.json()); // Permet que Unity ens enviï dades en format JSON

// --- RUTES (Endpoints) ---
const userController = require('./controllers/UserController');

app.get('/health', (req, res) => {
    res.send({ status: "Servidor actiu", date: new Date() });
});

// Ruta de registre per a UnityWebRequest
app.post('/api/register', userController.register);

// --- WEBSOCKETS (Comunicació en temps real) ---
io.on('connection', (socket) => {
    console.log('Un jugador s\'ha connectat:', socket.id);

    socket.on('disconnect', () => {
        console.log('Jugador desconnectat');
    });
});

// --- INICIAR SERVIDOR ---
server.listen(PORT, () => {
    console.log(`🚀 Servidor corrent a: http://localhost:${PORT}`);
});