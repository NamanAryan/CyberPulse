document.addEventListener('DOMContentLoaded', () => {
    const ITEMS_PER_PAGE = 10;
    let currentPage = 1;
    let allAnnouncements = [];

    const loadingState = document.getElementById('loadingState');
    const emptyState = document.getElementById('emptyState');
    const announcementsList = document.getElementById('announcementsList');
    const announcementCount = document.getElementById('announcementCount');
    const paginationControls = document.getElementById('paginationControls');
    const currentDateDisplay = document.getElementById('currentDate');
    const announcementHeading = document.getElementById('announcementHeading');
    const source = new URLSearchParams(window.location.search).get('source');
    // `tabUrl`/`tabLabel` point back at the tab that owns each feed.
    const sourceConfig = {
        quality: { endpoint: '/api/qualityannouncements', label: 'Quality Announcements', tabUrl: '/quality.html', tabLabel: 'Quality Insights' },
        hr: { endpoint: '/api/hrannouncements', label: 'HR Announcements', tabUrl: '/hr-connect.html', tabLabel: 'HR Connect' }
    }[source] || { endpoint: '/api/announcements', label: 'Announcements', source: 'all', tabUrl: '/index.html', tabLabel: 'Home' };
    updateHeaderDate();

    document.title = `${sourceConfig.label} - CyberPulse Portal`;
    if (announcementHeading) announcementHeading.textContent = sourceConfig.label;

    const backBtn = document.querySelector('.btn-back');
    if (backBtn) {
        backBtn.href = sourceConfig.tabUrl;
        backBtn.innerHTML = `<i class="bi bi-arrow-left"></i> Back to ${sourceConfig.tabLabel}`;
    }

    fetch(sourceConfig.endpoint)
        .then(res => res.json())
        .then(data => {
            allAnnouncements = (data || []).filter(item => item.isActive);
            renderPage();
        })
        .catch(err => {
            console.error('Error fetching announcements:', err);
            showError();
        });

    function renderPage() {
        loadingState.style.display = 'none';

        announcementCount.textContent = `${allAnnouncements.length} item${allAnnouncements.length === 1 ? '' : 's'}`;

        if (allAnnouncements.length === 0) {
            emptyState.style.display = 'block';
            paginationControls.style.display = 'none';
            return;
        }

        announcementsList.style.display = 'flex';

        const totalPages = Math.ceil(allAnnouncements.length / ITEMS_PER_PAGE);
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const endIndex = Math.min(startIndex + ITEMS_PER_PAGE, allAnnouncements.length);
        const pageItems = allAnnouncements.slice(startIndex, endIndex);

        announcementsList.innerHTML = pageItems.map(item => {
            const formattedDate = formatSlideDate(item.Date);

            return `
                <div class="announcement-list-item" onclick="window.location.href='details.html?id=${item.ID}&source=${source ? `${source}-all` : 'all'}'">
                    <h3 class="list-item-title">${escapeHtml(item.Title)}</h3>
                    <p class="list-item-desc">${escapeHtml(item.ShortDescription || 'Click to view details...')}</p>
                    <span class="list-item-date">${formattedDate}</span>
                </div>
            `;
        }).join('');

        // Render pagination
        if (totalPages > 1) {
            paginationControls.style.display = 'flex';
            let paginationHtml = '';

            // Previous button
            paginationHtml += `<button class="page-btn ${currentPage === 1 ? 'disabled' : ''}" ${currentPage === 1 ? 'disabled' : ''} data-page="${currentPage - 1}">
                <i class="bi bi-chevron-left"></i> Prev
            </button>`;

            // Page numbers
            paginationHtml += '<div class="page-numbers">';
            for (let i = 1; i <= totalPages; i++) {
                if (totalPages <= 7 || i === 1 || i === totalPages || Math.abs(i - currentPage) <= 1) {
                    paginationHtml += `<button class="page-num ${i === currentPage ? 'active' : ''}" data-page="${i}">${i}</button>`;
                } else if (Math.abs(i - currentPage) === 2) {
                    paginationHtml += `<span class="page-ellipsis">…</span>`;
                }
            }
            paginationHtml += '</div>';

            // Next button
            paginationHtml += `<button class="page-btn ${currentPage === totalPages ? 'disabled' : ''}" ${currentPage === totalPages ? 'disabled' : ''} data-page="${currentPage + 1}">
                Next <i class="bi bi-chevron-right"></i>
            </button>`;

            paginationControls.innerHTML = paginationHtml;

            // Attach click handlers
            paginationControls.querySelectorAll('[data-page]').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const page = parseInt(e.currentTarget.dataset.page, 10);
                    if (page >= 1 && page <= totalPages && page !== currentPage) {
                        currentPage = page;
                        renderPage();
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    }
                });
            });
        } else {
            paginationControls.style.display = 'none';
        }
    }

    function showError() {
        loadingState.style.display = 'none';
        emptyState.style.display = 'block';
        emptyState.querySelector('h3').textContent = 'Error Loading Feed';
        emptyState.querySelector('p').textContent = 'Please try again later.';
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

    // Helper to format slide date exactly like the mockup
    function formatSlideDate(dateStr) {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        
        const dayName = days[d.getDay()];
        const monthName = months[d.getMonth()];
        const dayVal = String(d.getDate()).padStart(2, '0');
        const yearVal = d.getFullYear();
        
        return `${dayName} ${monthName} ${dayVal} ${yearVal}`;
    }
});
