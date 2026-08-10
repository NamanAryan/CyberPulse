// CyberPulse Client Portal - Frontend Controller

document.addEventListener('DOMContentLoaded', () => {
    // State Variables
    let announcements = [];
    let newsArticles = [];
    let currentHealthTip = null;
    let mediaFiles = [];
    let docFiles = [];
    let currentMediaTab = 'photos';
    let currentMediaIndex = 0;
    let currentQualityTab = 'QualityManual';
    
    let currentSlide = 0;
    let carouselInterval = null;

    // DOM Elements
    const currentDateDisplay = document.getElementById('currentDate');
    const refreshBtn = document.getElementById('refreshBtn');

    // Announcements
    const carouselSection = document.getElementById('carouselSection');
    const carouselTrack = document.getElementById('carouselTrack');
    const carouselIndicators = document.getElementById('carouselIndicators');
    const emptyState = document.getElementById('emptyState');
    const hrAnnouncementSection = document.getElementById('hrAnnouncementSection');
    const hrAnnouncementTrack = document.getElementById('hrAnnouncementTrack');
    const hrAnnouncementIndicators = document.getElementById('hrAnnouncementIndicators');

    // Documents
    const documentsList = document.getElementById('documentsList');

    // News & Articles
    const newsSection = document.getElementById('newsSection');
    const newsArticlesList = document.getElementById('newsArticlesList');

    // Media Gallery
    const mediaTabs = document.querySelectorAll('.media-tab');
    const qualityDocumentsList = document.getElementById('qualityDocumentsList');
    const mediaGalleryTrack = document.getElementById('mediaGalleryTrack');
    const mediaPrevBtn = document.getElementById('mediaPrevBtn');
    const mediaNextBtn = document.getElementById('mediaNextBtn');
    
    // Media viewer
    const mediaViewer = document.getElementById('mediaViewer');
    const viewerImg = document.getElementById('viewerImg');
    const viewerVid = document.getElementById('viewerVid');
    const viewerTitle = document.getElementById('viewerTitle');
    const viewerClose = document.getElementById('viewerClose');

    // Update Date Display
    updateHeaderDate();
    setInterval(updateHeaderDate, 60000);

    // Initial Load
    fetchAnnouncements();
    fetchCategoryAnnouncements('/api/hrannouncements', hrAnnouncementSection, hrAnnouncementTrack, hrAnnouncementIndicators, 'hr');
    fetchMediaFiles();
    fetchNewsArticles();
    fetchHealthTip();
    fetchQualityDocuments();

    // Set up Auto-Refresh every 30 seconds
    setInterval(fetchAnnouncements, 30000);
    setInterval(() => fetchCategoryAnnouncements('/api/hrannouncements', hrAnnouncementSection, hrAnnouncementTrack, hrAnnouncementIndicators, 'hr'), 30000);
    setInterval(fetchMediaFiles, 30000);
    setInterval(fetchNewsArticles, 30000);
    setInterval(fetchHealthTip, 30000);
    setInterval(fetchQualityDocuments, 30000);

    // Event Listeners
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => {
            const icon = refreshBtn.querySelector('i');
            icon.classList.add('bi-spin');

            Promise.all([
                new Promise(resolve => fetchAnnouncements(resolve)),
                new Promise(resolve => fetchCategoryAnnouncements('/api/hrannouncements', hrAnnouncementSection, hrAnnouncementTrack, hrAnnouncementIndicators, 'hr', resolve)),
                new Promise(resolve => fetchMediaFiles(resolve)),
                new Promise(resolve => fetchNewsArticles(resolve)),
                new Promise(resolve => fetchHealthTip(resolve)),
                new Promise(resolve => fetchQualityDocuments(resolve))
            ]).then(() => {
                setTimeout(() => { icon.classList.remove('bi-spin'); }, 600);
            });
        });
    }

    mediaTabs.forEach(tab => {
        tab.addEventListener('click', (e) => {
            mediaTabs.forEach(t => t.classList.remove('active'));
            e.target.classList.add('active');
            currentMediaTab = e.target.dataset.tab;
            currentMediaIndex = 0;
            renderMediaGallery();
        });
    });

    if (mediaPrevBtn) {
        mediaPrevBtn.addEventListener('click', () => {
            if (currentMediaIndex > 0) {
                currentMediaIndex--;
                updateMediaGalleryTransform();
            }
        });
    }

    if (mediaNextBtn) {
        mediaNextBtn.addEventListener('click', () => {
            const items = mediaGalleryTrack.querySelectorAll('.media-item');
            // If we have more than 4 items, we can slide
            if (currentMediaIndex < items.length - 4) {
                currentMediaIndex++;
                updateMediaGalleryTransform();
            }
        });
    }

    // Helper functions
    function updateHeaderDate() {
        if (!currentDateDisplay) return;
        const options = { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' };
        currentDateDisplay.textContent = new Date().toLocaleDateString('en-US', options);
    }

    function fetchAnnouncements(callback) {
        fetch('/api/announcements')
            .then(res => res.json())
            .then(data => {
                announcements = data.filter(item => item.isActive);
                announcements.sort((a, b) => new Date(b.Date) - new Date(a.Date));
                filterAndRenderAnnouncements();
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching announcements:', err);
                if (callback) callback();
            });
    }

    function filterAndRenderAnnouncements() {
        const activeAnnouncements = announcements;
        
        if (activeAnnouncements.length > 0) {
            if (carouselSection) carouselSection.style.display = 'block';
            if (emptyState) emptyState.style.display = 'none';
            
            // Build slides
            if (carouselTrack) {
                carouselTrack.innerHTML = activeAnnouncements.map((item, index) => `
                    <div class="carousel-slide" onclick="window.location.href='details.html?id=${item.ID}&source=feed'" data-index="${index}">
                        <h3 class="carousel-slide-title">${escapeHtml(item.Title)}</h3>
                        <p class="carousel-slide-desc">${escapeHtml(item.ShortDescription || 'Click to view details...')}</p>
                        <span class="carousel-slide-date">${formatSlideDate(item.Date)}</span>
                    </div>
                `).join('');
            }
            
            // Build dots
            if (carouselIndicators) {
                carouselIndicators.innerHTML = activeAnnouncements.map((_, index) => `
                    <button class="carousel-dot ${index === 0 ? 'active' : ''}" data-target="${index}" aria-label="Go to slide ${index + 1}"></button>
                `).join('');
            }
            
            initCarousel();
            
        } else {
            if (carouselSection) carouselSection.style.display = 'none';
            if (emptyState) emptyState.style.display = 'block';
        }
    }

    let _onMouseEnter = null;
    let _onMouseLeave = null;

    function initCarousel() {
        if (!carouselIndicators || !carouselTrack) return;
        
        // Clear any existing interval
        if (carouselInterval) clearInterval(carouselInterval);
        currentSlide = 0;
        updateCarouselTransform();
        
        const dots = carouselIndicators.querySelectorAll('.carousel-dot');
        const totalSlides = dots.length;
        if (totalSlides === 0) return;
        
        // Setup dot clicks
        dots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                currentSlide = index;
                updateCarouselTransform();
                clearInterval(carouselInterval);
                startAutoSlide();
            });
        });
        
        function startAutoSlide() {
            carouselInterval = setInterval(() => {
                currentSlide = (currentSlide + 1) % totalSlides;
                updateCarouselTransform();
            }, 2500);
        }
        
        startAutoSlide();
        
        // Remove old hover listeners
        if (_onMouseEnter) carouselTrack.removeEventListener('mouseenter', _onMouseEnter);
        if (_onMouseLeave) carouselTrack.removeEventListener('mouseleave', _onMouseLeave);

        _onMouseEnter = () => clearInterval(carouselInterval);
        _onMouseLeave = () => startAutoSlide();

        carouselTrack.addEventListener('mouseenter', _onMouseEnter);
        carouselTrack.addEventListener('mouseleave', _onMouseLeave);
    }

    function updateCarouselTransform() {
        if (!carouselTrack || !carouselIndicators) return;
        
        carouselTrack.style.transform = `translateX(-${currentSlide * 100}%)`;
        
        // Update dots
        const dots = carouselIndicators.querySelectorAll('.carousel-dot');
        dots.forEach((dot, i) => {
            if (i === currentSlide) {
                dot.classList.add('active');
            } else {
                dot.classList.remove('active');
            }
        });
    }

    function fetchCategoryAnnouncements(endpoint, section, track, indicators, source, callback) {
        if (!section || !track || !indicators) {
            if (callback) callback();
            return;
        }

        fetch(endpoint)
            .then(res => res.json())
            .then(data => {
                const items = (data || [])
                    .filter(item => item.isActive)
                    .sort((a, b) => new Date(b.Date) - new Date(a.Date));
                renderCategoryCarousel(items, section, track, indicators, source);
                if (callback) callback();
            })
            .catch(err => {
                console.error(`Error fetching ${source} announcements:`, err);
                section.style.display = 'none';
                if (callback) callback();
            });
    }

    function renderCategoryCarousel(items, section, track, indicators, source) {
        if (track._announcementInterval) clearInterval(track._announcementInterval);
        if (items.length === 0) {
            section.style.display = 'none';
            return;
        }

        section.style.display = 'block';
        track.innerHTML = items.map((item, index) => `
            <div class="carousel-slide" onclick="window.location.href='details.html?id=${item.ID}&source=${source}'" data-index="${index}">
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

    function fetchMediaFiles(callback) {
        fetch('/api/hrfiles')
            .then(res => res.json())
            .then(data => {
                data = data || [];
                
                mediaFiles = data.filter(item => item.FileType === 'photo' || item.FileType === 'video');
                docFiles = data.filter(item => item.FileType !== 'photo' && item.FileType !== 'video');
                
                if (mediaGalleryTrack) renderMediaGallery();
                if (documentsList) renderDocuments();
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching hr files:', err);
                if (callback) callback();
            });
    }

    function renderMediaGallery() {
        if (!mediaGalleryTrack) return;
        
        const filteredMedia = mediaFiles.filter(item => {
            if (currentMediaTab === 'photos') return item.FileType === 'photo';
            if (currentMediaTab === 'videos') return item.FileType === 'video';
            return true;
        });

        if (filteredMedia.length === 0) {
            mediaGalleryTrack.innerHTML = '<div style="padding: 20px; color: #777; width: 100%; text-align: center;">No media available.</div>';
            if (mediaPrevBtn) mediaPrevBtn.style.opacity = '0.5';
            if (mediaNextBtn) mediaNextBtn.style.opacity = '0.5';
            return;
        }

        mediaGalleryTrack.innerHTML = filteredMedia.map((item, index) => {
            let thumbContent = '';
            if (item.FileType === 'video') {
                thumbContent = `<video src="${item.FilePath}#t=0.1" preload="metadata"></video>
                                <i class="bi bi-play-circle-fill" style="position:absolute; top:50%; left:50%; transform:translate(-50%,-50%); font-size:30px; color:rgba(255,255,255,0.9);"></i>`;
            } else {
                thumbContent = `<img src="${item.FilePath}" alt="${escapeHtml(item.FileName)}" loading="lazy">`;
            }
            
            return `
                <div class="media-item">
                    <div class="media-item-content" onclick="window._openViewer('${item.FilePath.replace(/'/g, "\\'")}', '${item.FileType}', '${item.FileName.replace(/'/g, "\\'")}')">
                        ${thumbContent}
                    </div>
                </div>
            `;
        }).join('');
        
        currentMediaIndex = 0;
        updateMediaGalleryTransform();
    }

    function updateMediaGalleryTransform() {
        if (!mediaGalleryTrack) return;
        
        // 4 items visible, each is 25%. So shift by -25% * currentMediaIndex.
        mediaGalleryTrack.style.transform = `translateX(-${currentMediaIndex * 25}%)`;
        
        const items = mediaGalleryTrack.querySelectorAll('.media-item');
        if (mediaPrevBtn) mediaPrevBtn.style.opacity = currentMediaIndex === 0 ? '0.5' : '1';
        if (mediaNextBtn) mediaNextBtn.style.opacity = (items.length <= 4 || currentMediaIndex >= items.length - 4) ? '0.5' : '1';
    }

    function renderDocuments() {
        if (!documentsList) return;
        
        if (docFiles.length === 0) {
            documentsList.innerHTML = '<div style="padding: 10px; color: #777;">No documents available.</div>';
            return;
        }

        documentsList.innerHTML = docFiles.map(item => {
            let iconClass = 'bi-file-earmark-text';
            let colorClass = '';
            if (item.FileType === 'pdf') { iconClass = 'bi-file-earmark-pdf-fill'; colorClass = 'pdf'; }
            else if (item.FileType === 'word') { iconClass = 'bi-file-earmark-word-fill'; colorClass = 'word'; }
            else if (item.FileType === 'excel') { iconClass = 'bi-file-earmark-excel-fill'; colorClass = 'excel'; }
            else if (item.FileType === 'txt') { iconClass = 'bi-file-earmark-text-fill'; colorClass = 'txt'; }

            return `
                <a href="${item.FilePath}" download="${escapeHtml(item.FileName)}" class="document-item">
                    <i class="bi ${iconClass} doc-icon ${colorClass}"></i>
                    <span>${escapeHtml(item.FileName)}</span>
                </a>
            `;
        }).join('');
    }

    // --- Media Viewer Overlay ---
    if (viewerClose && mediaViewer) {
        viewerClose.addEventListener('click', closeViewer);
        mediaViewer.addEventListener('click', (e) => {
            if (e.target === mediaViewer) closeViewer();
        });
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') closeViewer();
        });
    }

    function openViewer(filePath, fileType, fileName) {
        if (!mediaViewer) return;
        
        if (viewerTitle) viewerTitle.textContent = fileName;
        if (fileType === 'photo') {
            if (viewerVid) {
                viewerVid.style.display = 'none';
                viewerVid.pause();
                viewerVid.src = '';
            }
            if (viewerImg) {
                viewerImg.src = filePath;
                viewerImg.style.display = 'block';
            }
        } else if (fileType === 'video') {
            if (viewerImg) {
                viewerImg.style.display = 'none';
                viewerImg.src = '';
            }
            if (viewerVid) {
                viewerVid.src = filePath;
                viewerVid.style.display = 'block';
                viewerVid.play().catch(() => {});
            }
        }
        mediaViewer.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeViewer() {
        if (!mediaViewer) return;
        
        mediaViewer.classList.remove('open');
        if (viewerVid) {
            viewerVid.pause();
            viewerVid.src = '';
        }
        if (viewerImg) {
            viewerImg.src = '';
        }
        document.body.style.overflow = '';
    }

    window._openViewer = openViewer;

    // === News & Articles ===
    function fetchNewsArticles(callback) {
        fetch('/api/newsarticles')
            .then(res => res.json())
            .then(data => {
                newsArticles = (data || []).filter(item => item.isActive);
                newsArticles.sort((a, b) => new Date(b.Date) - new Date(a.Date));
                renderNewsArticles();
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching news articles:', err);
                if (callback) callback();
            });
    }

    function renderNewsArticles() {
        if (!newsArticlesList || !newsSection) return;

        if (newsArticles.length === 0) {
            newsSection.style.display = 'none';
            return;
        }

        newsSection.style.display = 'block';

        newsArticlesList.innerHTML = newsArticles.map(item => {
            const imgSrc = item.ImagePath ? `/Uploads/NewsArticles${escapeHtml(item.ImagePath)}` : '';
            const typeBadge = `<span style="color: #c02b80; font-size: 14px; font-family: 'Outfit', sans-serif;">${escapeHtml(item.Type)}</span>`;
            const linkText = item.Description || item.URL;
            const formattedDate = formatSlideDate(item.Date);

            return `
                <div class="news-card">
                    ${imgSrc ? `<div class="news-card-img"><img src="${imgSrc}" alt="${escapeHtml(item.ImageName || '')}" loading="lazy"></div>` : ''}
                    <div class="news-card-body">
                        <div class="news-card-meta">
                            ${typeBadge}
                        </div>
                        <a href="${escapeHtml(item.URL)}" target="_blank" class="news-card-link">
                            ${escapeHtml(linkText)}
                        </a>
                    </div>
                </div>
            `;
        }).join('');
    }

    // Helpers
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

    // --- Health Tip Logic ---
    function fetchHealthTip(callback) {
        fetch('/api/healthtip', { cache: 'no-store' })
            .then(response => response.json())
            .then(data => {
                const section = document.getElementById('healthTipSection');
                const img = document.getElementById('healthTipImgDisplay');
                const textDiv = document.getElementById('healthTipTextDisplay');
                
                if (data.exists && data.filePath) {
                    if (data.isImage === false && data.textContent) {
                        if (img) img.style.display = 'none';
                        if (textDiv) {
                            textDiv.textContent = data.textContent;
                            textDiv.style.display = 'block';
                        }
                    } else {
                        if (textDiv) textDiv.style.display = 'none';
                        if (img) {
                            img.src = data.filePath;
                            img.style.display = 'block';
                        }
                    }
                    section.style.display = 'block';
                } else {
                    section.style.display = 'none';
                }
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching Health Tip:', err);
                const section = document.getElementById('healthTipSection');
                if (section) section.style.display = 'none';
                if (callback) callback();
            });
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
    function fetchQualityDocuments(callback) {
        if (qualityDocumentsList) {
            qualityDocumentsList.innerHTML = '<div style="padding: 20px; color: #777; text-align: center;"><i class="bi bi-arrow-clockwise bi-spin" style="margin-right: 8px;"></i> Loading documents...</div>';
        }

        fetch(`/api/qualityfiles?tab=${encodeURIComponent(currentQualityTab)}`, { cache: 'no-store' })
            .then(res => res.json())
            .then(data => {
                renderQualityDocuments(data);
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching quality files:', err);
                if (qualityDocumentsList) {
                    qualityDocumentsList.innerHTML = '<div style="padding: 20px; color: #dc3545; text-align: center;">Error loading documents. Please try again.</div>';
                }
                if (callback) callback();
            });
    }

    function renderQualityDocuments(files) {
        if (!qualityDocumentsList) return;
        
        if (!files || files.length === 0) {
            qualityDocumentsList.innerHTML = '<div style="padding: 10px; color: #777; grid-column: 1 / -1;">No documents available in this section.</div>';
            return;
        }

        // Group files by subfolder dynamically
        const groupedFiles = {};
        const prefix = `/Uploads/QualityInside/${currentQualityTab}/`;
        
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
        
        // Sort folder keys to have root ('') first, then alphabetical
        const folders = Object.keys(groupedFiles).sort((a, b) => {
            if (a === '') return -1;
            if (b === '') return 1;
            return a.localeCompare(b);
        });

        folders.forEach(folder => {
            html += `<div class="subfolder-section" style="margin-bottom: 25px; background: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); border: 1px solid #e9ecef;">`;
            
            // Only show a header if it's a subfolder OR if there are multiple folders
            if (folder !== '') {
                html += `
                    <h4 style="margin: 0 0 15px 0; font-size: 1.1rem; color: #0056b3; font-weight: 600; border-bottom: 1px solid #eee; padding-bottom: 10px;">
                        ${escapeHtml(folder.replace(/\//g, ' / '))}
                    </h4>
                `;
            }
            
            const folderFiles = groupedFiles[folder];
            html += `<div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 15px; width: 100%;">`;
            
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
                    <a href="${item.FilePath}" download="${escapeHtml(item.FileName)}" class="document-item hover-effect" style="border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; background: #f8fafc; text-decoration: none; display: flex; align-items: center; gap: 10px; transition: all 0.2s;">
                        <i class="bi ${iconClass} doc-icon ${colorClass}" style="font-size: 20px;"></i>
                        <span style="font-weight: 500; font-size: 0.95rem; color: #334155; word-break: break-word;">${escapeHtml(displayName)}</span>
                    </a>
                `;
            }).join('');
            
            html += `</div></div>`;
        });

        qualityDocumentsList.innerHTML = `<div style="display: grid; grid-template-columns: 1fr; width: 100%;">${html}</div>`;
    }
});

// Spin Animation CSS Inject helper for bi-arrow-clockwise
const style = document.createElement('style');
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
