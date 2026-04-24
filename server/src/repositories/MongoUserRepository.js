// src/repositories/MongoUserRepository.js
const User = require('../models/User');

class MongoUserRepository {
    async findByUsername(username) {
        return await User.findOne({ username });
    }

    async save(userData) {
        const user = new User(userData);
        return await user.save();
    }

    async update(userData) {
        return await User.findOneAndUpdate(
            { username: userData.username },
            userData,
            { returnDocument: 'after' }
        );
    }
}

module.exports = new MongoUserRepository();
