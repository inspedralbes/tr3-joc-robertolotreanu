class InMemoryUserRepository {
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

    async getAll() {
        return this.users;
    }
}

module.exports = new InMemoryUserRepository();