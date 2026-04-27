// src/services/UserService.js
const userRepository = require('../repositories/UserRepository');
const bcrypt = require('bcryptjs');

class UserService {
    async registerUser(username, password) {
        // 1. Mirem si ja existeix
        const existing = await userRepository.findByUsername(username);
        if (existing) {
            throw new Error("Aquest usuari ja està registrat");
        }

        // 2. Xifrem la contrasenya
        const salt = await bcrypt.genSalt(10);
        const hashedPassword = await bcrypt.hash(password, salt);

        // 3. Creem l'objecte 
        const newUser = {
            id: Date.now(),
            username,
            password: hashedPassword,
            createdAt: new Date(),
            stats: { gamesPlayed: 0, bestTime: 0 } // Afegim les stats orientades a temps de supervivència
        };

        // 4. Cridem al repository per guardar
        return await userRepository.save(newUser);
    }

    async getUserStats(username) {
        // Obtenim de BD l'usuari i si existeix, retornem només les stats
        const user = await userRepository.findByUsername(username);
        if (!user) {
            throw new Error("Usuari no trobat");
        }
        return user.stats;
    }

    async loginUser(username, password) {
        // 1. Busquem l'usuari
        const user = await userRepository.findByUsername(username);
        if (!user) {
            throw new Error("Usuari no trobat");
        }

        // 2. Comprovem contrasenya
        const isMatch = await bcrypt.compare(password, user.password);
        if (!isMatch) {
            throw new Error("Contrasenya incorrecta");
        }

        return user;
    }

    async updateUserStats(username, timeSurvived) {
        const user = await userRepository.findByUsername(username);
        if (!user) {
            throw new Error("Usuari no trobat");
        }
        
        user.stats.gamesPlayed += 1;
        if (timeSurvived > user.stats.bestTime) {
            user.stats.bestTime = timeSurvived;
        }

        return await userRepository.update(user);
    }
}

module.exports = new UserService();