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

// File export utility for Transparency Viewer
window.downloadFile = function(filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
};

// Chat auto-scroll functionality
window.chatAutoScroll = {
    _scrollContainers: new Map(),

    // Initialize auto-scroll for a container
    init: function(containerElement, dotNetRef) {
        if (!containerElement) return;

        const containerId = containerElement.id || this._generateId();
        if (!containerElement.id) {
            containerElement.id = containerId;
        }

        // Store container state
        this._scrollContainers.set(containerId, {
            element: containerElement,
            dotNetRef: dotNetRef,
            autoScrollEnabled: true,
            isUserScrolling: false,
            scrollTimeout: null
        });

        // Add scroll event listener
        containerElement.addEventListener('scroll', (e) => this._handleScroll(containerId, e));

        // Initial scroll to bottom
        this.scrollToBottom(containerId);
    },

    // Handle user scroll events
    _handleScroll: function(containerId, event) {
        const state = this._scrollContainers.get(containerId);
        if (!state) return;

        const element = state.element;
        const isAtBottom = Math.abs(element.scrollHeight - element.scrollTop - element.clientHeight) < 5;

        // Clear existing timeout
        if (state.scrollTimeout) {
            clearTimeout(state.scrollTimeout);
        }

        // If user scrolled away from bottom, disable auto-scroll
        if (!isAtBottom && !state.isUserScrolling) {
            state.autoScrollEnabled = false;
            state.isUserScrolling = true;
        }

        // Set timeout to detect end of scroll
        state.scrollTimeout = setTimeout(() => {
            state.isUserScrolling = false;

            // If user scrolled back to bottom, re-enable auto-scroll
            const stillAtBottom = Math.abs(element.scrollHeight - element.scrollTop - element.clientHeight) < 5;
            if (stillAtBottom) {
                state.autoScrollEnabled = true;
            }
        }, 150);
    },

    // Scroll to bottom if auto-scroll is enabled
    scrollToBottom: function(containerId) {
        const state = this._scrollContainers.get(containerId);
        if (!state) return;

        if (state.autoScrollEnabled) {
            state.element.scrollTop = state.element.scrollHeight;
        }
    },

    // Force enable auto-scroll and scroll to bottom (called when user sends message)
    enableAndScroll: function(containerId) {
        const state = this._scrollContainers.get(containerId);
        if (!state) return;

        state.autoScrollEnabled = true;
        state.isUserScrolling = false;
        state.element.scrollTop = state.element.scrollHeight;
    },

    // Cleanup
    dispose: function(containerId) {
        const state = this._scrollContainers.get(containerId);
        if (state) {
            if (state.scrollTimeout) {
                clearTimeout(state.scrollTimeout);
            }
            this._scrollContainers.delete(containerId);
        }
    },

    // Generate unique ID
    _generateId: function() {
        return 'chat-scroll-' + Math.random().toString(36).substr(2, 9);
    }
};
