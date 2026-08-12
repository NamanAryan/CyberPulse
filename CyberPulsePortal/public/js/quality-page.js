document.addEventListener('DOMContentLoaded', () => {
    // Find the active tab to determine which documents to fetch
    const activeTabElem = document.querySelector('.quality-tab.active');
    let currentTab = activeTabElem ? activeTabElem.dataset.tab : 'QualityManual';
    
    const documentsList = document.getElementById('qualityDocumentsList');
    const refreshBtn = document.getElementById('refreshBtn');

    // The header date and the nav shell are rendered by portal-shell.js.

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function fetchDocuments(callback) {
        if (documentsList) {
            documentsList.innerHTML = '<div style="padding: 20px; color: #777; text-align: center;"><i class="bi bi-arrow-clockwise bi-spin" style="margin-right: 8px;"></i> Loading documents...</div>';
        }

        fetch(`/api/qualityfiles?tab=${encodeURIComponent(currentTab)}`, { cache: 'no-store' })
            .then(res => res.json())
            .then(data => {
                renderDocuments(data);
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching quality files:', err);
                if (documentsList) {
                    documentsList.innerHTML = '<div style="padding: 20px; color: #dc3545; text-align: center;">Error loading documents. Please try again.</div>';
                }
                if (callback) callback();
            });
    }

    function renderDocuments(files) {
        if (!documentsList) return;
        
        if (!files || files.length === 0) {
            documentsList.innerHTML = '<div style="padding: 10px; color: #777; width: 100%;">No documents available in this section.</div>';
            return;
        }

        // Group files by subfolder dynamically
        const groupedFiles = {};
        const prefix = `/Uploads/QualityInside/${currentTab}/`;
        
        files.forEach(item => {
            let relativePath = '';
            let normalizedFilePath = item.FilePath.replace(/\\/g, '/');
            if (normalizedFilePath.startsWith(prefix)) {
                const remainder = normalizedFilePath.substring(prefix.length);
                const slashIndex = remainder.lastIndexOf('/');
                if (slashIndex > -1) {
                    relativePath = remainder.substring(0, slashIndex);
                }
            }
            
            if (!groupedFiles[relativePath]) {
                groupedFiles[relativePath] = [];
            }
            groupedFiles[relativePath].push(item);
        });

        let html = '';
        
        // Sort folder keys to have root ('') first, then custom order, then alphabetical
        const folderOrder = {
            'QualityStandard': 1,
            'Guidelines': 2,
            'Template': 1,
            'Form': 2,
            'Checklist': 3
        };

        const folders = Object.keys(groupedFiles).sort((a, b) => {
            if (a === '') return -1;
            if (b === '') return 1;
            const orderA = folderOrder[a] || 99;
            const orderB = folderOrder[b] || 99;
            if (orderA !== orderB) return orderA - orderB;
            return a.localeCompare(b);
        });

        folders.forEach(folder => {
            html += `<div class="subfolder-section" style="margin-bottom: 25px; background: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); border: 1px solid #e9ecef; width: 100%;">`;
            
            if (folder !== '') {
                let displayFolder = folder.replace(/\//g, ' / ');
                if (displayFolder === 'QualityStandard') {
                    displayFolder = 'Standards';
                }
                html += `
                    <h4 style="margin: 0 0 15px 0; font-size: 1.1rem; color: #0056b3; font-weight: 600; border-bottom: 1px solid #eee; padding-bottom: 10px;">
                        ${escapeHtml(displayFolder)}
                    </h4>
                `;
            }
            
            const folderFiles = groupedFiles[folder];
            // Render vertically using flexbox
            html += `<div style="display: flex; flex-direction: column; gap: 12px; width: 100%;">`;
            
            html += folderFiles.map(item => {
                let iconClass = 'bi-file-earmark-text';
                let colorClass = '';
                
                const ext = item.FileName.split('.').pop().toLowerCase();
                if (ext === 'pdf') { iconClass = 'bi-file-earmark-pdf-fill'; colorClass = 'pdf'; }
                else if (['doc', 'docx'].includes(ext)) { iconClass = 'bi-file-earmark-word-fill'; colorClass = 'word'; }
                else if (['xls', 'xlsx', 'csv'].includes(ext)) { iconClass = 'bi-file-earmark-excel-fill'; colorClass = 'excel'; }
                else if (ext === 'txt') { iconClass = 'bi-file-earmark-text-fill'; colorClass = 'txt'; }

                let displayName = item.FileName;
                const underscoreIndex = displayName.indexOf('_');
                if (underscoreIndex === 36) { // Length of Guid
                    displayName = displayName.substring(underscoreIndex + 1);
                }

                return `
                    <a href="${item.FilePath}" download="${escapeHtml(displayName)}" class="document-item hover-effect" style="border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; background: #f8fafc; text-decoration: none; display: flex; align-items: center; gap: 10px; transition: all 0.2s; width: 100%;">
                        <i class="bi ${iconClass} doc-icon ${colorClass}" style="font-size: 20px;"></i>
                        <span style="font-weight: 500; font-size: 0.95rem; color: #334155; word-break: break-word;">${escapeHtml(displayName)}</span>
                    </a>
                `;
            }).join('');
            
            html += `</div></div>`;
        });

        documentsList.innerHTML = `<div style="display: flex; flex-direction: column; width: 100%;">${html}</div>`;
    }

    // Refresh button
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => {
            const icon = refreshBtn.querySelector('i');
            icon.classList.add('bi-spin');
            
            fetchDocuments(() => {
                setTimeout(() => { icon.classList.remove('bi-spin'); }, 600);
            });
        });
    }

    // Initial Load
    fetchDocuments();
});

// Spin Animation CSS Inject helper for bi-arrow-clockwise (if not already injected by another script)
if (!document.querySelector('style#spin-anim-style')) {
    const style = document.createElement('style');
    style.id = 'spin-anim-style';
    style.textContent = `
        @keyframes spin-anim {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        .bi-spin {
            display: inline-block;
            animation: spin-anim 0.6s linear infinite;
        }
    `;
    document.head.appendChild(style);
}
