const UserService = require('../services/UserService');

exports.register = async (req, res) => {
    try {
        const { username, password } = req.body;
        const user = await UserService.register(username, password);
        res.status(201).json({ message: "Usuari creat!", user });
    } catch (error) {
        res.status(400).json({ error: error.message });
    }
};