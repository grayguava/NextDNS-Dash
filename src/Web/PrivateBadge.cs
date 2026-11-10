static class PrivateBadge
{
    // Injected into every document while the WebView runs InPrivate. The badge
    // is built through the DOM and isolated in a shadow root so page CSS and
    // CSP cannot remove or restyle it.
    public const string Script = @"(function () {
    if (window.top !== window) return;
    if (document.getElementById('__private_badge')) return;

    function add() {
        if (!document.documentElement) return;
        if (document.getElementById('__private_badge')) return;

        var host = document.createElement('div');
        host.id = '__private_badge';
        host.setAttribute('role', 'img');
        host.setAttribute('aria-label', 'Private mode');
        host.style.setProperty('all', 'initial', 'important');
        host.style.setProperty('position', 'fixed', 'important');
        host.style.setProperty('left', '12px', 'important');
        host.style.setProperty('bottom', '12px', 'important');
        host.style.setProperty('width', '32.2px', 'important');
        host.style.setProperty('height', '32.2px', 'important');
        host.style.setProperty('display', 'block', 'important');
        host.style.setProperty('z-index', '2147483647', 'important');
        host.style.setProperty('pointer-events', 'none', 'important');

        var root = host.attachShadow ? host.attachShadow({ mode: 'closed' }) : host;
        var badge = document.createElement('div');
        badge.style.cssText = 'width:32.2px;height:32.2px;border-radius:50%;box-sizing:border-box;' +
            'background:rgba(20,24,28,0.82);border:1px solid rgba(255,255,255,0.28);' +
            'display:flex;align-items:center;justify-content:center;' +
            'box-shadow:0 1px 4px rgba(0,0,0,0.35);';

        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('width', '18.4');
        svg.setAttribute('height', '18.4');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', '#e9ecef');
        svg.setAttribute('stroke-width', '2');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');

        var eye = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        eye.setAttribute('d', 'M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24');
        var slash = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        slash.setAttribute('x1', '1');
        slash.setAttribute('y1', '1');
        slash.setAttribute('x2', '23');
        slash.setAttribute('y2', '23');

        svg.appendChild(eye);
        svg.appendChild(slash);
        badge.appendChild(svg);
        root.appendChild(badge);
        document.documentElement.appendChild(host);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', add);
    } else {
        add();
    }
})();";
}
