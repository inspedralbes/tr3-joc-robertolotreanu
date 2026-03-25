class UserRepository {
    constructor() {
        this.users = []; 
    }

    async save(user) {
        this.users.push(user);
        return user;
    }

    async findByUsername(username) {
        return this.users.find(u => u.username === username);
    }
}

// Exportem una instància única (Singleton)
module.exports = new UserRepository();