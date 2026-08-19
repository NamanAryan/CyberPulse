const express = require('express');
const fs = require('fs');
const path = require('path');

// Bypass self-signed certificate validation errors for local server-to-server calls
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const app = express();
const PORT = process.env.PORT || 8001;

app.use(express.static(path.join(__dirname, 'public')));
// Lets shared pages (e.g. all.html, details.html) also resolve under /quality/*
// for links reached from the Quality section, without duplicating any files.
app.use('/quality', express.static(path.join(__dirname, 'public')));

// CyberPulsePortal and CyberPulseAdministration are sibling folders under CyberPulse.
const ADMIN_DIR = path.join(__dirname, '..', 'CyberPulseAdministration');
const UPLOADS_DIR = path.join(ADMIN_DIR, 'Uploads');
const JSON_OUTPUT_DIR = path.join(ADMIN_DIR, 'JsonOutput');

app.use('/Uploads', express.static(UPLOADS_DIR));

const HR_UPLOADS_DIR = path.join(UPLOADS_DIR, 'HrPortal');

// Every quality sub-tab folder lives under this single parent folder.
const QUALITY_UPLOADS_DIR = path.join(UPLOADS_DIR, 'QualityInside', 'QualityDocuments');
const QUALITY_UPLOADS_URL = '/Uploads/QualityInside/QualityDocuments';

function getLocalHrFiles() {
    const fileTypes = ['pdf', 'word', 'excel', 'txt', 'photo', 'video'];
    const files = [];

    for (const fileType of fileTypes) {
        const directory = path.join(HR_UPLOADS_DIR, fileType);
        if (!fs.existsSync(directory)) continue;

        for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
            if (!entry.isFile()) continue;

            const filePath = path.join(directory, entry.name);
            const stats = fs.statSync(filePath);
            // Uploaded files are stored as "<guid>_<original filename>".
            const fileName = entry.name.replace(/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}_/i, '');

            files.push({
                FileName: fileName,
                FilePath: `/Uploads/HrPortal/${fileType}/${encodeURIComponent(entry.name)}`,
                FileType: fileType,
                FileSize: stats.size,
                UploadDate: stats.birthtime
            });
        }
    }

    return files.sort((a, b) => new Date(b.UploadDate) - new Date(a.UploadDate));
}

app.get('/api/announcements', (req, res) => {
    const jsonPath = path.join(JSON_OUTPUT_DIR, 'announcements.json');
    
    if (!fs.existsSync(jsonPath)) {
        return res.json([]);
    }

    fs.readFile(jsonPath, 'utf8', (err, data) => {
        if (err) {
            console.error('Error reading announcements JSON file:', err);
            return res.status(500).json({ error: 'Failed to read announcements data.' });
        }

        try {
            const announcements = JSON.parse(data || '[]');
            res.json(announcements);
        } catch (parseErr) {
            console.error('Error parsing announcements JSON data:', parseErr);
            res.status(500).json({ error: 'Invalid announcements JSON data format.' });
        }
    });
});

function serveAnnouncementFeed(fileName, res) {
    const jsonPath = path.join(JSON_OUTPUT_DIR, fileName);

    if (!fs.existsSync(jsonPath)) {
        return res.json([]);
    }

    fs.readFile(jsonPath, 'utf8', (err, data) => {
        if (err) {
            console.error(`Error reading ${fileName}:`, err);
            return res.status(500).json({ error: 'Failed to read announcements data.' });
        }

        try {
            res.json(JSON.parse(data || '[]'));
        } catch (parseErr) {
            console.error(`Error parsing ${fileName}:`, parseErr);
            res.status(500).json({ error: 'Invalid announcements JSON data format.' });
        }
    });
}

app.get('/api/qualityannouncements', (req, res) => {
    serveAnnouncementFeed('qualityannouncements.json', res);
});

app.get('/api/hrannouncements', (req, res) => {
    serveAnnouncementFeed('hrannouncements.json', res);
});

app.get('/api/hrfiles', (req, res) => {
    try {
        // The standalone portal serves the same Uploads directory as the MVC
        // application. Listing it here avoids relying on an MVC endpoint that
        // may return a login/error HTML page instead of JSON.
        res.json(getLocalHrFiles());
    } catch (err) {
        console.error('Error reading local HR uploads:', err);
        res.status(500).json({ error: 'Failed to read HR files from local storage.' });
    }
});

