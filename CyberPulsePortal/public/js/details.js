document.addEventListener('DOMContentLoaded', () => {
    // DOM Elements
    const detailsWrapper = document.getElementById('detailsWrapper');
    const loadingState = document.getElementById('loadingState');
    const errorState = document.getElementById('errorState');
    const currentDateDisplay = document.getElementById('currentDate');

    const detailsTitle = document.getElementById('detailsTitle');
    const detailsDate = document.getElementById('detailsDate');
    const detailsShortDesc = document.getElementById('detailsShortDesc');
    const descriptionIframe = document.getElementById('descriptionIframe');

    // Update Date Display
    updateHeaderDate();

    // Get ID and source from query parameters
    const urlParams = new URLSearchParams(window.location.search);
    const idParam = urlParams.get('id');
    const sourceParam = urlParams.get('source');

    if (!idParam) {
        showError();
        return;
    }

    const id = parseInt(idParam, 10);
    const isAllView = sourceParam === 'all' || (sourceParam && sourceParam.endsWith('-all'));
    const announcementSource = sourceParam ? sourceParam.replace('-all', '') : 'feed';
    const sourceConfig = {
        quality: { endpoint: '/api/qualityannouncements', listUrl: 'all.html?source=quality', listLabel: 'Quality Announcements' },
        hr: { endpoint: '/api/hrannouncements', listUrl: 'all.html?source=hr', listLabel: 'HR Announcements' }
    }[announcementSource] || { endpoint: '/api/announcements', listUrl: 'all.html', listLabel: 'All Announcements' };

    const backBtn = detailsWrapper.querySelector('.btn-back');
    if (backBtn) {
        if (isAllView) {
            backBtn.href = sourceConfig.listUrl;
            backBtn.innerHTML = `<i class="bi bi-arrow-left"></i> Back to ${sourceConfig.listLabel}`;
        } else {
            backBtn.href = 'index.html';
            backBtn.innerHTML = '<i class="bi bi-arrow-left"></i> Back to Feed';
        }
    }

    // Fetch and display
    fetch(sourceConfig.endpoint)
        .then(res => res.json())
        .then(data => {
            const announcement = data.find(a => a.ID === id);
            
            if (announcement) {
                renderDetails(announcement);
            } else {
                showError();
            }
        })
        .catch(err => {
            console.error('Error fetching announcement details:', err);
            showError();
        });

    function renderDetails(item) {
        loadingState.style.display = 'none';
        detailsWrapper.style.display = 'block';

        detailsDate.textContent = formatSlideDate(item.Date);
        
        detailsTitle.textContent = item.Title;
        
        if (item.ShortDescription) {
            detailsShortDesc.textContent = item.ShortDescription;
            detailsShortDesc.style.display = 'block';
        } else {
            detailsShortDesc.style.display = 'none';
        }

        // Isolated style reset and injection for description
        const docReset = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
                <style>
                    * { box-sizing: border-box; }
                    body {
                        font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        color: #374151;
                        font-size: 15.5px;
                        line-height: 1.7;
                        margin: 0;
                        padding: 10px;
                        background-color: transparent;
                        -webkit-font-smoothing: antialiased;
                    }
                    a {
                        color: #003057;
                        text-decoration: none;
                        font-weight: 500;
                    }
                    a:hover {
                        text-decoration: underline;
                    }
                    p { margin-top: 0; margin-bottom: 18px; }
                    h1, h2, h3, h4, h5, h6 {
                        color: #374151;
                        font-family: 'Outfit', 'Inter', sans-serif;
                        margin-top: 0;
                        margin-bottom: 14px;
                        line-height: 1.3;
                    }
                    h1 { font-size: 24px; font-weight: 700; border-bottom: 1px solid #e5e7eb; padding-bottom: 8px; }
                    h2 { font-size: 20px; font-weight: 600; }
                    h3 { font-size: 18px; font-weight: 600; }
                    ul, ol { margin-top: 0; margin-bottom: 18px; padding-left: 24px; }
                    li { margin-bottom: 8px; }
                    blockquote {
                        margin: 0 0 18px 0;
                        padding-left: 16px;
                        border-left: 4px solid #d1d5db;
                        color: #6b7280;
                        font-style: italic;
                    }
                    img {
                        max-width: 100%;
                        height: auto;
                        border-radius: 8px;
                        margin-bottom: 16px;
                    }
                    table {
                        width: 100%;
                        border-collapse: collapse;
                        margin-bottom: 18px;
                        font-size: 14px;
                    }
                    th, td {
                        padding: 10px 14px;
                        border: 1px solid #e5e7eb;
                        text-align: left;
                    }
                    th {
                        background-color: #fafbfc;
                        font-weight: 600;
                    }
                </style>
            </head>
            <body>
                ${item.Description || '<p class="text-muted" style="color:#9ca3af; font-style:italic;">No announcement body details available.</p>'}
            </body>
            </html>
        `;
        
        descriptionIframe.srcdoc = docReset;

        // Auto resize iframe
        descriptionIframe.onload = () => {
            setTimeout(() => {
                try {
                    const iframeDoc = descriptionIframe.contentDocument || descriptionIframe.contentWindow.document;
                    if (iframeDoc && iframeDoc.body) {
                        const contentHeight = Math.max(
                            iframeDoc.body.scrollHeight,
                            iframeDoc.documentElement.scrollHeight
                        );
                        descriptionIframe.style.height = `${contentHeight + 40}px`;
                    }
                } catch (e) {
                    // Fallback
                    descriptionIframe.style.height = '600px';
                }
            }, 100);
        };
    }

    function showError() {
        loadingState.style.display = 'none';
        detailsWrapper.style.display = 'none';
        errorState.style.display = 'block';
    }

    function updateHeaderDate() {
        const options = { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' };
        currentDateDisplay.textContent = new Date().toLocaleDateString('en-US', options);
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
