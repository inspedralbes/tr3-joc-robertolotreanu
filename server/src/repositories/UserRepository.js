// src/repositories/UserRepository.js
const dotenv = require('dotenv');
dotenv.config();

// Selector de implementación del Repositorio (Estrategia de Persistencia)
// Si existe MONGO_URI en el .env, usamos MongoDB. Si no, usamos el JSON local.

let repository;

if (process.env.MONGO_URI) {
    console.log("📂 [Repository] Usant implementació de MONGODB (Producció)");
    repository = require('./MongoUserRepository');
} else {
    console.log("📂 [Repository] Usant implementació JSON LOCAL (Desenvolupament)");
    repository = require('./JsonUserRepository');
}

module.exports = repository;