app.get('/api/healthtip', (req, res) => {
    const healthTipDir = path.join(UPLOADS_DIR, 'HealthTip');

    try {
        if (!fs.existsSync(healthTipDir)) {
            return res.json({ exists: false, filePath: null });
        }

        const files = fs.readdirSync(healthTipDir, { withFileTypes: true })
            .filter(entry => entry.isFile())
            .map(entry => ({ name: entry.name, modified: fs.statSync(path.join(healthTipDir, entry.name)).mtime }))
            .sort((a, b) => b.modified - a.modified);

        if (files.length === 0) {
            return res.json({ exists: false, filePath: null });
        }

        const fileName = files[0].name;
        const extension = path.extname(fileName).toLowerCase();
        const isImage = ['.jpg', '.jpeg', '.png', '.gif', '.webp'].includes(extension);
        const result = {
            exists: true,
            filePath: `/Uploads/HealthTip/${encodeURIComponent(fileName)}`,
            isImage
        };

        if (extension === '.txt') {
            result.textContent = fs.readFileSync(path.join(healthTipDir, fileName), 'utf8');
        }

        return res.json(result);
    } catch (err) {
        console.error('Error reading Health Tip:', err);
        return res.status(500).json({ exists: false, error: 'Failed to retrieve Health Tip.' });
    }
});

app.get('/api/newsarticles', (req, res) => {
    const jsonPath = path.join(JSON_OUTPUT_DIR, 'newsarticles.json');
    
    if (!fs.existsSync(jsonPath)) {
        return res.json([]);
    }

    fs.readFile(jsonPath, 'utf8', (err, data) => {
        if (err) {
            console.error('Error reading news articles JSON file:', err);
            return res.status(500).json({ error: 'Failed to read news articles data.' });
        }

        try {
            const articles = JSON.parse(data || '[]');
            res.json(articles);
        } catch (parseErr) {
            console.error('Error parsing news articles JSON data:', parseErr);
            res.status(500).json({ error: 'Invalid news articles JSON data format.' });
        }
    });
});

app.get('/api/qualityfiles', (req, res) => {
    const tab = req.query.tab;
    if (!tab) {
        return res.status(400).json({ error: 'Tab parameter is required' });
    }

    // tab can be a nested path like "QualityStandardGuidelines/QualityStandard"
    // or a parent path like "QualityStandardGuidelines". Resolve it and confirm
    // it stays inside the quality folder, so a "../" segment cannot walk out.
    const qualityRoot = path.resolve(QUALITY_UPLOADS_DIR);
    const dirPath = path.resolve(qualityRoot, ...tab.split('/'));

    if (dirPath !== qualityRoot && !dirPath.startsWith(qualityRoot + path.sep)) {
        return res.status(400).json({ error: 'Invalid tab parameter' });
    }
    
    if (!fs.existsSync(dirPath)) {
        return res.json([]);
    }

    const fileList = [];
    
    // Recursive function to gather all files in the directory and its subdirectories
    function walkDir(currentPath, currentUrlBase) {
        if (!fs.existsSync(currentPath)) return;
        
        let files;
        try {
            files = fs.readdirSync(currentPath);
        } catch (err) {
            console.error('Error reading directory:', err);
            return;
        }

        for (const file of files) {
            const filePath = path.join(currentPath, file);
            try {
                const stats = fs.statSync(filePath);
                if (stats.isFile()) {
                    fileList.push({
                        FileName: file,
                        FilePath: `${currentUrlBase}/${file}`,
                        UploadDate: stats.birthtime,
                        FileSize: stats.size
                    });
                } else if (stats.isDirectory()) {
                    // Recursively process subdirectories
                    walkDir(filePath, `${currentUrlBase}/${file}`);
                }
            } catch (statErr) {
                console.error(`Error statting file ${file}:`, statErr);
            }
        }
    }

    try {
        walkDir(dirPath, `${QUALITY_UPLOADS_URL}/${tab}`);
        // Sort by UploadDate descending
        fileList.sort((a, b) => new Date(b.UploadDate) - new Date(a.UploadDate));
        res.json(fileList);
    } catch (err) {
        console.error('Error in /api/qualityfiles:', err);
        return res.status(500).json({ error: 'Failed to read directory' });
    }
});

app.listen(PORT, () => {
    console.log(`CyberPulse Portal server running at http://localhost:${PORT}`);
});
