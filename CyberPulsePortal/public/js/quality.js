document.addEventListener('DOMContentLoaded', () => {
    // The header date and the nav/banner shell are rendered by portal-shell.js.
    const refreshBtn = document.getElementById('refreshBtn');

    const section = document.getElementById('qualityAnnouncementSection');
    const track = document.getElementById('qualityAnnouncementTrack');
    const indicators = document.getElementById('qualityAnnouncementIndicators');

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function formatSlideDate(dateStr) {
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

    function fetchQualityAnnouncements(callback) {
        if (!section || !track || !indicators) {
            if (callback) callback();
            return;
        }

        fetch('/api/qualityannouncements')
            .then(res => res.json())
            .then(data => {
                const items = (data || [])
                    .filter(item => item.isActive)
                    .sort((a, b) => new Date(b.Date) - new Date(a.Date));
                renderCarousel(items);
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching quality announcements:', err);
                section.style.display = 'none';
                if (callback) callback();
            });
    }

    function renderCarousel(items) {
        if (track._announcementInterval) clearInterval(track._announcementInterval);
        if (items.length === 0) {
            section.style.display = 'none';
            return;
        }

        section.style.display = 'block';
        track.innerHTML = items.map((item, index) => `
            <div class="carousel-slide" onclick="window.location.href='/quality/details.html?id=${item.ID}&source=quality'" data-index="${index}">
                <h3 class="carousel-slide-title">${escapeHtml(item.Title)}</h3>
                <p class="carousel-slide-desc">${escapeHtml(item.ShortDescription || 'Click to view details...')}</p>
                <span class="carousel-slide-date">${formatSlideDate(item.Date)}</span>
            </div>
        `).join('');
        indicators.innerHTML = items.map((_, index) => `
            <button class="carousel-dot ${index === 0 ? 'active' : ''}" data-target="${index}" aria-label="Go to announcement ${index + 1}"></button>
        `).join('');

        let currentIndex = 0;
        const dots = indicators.querySelectorAll('.carousel-dot');
        const update = () => {
            track.style.transform = `translateX(-${currentIndex * 100}%)`;
            dots.forEach((dot, index) => dot.classList.toggle('active', index === currentIndex));
        };
        const start = () => {
            clearInterval(track._announcementInterval);
            if (items.length > 1) {
                track._announcementInterval = setInterval(() => {
                    currentIndex = (currentIndex + 1) % items.length;
                    update();
                }, 2500);
            }
        };

        dots.forEach((dot, index) => dot.addEventListener('click', () => {
            currentIndex = index;
            update();
            start();
        }));
        track.onmouseenter = () => clearInterval(track._announcementInterval);
        track.onmouseleave = start;
        update();
        start();
    }

    // --- Quality Certificates (listed straight on the card, no click-through) ---
    const certificateList = document.getElementById('qualityCertificateList');

    function fetchQualityCertificates(callback) {
        if (!certificateList) {
            if (callback) callback();
            return;
        }

        fetch('/api/qualityfiles?tab=QualityCertificate', { cache: 'no-store' })
            .then(res => res.json())
            .then(data => {
                renderCertificates(data);
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching quality certificates:', err);
                certificateList.innerHTML = '<div style="padding: 10px; color: #dc3545;">Error loading certificates. Please try again.</div>';
                if (callback) callback();
            });
    }

    function renderCertificates(files) {
        if (!files || files.length === 0) {
            certificateList.innerHTML = '<div style="padding: 10px; color: #777;">No certificates available.</div>';
            return;
        }

        certificateList.innerHTML = files.map(item => {
            // Uploaded files are stored as "<guid>_<original filename>".
            let displayName = item.FileName;
            if (displayName.indexOf('_') === 36) {
                displayName = displayName.substring(37);
            }

            return `
                <a href="${item.FilePath}" download="${escapeHtml(displayName)}" class="document-item">
                    ${escapeHtml(displayName)}
                </a>
            `;
        }).join('');
    }

    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => {
            const icon = refreshBtn.querySelector('i');
            icon.classList.add('bi-spin');
            fetchQualityAnnouncements(() => {
                setTimeout(() => { icon.classList.remove('bi-spin'); }, 600);
            });
            fetchQualityCertificates();
        });
    }

    fetchQualityAnnouncements();
    setInterval(fetchQualityAnnouncements, 30000);
    fetchQualityCertificates();
});

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
