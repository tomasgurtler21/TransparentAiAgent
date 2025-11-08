// Auto-scroll utility for Transparency Viewer
window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Overlay resize functionality
window.overlayResize = {
    init: function() {
        const handles = document.querySelectorAll('.overlay-resize-handle');
        handles.forEach(handle => {
            let isResizing = false;
            let startX = 0;
            let startWidth = 0;
            let overlay = null;

            handle.addEventListener('mousedown', (e) => {
                isResizing = true;
                startX = e.clientX;
                overlay = handle.closest('.overlay-panel-right');
                if (overlay) {
                    startWidth = overlay.offsetWidth;
                    document.body.style.cursor = 'ew-resize';
                    document.body.style.userSelect = 'none';
                }
                e.preventDefault();
            });

            document.addEventListener('mousemove', (e) => {
                if (!isResizing || !overlay) return;

                const deltaX = startX - e.clientX;
                const newWidth = startWidth + deltaX;

                // Min width: 300px, Max width: 80vw
                const minWidth = 300;
                const maxWidth = window.innerWidth * 0.8;
                const clampedWidth = Math.max(minWidth, Math.min(newWidth, maxWidth));

                overlay.style.width = clampedWidth + 'px';
            });

            document.addEventListener('mouseup', () => {
                if (isResizing && overlay) {
                    // Store width preference
                    localStorage.setItem('overlayWidth', overlay.style.width);
                }
                isResizing = false;
                overlay = null;
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
            });
        });

        // Restore saved width
        const savedWidth = localStorage.getItem('overlayWidth');
        if (savedWidth) {
            document.querySelectorAll('.overlay-panel-right').forEach(overlay => {
                overlay.style.width = savedWidth;
            });
        }
    }
};
