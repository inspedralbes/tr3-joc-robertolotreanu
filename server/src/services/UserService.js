// src/services/UserService.js
const userRepository = require('../repositories/UserRepository');

class UserService {
    async registerUser(username, password) {
        // 1. Mirem si ja existeix
        const existing = await userRepository.findByUsername(username);
        if (existing) {
            throw new Error("Aquest usuari ja està registrat");
        }

        // 2. Creem l'objecte (aquí podríem posar ID, data, etc.)
        const newUser = {
            id: Date.now(),
            username,
            password, // Més endavant la hashejarem per seguretat
            createdAt: new Date()
        };

        // 3. Cridem al repository per guardar
        return await userRepository.save(newUser);
    }
}

module.exports = new UserService();