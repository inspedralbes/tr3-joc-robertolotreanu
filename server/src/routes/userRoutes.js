const express = require('express');
const router = express.Router();
const userController = require('../controllers/UserController');

// Ruta per registrar un nou usuari
router.post('/register', userController.register);

// Ruta per iniciar sessió
router.post('/login', userController.login);

// Ruta per consultar les estadístiques
router.get('/:username/stats', userController.getStats);

// Ruta per actualitzar les estadístiques al final de la partida
router.post('/:username/update-stats', userController.updateStats);

module.exports = router;
