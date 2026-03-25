const UserRepository = require('../repositories/InMemoryUserRepository');

class UserService {
    async register(username, password) {
        const existing = await UserRepository.findByUsername(username);
        if (existing) throw new Error("L'usuari ja existeix");

        const newUser = { id: Date.now(), username, password, score: 0 };
        return await UserRepository.save(newUser);
    }
}

module.exports = new UserService();