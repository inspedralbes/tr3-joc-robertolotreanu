// src/controllers/UserController.js
const userService = require('../services/UserService');

exports.register = async (req, res) => {
    try {
        const { username, password } = req.body;
        
        // Cridem al servei
        const user = await userService.registerUser(username, password);
        
        console.log("Usuari guardat correctament al Repository:", user.username);
        
        res.status(201).json({ 
            message: "Usuari creat amb èxit!", 
            user: { username: user.username, id: user.id } 
        });
    } catch (error) {
        // Si el Service llança un error (com "usuari ja existeix"), cau aquí
        res.status(400).json({ error: error.message });
    }
};