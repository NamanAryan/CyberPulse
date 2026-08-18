// Shared portal shell - renders the top navigation and the page banner so every
// tab page stays in sync from a single definition. Pages supply their own copy
// as props via data attributes on the mount points:
//
//   <nav data-portal-nav data-active="home"></nav>
//   <div data-portal-banner data-title="Home" data-tagline="Your Daily Pulse"></div>
//
// The banner is a static image + text overlay (it is not a carousel), so there
// are no dots or arrow controls on it.

(function () {
    const NAV_ITEMS = [
        { key: 'home', label: 'Home', href: '/index.html' },
        { key: 'hr', label: 'HR Connect', href: '/hr-connect.html' },
        { key: 'quality', label: 'Quality Insights', href: '/quality.html' }
    ];

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function renderNav(mount) {
        const active = mount.dataset.active || '';

        mount.classList.add('portal-nav');
        mount.setAttribute('aria-label', 'Main navigation');
        mount.innerHTML = `
            <div class="container portal-nav-inner">
                ${NAV_ITEMS.map(item => {
                    const isActive = item.key === active;
                    return `<a class="portal-nav-link${isActive ? ' active' : ''}" href="${item.href}"${isActive ? ' aria-current="page"' : ''}>${item.label}</a>`;
                }).join('')}
            </div>
        `;
    }

    function renderBanner(mount) {
        const title = mount.dataset.title || '';
        const tagline = mount.dataset.tagline || '';

        mount.classList.add('page-banner');
        mount.innerHTML = `
            <div class="container page-banner-inner">
                <h1 class="page-banner-title">${escapeHtml(title)}</h1>
                ${tagline ? `<p class="page-banner-tagline">${escapeHtml(tagline)}</p>` : ''}
            </div>
        `;
    }

    // Shared header chrome: the date readout appears on every page, so it lives
    // here instead of being repeated in each page controller.
    function startHeaderDate() {
        const display = document.getElementById('currentDate');
        if (!display) return;

        const update = () => {
            const options = { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' };
            display.textContent = new Date().toLocaleDateString('en-US', options);
        };

        update();
        setInterval(update, 60000);
    }

    // The header bar and the nav are both sticky, so the nav has to dock right
    // below the header. Publish the header's measured height as a CSS variable
    // (see `.portal-nav` in style.css) so the offset survives wrapping headers
    // and viewport changes.
    function trackHeaderHeight() {
        const header = document.querySelector('.header-bar');
        if (!header) return;

        const update = () => {
            document.documentElement.style.setProperty('--portal-header-height', header.offsetHeight + 'px');
        };

        update();
        window.addEventListener('resize', update);
    }

    function init() {
        document.querySelectorAll('[data-portal-nav]').forEach(renderNav);
        document.querySelectorAll('[data-portal-banner]').forEach(renderBanner);
        startHeaderDate();
        trackHeaderHeight();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
