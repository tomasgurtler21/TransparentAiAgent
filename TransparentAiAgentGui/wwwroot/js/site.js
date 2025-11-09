// Auto-scroll utility for Transparency Viewer
window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Overlay resize functionality
window.overlayResize = {
    // Shared state for resize operation
    _isResizing: false,
    _startX: 0,
    _startWidth: 0,
    _currentOverlay: null,
    _initialized: false,

    init: function() {
        // Prevent multiple initializations
        if (this._initialized) {
            return;
        }
        this._initialized = true;

        // Add mousedown listeners to each handle
        const handles = document.querySelectorAll('.overlay-resize-handle');
        handles.forEach(handle => {
            handle.addEventListener('mousedown', (e) => {
                this._isResizing = true;
                this._startX = e.clientX;
                this._currentOverlay = handle.closest('.overlay-panel-right');

                if (this._currentOverlay) {
                    this._startWidth = this._currentOverlay.offsetWidth;
                    document.body.style.cursor = 'ew-resize';
                    document.body.style.userSelect = 'none';
                }
                e.preventDefault();
            });
        });

        // Single mousemove listener on document
        document.addEventListener('mousemove', (e) => {
            if (!this._isResizing || !this._currentOverlay) return;

            const deltaX = this._startX - e.clientX;
            const newWidth = this._startWidth + deltaX;

            // Min width: 300px, Max width: 80vw
            const minWidth = 300;
            const maxWidth = window.innerWidth * 0.8;
            const clampedWidth = Math.max(minWidth, Math.min(newWidth, maxWidth));

            this._currentOverlay.style.width = clampedWidth + 'px';
        });

        // Single mouseup listener on document
        document.addEventListener('mouseup', () => {
            if (this._isResizing && this._currentOverlay) {
                // Store width preference
                localStorage.setItem('overlayWidth', this._currentOverlay.style.width);
            }

            // Reset state
            this._isResizing = false;
            this._currentOverlay = null;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
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
