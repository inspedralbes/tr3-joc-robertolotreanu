const fs = require('fs');
const path = require('path');

class UserRepository {
    constructor() {
        this.dbPath = path.join(__dirname, '..', '..', 'data', 'users.json');
        this.users = [];
        this.initDb();
    }

    initDb() {
        try {
            // Check if data directory exists
            const dirPath = path.dirname(this.dbPath);
            if (!fs.existsSync(dirPath)) {
                fs.mkdirSync(dirPath, { recursive: true });
            }
            if (fs.existsSync(this.dbPath)) {
                const data = fs.readFileSync(this.dbPath, 'utf8');
                this.users = JSON.parse(data);
            } else {
                this.saveDb();
            }
        } catch (error) {
            console.error("No s'ha pogut inicialitzar la BD local", error);
        }
    }

    saveDb() {
        try {
            fs.writeFileSync(this.dbPath, JSON.stringify(this.users, null, 2), 'utf8');
        } catch (error) {
            console.error("Error guardant a disc", error);
        }
    }

    async save(user) {
        this.users.push(user);
        this.saveDb(); // Guardem immediatament al disc
        return user;
    }

    async findByUsername(username) {
        return this.users.find(u => u.username === username);
    }

    async update(user) {
        const index = this.users.findIndex(u => u.username === user.username);
        if (index !== -1) {
            this.users[index] = user;
            this.saveDb();
            return user;
        }
        return null;
    }
}

// Exportem una instància única (Singleton)
module.exports = new UserRepository();