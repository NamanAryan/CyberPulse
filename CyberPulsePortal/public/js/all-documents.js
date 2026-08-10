document.addEventListener('DOMContentLoaded', () => {
    const ITEMS_PER_PAGE = 12;
    let currentPage = 1;
    let currentTab = 'docs';
    let allFiles = [];

    // DOM references
    const loadingState = document.getElementById('loadingState');
    const documentCount = document.getElementById('documentCount');
    const paginationControls = document.getElementById('paginationControls');
    const currentDateDisplay = document.getElementById('currentDate');
    const tabBar = document.getElementById('tabBar');

    const paneDocs = document.getElementById('pane-docs');
    const paneMedia = document.getElementById('pane-media');

    const countDocs = document.getElementById('countDocs');
    const countMedia = document.getElementById('countMedia');

    // Media viewer
    const mediaViewer = document.getElementById('mediaViewer');
    const viewerImg = document.getElementById('viewerImg');
    const viewerVid = document.getElementById('viewerVid');
    const viewerTitle = document.getElementById('viewerTitle');
    const viewerClose = document.getElementById('viewerClose');

    updateHeaderDate();
    setInterval(updateHeaderDate, 60000);

    // --- Tab switching ---
    tabBar.addEventListener('click', (e) => {
        const btn = e.target.closest('.tab-btn');
        if (!btn || btn.classList.contains('active')) return;
        tabBar.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        currentTab = btn.dataset.tab;
        currentPage = 1;
        renderCurrentTab();
    });

    // --- Fetch data ---
    fetch('/api/hrfiles')
        .then(res => res.json())
        .then(data => {
            allFiles = data;
            // Sort newest first
            allFiles.sort((a, b) => {
                return parseDate(b.UploadDate) - parseDate(a.UploadDate);
            });
            loadingState.style.display = 'none';
            updateCounts();
            renderCurrentTab();
        })
        .catch(err => {
            console.error('Error fetching files:', err);
            showError();
        });

    // --- Media Viewer ---
    viewerClose.addEventListener('click', closeViewer);
    mediaViewer.addEventListener('click', (e) => {
        if (e.target === mediaViewer) closeViewer();
    });
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeViewer();
    });

    function openViewer(filePath, fileType, fileName) {
        viewerTitle.textContent = fileName;
        if (fileType === 'photo') {
            viewerVid.style.display = 'none';
            viewerVid.pause();
            viewerVid.src = '';
            viewerImg.src = filePath;
            viewerImg.style.display = 'block';
        } else if (fileType === 'video') {
            viewerImg.style.display = 'none';
            viewerImg.src = '';
            viewerVid.src = filePath;
            viewerVid.style.display = 'block';
            viewerVid.play().catch(() => {});
        }
        mediaViewer.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeViewer() {
        mediaViewer.classList.remove('open');
        viewerVid.pause();
        viewerVid.src = '';
        viewerImg.src = '';
        document.body.style.overflow = '';
    }

    // Make openViewer accessible from onclick
    window._openViewer = openViewer;

    // --- Rendering ---
    function updateCounts() {
        const docs = allFiles.filter(f => f.FileType === 'pdf' || f.FileType === 'word' || f.FileType === 'txt');
        const media = allFiles.filter(f => f.FileType === 'photo' || f.FileType === 'video');
        countDocs.textContent = docs.length;
        countMedia.textContent = media.length;
        documentCount.textContent = `${allFiles.length} file${allFiles.length === 1 ? '' : 's'}`;
    }

    function getFilteredFiles() {
        if (currentTab === 'docs') return allFiles.filter(f => f.FileType === 'pdf' || f.FileType === 'word' || f.FileType === 'txt');
        if (currentTab === 'media') return allFiles.filter(f => f.FileType === 'photo' || f.FileType === 'video');
        return allFiles;
    }

    function renderCurrentTab() {
        // Hide all panes
        [paneDocs, paneMedia].forEach(p => p.classList.remove('active'));

        const activePane = currentTab === 'docs' ? paneDocs : paneMedia;
        activePane.classList.add('active');

        const filtered = getFilteredFiles();
        const totalPages = Math.ceil(filtered.length / ITEMS_PER_PAGE);
        if (currentPage > totalPages) currentPage = 1;
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const pageItems = filtered.slice(startIndex, startIndex + ITEMS_PER_PAGE);

        if (filtered.length === 0) {
            activePane.innerHTML = renderEmpty(
                currentTab === 'docs' ? 'No documents uploaded yet.' : 'No media files uploaded yet.'
            );
            paginationControls.style.display = 'none';
            return;
        }

        // Render based on tab type
        if (currentTab === 'media') {
            activePane.innerHTML = renderMediaGrid(pageItems);
        } else {
            activePane.innerHTML = renderDocList(pageItems);
        }

        renderPagination(totalPages);
    }

    function renderDocList(items) {
        return '<div class="doc-list">' + items.map(item => {
            let iconClass = 'bi-file-earmark-text';
            let iconColor = '#6b7280';
            if (item.FileType === 'pdf') { iconClass = 'bi-file-pdf'; iconColor = '#dc2626'; }
            else if (item.FileType === 'word') { iconClass = 'bi-file-word'; iconColor = '#2563eb'; }
            return `
                <div class="doc-card">
                    <div class="doc-icon" style="color: ${iconColor};">
                        <i class="bi ${iconClass}"></i>
                    </div>
                    <div class="doc-info">
                        <a class="doc-name" href="${escapeAttr(item.FilePath)}" download="${escapeAttr(item.FileName)}" title="${escapeAttr(item.FileName)}">
                            ${escapeHtml(item.FileName)}
                        </a>
                        <div class="doc-meta">
                            <span><i class="bi bi-hdd me-1"></i>${formatSize(item.FileSize)}</span>
                            <span><i class="bi bi-calendar3 me-1"></i>${formatDate(item.UploadDate)}</span>
                        </div>
                    </div>
                </div>
            `;
        }).join('') + '</div>';
    }

    function renderMediaGrid(items) {
        return '<div class="media-grid">' + items.map(item => {
            const isVideo = item.FileType === 'video';
            const thumbContent = isVideo
                ? `<video src="${escapeAttr(item.FilePath)}#t=0.1" preload="metadata"></video>
                   <i class="bi bi-play-circle-fill play-badge"></i>`
                : `<img src="${escapeAttr(item.FilePath)}" alt="${escapeAttr(item.FileName)}" loading="lazy">`;

            return `
                <div class="media-card" onclick="window._openViewer('${escapeAttr(item.FilePath)}', '${item.FileType}', '${escapeJs(item.FileName)}')">
                    <div class="media-thumb">${thumbContent}</div>
                    <div class="media-card-body">
                        <div class="media-name" title="${escapeAttr(item.FileName)}">${escapeHtml(item.FileName)}</div>
                        <div class="media-meta">
                            <span>${formatSize(item.FileSize)}</span>
                            <span>${formatDate(item.UploadDate)}</span>
                        </div>
                    </div>
                </div>
            `;
        }).join('') + '</div>';
    }

    function renderEmpty(msg) {
        return `
            <div class="empty-panel">
                <i class="bi bi-folder2-open"></i>
                <h3>Nothing Here</h3>
                <p>${msg}</p>
            </div>
        `;
    }

    // --- Pagination ---
    function renderPagination(totalPages) {
        if (totalPages <= 1) {
            paginationControls.style.display = 'none';
            return;
        }
        paginationControls.style.display = 'flex';
        let html = '';

        html += `<button class="page-btn ${currentPage === 1 ? 'disabled' : ''}" ${currentPage === 1 ? 'disabled' : ''} data-page="${currentPage - 1}">
            <i class="bi bi-chevron-left"></i> Prev
        </button>`;

        html += '<div class="page-numbers">';
        for (let i = 1; i <= totalPages; i++) {
            if (totalPages <= 7 || i === 1 || i === totalPages || Math.abs(i - currentPage) <= 1) {
                html += `<button class="page-num ${i === currentPage ? 'active' : ''}" data-page="${i}">${i}</button>`;
            } else if (Math.abs(i - currentPage) === 2) {
                html += `<span class="page-ellipsis">…</span>`;
            }
        }
        html += '</div>';

        html += `<button class="page-btn ${currentPage === totalPages ? 'disabled' : ''}" ${currentPage === totalPages ? 'disabled' : ''} data-page="${currentPage + 1}">
            Next <i class="bi bi-chevron-right"></i>
        </button>`;

        paginationControls.innerHTML = html;

        paginationControls.querySelectorAll('[data-page]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const page = parseInt(e.currentTarget.dataset.page, 10);
                if (page >= 1 && page <= totalPages && page !== currentPage) {
                    currentPage = page;
                    renderCurrentTab();
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        });
    }

    // --- Utilities ---
    function showError() {
        loadingState.style.display = 'none';
        paneDocs.classList.add('active');
        paneDocs.innerHTML = `
            <div class="empty-panel">
                <i class="bi bi-exclamation-triangle"></i>
                <h3>Error Loading Files</h3>
                <p>Could not connect to the HR Portal. Please try again later.</p>
            </div>
        `;
    }

    function updateHeaderDate() {
        const options = { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' };
        currentDateDisplay.textContent = new Date().toLocaleDateString('en-US', options);
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.innerText = str;
        return div.innerHTML;
    }

    function escapeAttr(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function escapeJs(str) {
        if (!str) return '';
        return str.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/"/g, '\\"');
    }

    function formatSize(bytes) {
        if (!bytes) return '0 B';
        const suffixes = ['B', 'KB', 'MB', 'GB'];
        let counter = 0;
        let number = bytes;
        while (Math.round(number / 1024) >= 1) {
            number /= 1024;
            counter++;
        }
        return `${number.toFixed(1)} ${suffixes[counter]}`;
    }

    function parseDate(dateStr) {
        if (typeof dateStr === 'string' && dateStr.startsWith('/Date(')) {
            return parseInt(dateStr.replace(/\/Date\(|\)\//g, ''), 10);
        }
        return new Date(dateStr).getTime();
    }

    function formatDate(dateStr) {
        if (!dateStr) return '';
        let d;
        if (typeof dateStr === 'string' && dateStr.startsWith('/Date(')) {
            d = new Date(parseInt(dateStr.replace(/\/Date\(|\)\//g, ''), 10));
        } else {
            d = new Date(dateStr);
        }
        if (isNaN(d.getTime())) return '';
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return `${months[d.getMonth()]} ${String(d.getDate()).padStart(2, '0')}, ${d.getFullYear()}`;
    }
});
