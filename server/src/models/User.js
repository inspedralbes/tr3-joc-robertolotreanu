class User {
    constructor(id, username, password, score = 0) {
        this.id = id;
        this.username = username;
        this.password = password; // Recorda: en el futur anirà hashejada!
        this.score = score;
    }
}

module.exports = User;