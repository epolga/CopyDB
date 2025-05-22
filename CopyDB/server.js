const express = require("express");
const mysql = require("mysql2");
const cors = require("cors");
require("dotenv").config();

const app = express();
app.use(express.json());
app.use(cors());

const db = mysql.createConnection({
    host: "cross-stitch-db.cmnauage0ofk.us-east-1.rds.amazonaws.com",
    user: "admin",
    password: "cuerYucHartk",
    database: "cross-stitch-db",
    port: 3306
});

db.connect(err => {
    if (err) {
        console.error("Database connection failed:", err);
    } else {
        console.log("Connected to MySQL database");
    }
});

// Get all users
app.get("/users", (req, res) => {
    db.query("SELECT * FROM users", (err, results) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json(results);
        }
    });
});

// Add a new user
app.post("/users", (req, res) => {
    const { name, email } = req.body;
    db.query("INSERT INTO users (name, email) VALUES (?, ?)", [name, email], (err, result) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json({ message: "User added successfully", id: result.insertId });
        }
    });
});

// Get all albums
app.get("/albums", (req, res) => {
    db.query("SELECT * FROM Albums", (err, results) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json(results);
        }
    });
});

// Get designs by AlbumID
app.get("/albums/:albumID/designs", (req, res) => {
    const { albumID } = req.params;
    db.query("SELECT * FROM Designs WHERE AlbumID = ?", [albumID], (err, results) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json(results);
        }
    });
});

// Get design by DesignID
app.get("/designs/:designID", (req, res) => {
    const { designID } = req.params;
    db.query("SELECT * FROM Designs WHERE DesignID = ?", [designID], (err, results) => {
        if (err) {
            res.status(500).json({ error: err.message });
        } else {
            res.json(results.length ? results[0] : { message: "Design not found" });
        }
    });
});

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});
