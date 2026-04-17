// src/controllers/UserController.js
const userService = require('../services/UserService');

class UserController {
    async register(req, res) {
        try {
            const { username, password } = req.body;
            const user = await userService.registerUser(username, password);
            console.log("Usuari guardat correctament al Repository:", user.username);
            res.status(201).json({ 
                message: "Usuari creat amb èxit!", 
                user: { username: user.username, id: user.id } 
            });
        } catch (error) {
            res.status(400).json({ error: error.message });
        }
    }

    async login(req, res) {
        try {
            const { username, password } = req.body;
            if (!username || !password) {
                return res.status(400).json({ message: "Cal informar l'usuari i la contrasenya" });
            }
            const user = await userService.loginUser(username, password);
            console.log("Sessió iniciada:", user.username);
            return res.status(200).json({ 
                message: "Sessió iniciada", 
                user: { id: user.id, username: user.username } 
            });
        } catch (error) {
            return res.status(401).json({ error: error.message });
        }
    }

    async getStats(req, res) {
        try {
            // Llegir els paràmetres passats per la URL (/api/users/nom/stats)
            const username = req.params.username;
            const stats = await userService.getUserStats(username);
            
            return res.status(200).json(stats);
        } catch (error) {
            return res.status(404).json({ error: error.message });
        }
    }

    async updateStats(req, res) {
        try {
            const username = req.params.username;
            const { timeSurvived } = req.body;
            
            if (timeSurvived === undefined) {
                return res.status(400).json({ message: "Cal informar el temps sobreviscut (timeSurvived)" });
            }

            const updatedUser = await userService.updateUserStats(username, parseFloat(timeSurvived));
            return res.status(200).json(updatedUser.stats);
        } catch (error) {
            return res.status(400).json({ error: error.message });
        }
    }
}

module.exports = new UserController();