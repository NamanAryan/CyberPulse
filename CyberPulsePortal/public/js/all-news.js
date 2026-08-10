document.addEventListener('DOMContentLoaded', () => {
    const ITEMS_PER_PAGE = 10;
    let currentPage = 1;
    let allNews = [];

    const loadingState = document.getElementById('loadingState');
    const emptyState = document.getElementById('emptyState');
    const newsList = document.getElementById('newsList');
    const newsCount = document.getElementById('newsCount');
    const paginationControls = document.getElementById('paginationControls');
    const currentDateDisplay = document.getElementById('currentDate');
    updateHeaderDate();

    fetch('/api/newsarticles')
        .then(res => res.json())
        .then(data => {
            allNews = (data || []).filter(item => item.isActive);
            allNews.sort((a, b) => new Date(b.Date) - new Date(a.Date));
            renderPage();
        })
        .catch(err => {
            console.error('Error fetching news articles:', err);
            showError();
        });

    function renderPage() {
        loadingState.style.display = 'none';

        newsCount.textContent = `${allNews.length} item${allNews.length === 1 ? '' : 's'}`;

        if (allNews.length === 0) {
            emptyState.style.display = 'block';
            paginationControls.style.display = 'none';
            return;
        }

        newsList.style.display = 'grid';

        const totalPages = Math.ceil(allNews.length / ITEMS_PER_PAGE);
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const endIndex = Math.min(startIndex + ITEMS_PER_PAGE, allNews.length);
        const pageItems = allNews.slice(startIndex, endIndex);

        newsList.innerHTML = pageItems.map(item => {
            const formattedDate = formatSlideDate(item.Date);
            const imgSrc = item.ImagePath ? `/Uploads/NewsArticles${escapeHtml(item.ImagePath)}` : '';
            const typeBadge = `<span class="all-news-type">${escapeHtml(item.Type)}</span>`;
            const linkText = item.Description || item.URL;

            return `
                <div class="news-card">
                    <div class="news-card-img">${imgSrc
                        ? `<img src="${imgSrc}" alt="${escapeHtml(item.ImageName || '')}" loading="lazy">`
                        : '<i class="bi bi-newspaper" aria-hidden="true"></i>'}</div>
                    ${typeBadge}
                    <a href="${escapeHtml(item.URL)}" target="_blank" class="news-card-link">
                        ${escapeHtml(linkText)}
                    </a>
                    <span class="all-news-date">${formattedDate}</span>
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
