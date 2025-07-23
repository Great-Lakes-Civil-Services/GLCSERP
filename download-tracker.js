// GLERP Download Tracker API
// This handles user registration and download tracking

const express = require('express');
const cors = require('cors');
const fs = require('fs').promises;
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static('public'));

// Data storage (in production, use a proper database)
const DATA_FILE = 'downloads.json';
const REGISTRATIONS_FILE = 'registrations.json';

// Initialize data files if they don't exist
async function initializeDataFiles() {
    try {
        await fs.access(DATA_FILE);
    } catch {
        await fs.writeFile(DATA_FILE, JSON.stringify([]));
    }
    
    try {
        await fs.access(REGISTRATIONS_FILE);
    } catch {
        await fs.writeFile(REGISTRATIONS_FILE, JSON.stringify([]));
    }
}

// Load data from file
async function loadData(filename) {
    try {
        const data = await fs.readFile(filename, 'utf8');
        return JSON.parse(data);
    } catch (error) {
        console.error(`Error loading ${filename}:`, error);
        return [];
    }
}

// Save data to file
async function saveData(filename, data) {
    try {
        await fs.writeFile(filename, JSON.stringify(data, null, 2));
        return true;
    } catch (error) {
        console.error(`Error saving ${filename}:`, error);
        return false;
    }
}

// API Routes

// Track download
app.post('/api/track-download', async (req, res) => {
    try {
        const downloadData = {
            ...req.body,
            timestamp: new Date().toISOString(),
            ip: req.ip,
            userAgent: req.get('User-Agent')
        };

        const downloads = await loadData(DATA_FILE);
        downloads.push(downloadData);
        await saveData(DATA_FILE, downloads);

        console.log('Download tracked:', downloadData);
        res.json({ success: true, message: 'Download tracked successfully' });
    } catch (error) {
        console.error('Error tracking download:', error);
        res.status(500).json({ success: false, message: 'Failed to track download' });
    }
});

// Register user
app.post('/api/register-user', async (req, res) => {
    try {
        const userData = {
            ...req.body,
            registrationDate: new Date().toISOString(),
            ip: req.ip,
            userAgent: req.get('User-Agent')
        };

        const registrations = await loadData(REGISTRATIONS_FILE);
        registrations.push(userData);
        await saveData(REGISTRATIONS_FILE, registrations);

        console.log('User registered:', userData);
        res.json({ success: true, message: 'User registered successfully' });
    } catch (error) {
        console.error('Error registering user:', error);
        res.status(500).json({ success: false, message: 'Failed to register user' });
    }
});

// Get download statistics
app.get('/api/statistics', async (req, res) => {
    try {
        const downloads = await loadData(DATA_FILE);
        const registrations = await loadData(REGISTRATIONS_FILE);

        const stats = {
            totalDownloads: downloads.length,
            totalRegistrations: registrations.length,
            downloadsToday: downloads.filter(d => {
                const today = new Date().toDateString();
                return new Date(d.timestamp).toDateString() === today;
            }).length,
            registrationsToday: registrations.filter(r => {
                const today = new Date().toDateString();
                return new Date(r.registrationDate).toDateString() === today;
            }).length,
            departments: registrations.reduce((acc, reg) => {
                acc[reg.department] = (acc[reg.department] || 0) + 1;
                return acc;
            }, {})
        };

        res.json(stats);
    } catch (error) {
        console.error('Error getting statistics:', error);
        res.status(500).json({ success: false, message: 'Failed to get statistics' });
    }
});

// Get all registrations (admin only)
app.get('/api/registrations', async (req, res) => {
    try {
        const registrations = await loadData(REGISTRATIONS_FILE);
        res.json(registrations);
    } catch (error) {
        console.error('Error getting registrations:', error);
        res.status(500).json({ success: false, message: 'Failed to get registrations' });
    }
});

// Get all downloads (admin only)
app.get('/api/downloads', async (req, res) => {
    try {
        const downloads = await loadData(DATA_FILE);
        res.json(downloads);
    } catch (error) {
        console.error('Error getting downloads:', error);
        res.status(500).json({ success: false, message: 'Failed to get downloads' });
    }
});

// Serve the download page
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'GLERP_Download_Page.html'));
});

// Health check
app.get('/health', (req, res) => {
    res.json({ status: 'OK', timestamp: new Date().toISOString() });
});

// Start server
async function startServer() {
    await initializeDataFiles();
    
    app.listen(PORT, () => {
        console.log(`🚀 GLERP Download Tracker running on port ${PORT}`);
        console.log(`📊 Statistics available at: http://localhost:${PORT}/api/statistics`);
        console.log(`👥 Registrations available at: http://localhost:${PORT}/api/registrations`);
        console.log(`📥 Downloads available at: http://localhost:${PORT}/api/downloads`);
    });
}

startServer().catch(console.error);

module.exports = app; 