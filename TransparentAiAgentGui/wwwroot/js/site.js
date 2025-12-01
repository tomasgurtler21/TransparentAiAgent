// Auto-scroll utility for Transparency Viewer
window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Grid column resize functionality - Resizes the third column (overlay column)
window.gridColumnResize = {
    _isResizing: false,
    _startX: 0,
    _startWidth: 0,
    _pageElement: null,
    _resizeHandle: null,
    _initialized: false,
    _observer: null,
    _savedWidth: 400, // Default width

    init: function() {
        // Prevent multiple initializations
        if (this._initialized) {
            return;
        }
        this._initialized = true;

        this._resizeHandle = document.getElementById('gridResizeHandle');
        this._pageElement = document.querySelector('.page');

        if (!this._resizeHandle || !this._pageElement) {
            console.warn('Grid resize: Required elements not found');
            return;
        }

        // Restore saved width
        const savedWidth = localStorage.getItem('gridOverlayColumnWidth');
        if (savedWidth) {
            this._savedWidth = parseInt(savedWidth);
        }

        // Set up mutation observer to watch for overlay visibility changes
        this._setupOverlayObserver();

        // Apply initial state
        this._updateGridBasedOnOverlayState();

        // Add mousedown listener to resize handle
        this._resizeHandle.addEventListener('mousedown', (e) => {
            this._isResizing = true;
            this._startX = e.clientX;

            // Get current width from computed style
            const currentGridColumns = window.getComputedStyle(this._pageElement).gridTemplateColumns;
            const columns = currentGridColumns.split(' ');
            this._startWidth = parseInt(columns[2]) || 400; // Default to 400px if parsing fails

            document.body.style.cursor = 'ew-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        });

        // Mousemove listener on document
        document.addEventListener('mousemove', (e) => {
            if (!this._isResizing) return;

            // Calculate new width (drag left = increase width, drag right = decrease width)
            const deltaX = this._startX - e.clientX;
            const newWidth = this._startWidth + deltaX;

            // Clamp width between 300px and 60% of window width
            const minWidth = 300;
            const maxWidth = window.innerWidth * 0.6;
            const clampedWidth = Math.max(minWidth, Math.min(newWidth, maxWidth));

            this._applyColumnWidth(clampedWidth);
        });

        // Mouseup listener on document
        document.addEventListener('mouseup', () => {
            if (this._isResizing) {
                // Get final width from computed style
                const currentGridColumns = window.getComputedStyle(this._pageElement).gridTemplateColumns;
                const columns = currentGridColumns.split(' ');
                const finalWidth = parseInt(columns[2]) || 400;

                // Store width preference (both in memory and localStorage)
                this._savedWidth = finalWidth;
                localStorage.setItem('gridOverlayColumnWidth', finalWidth);

                // Reset state
                this._isResizing = false;
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
            }
        });
    },

    _applyColumnWidth: function(width) {
        if (!this._pageElement) return;

        // Update grid template columns: 250px (sidebar) 1fr (main) [width]px (overlay)
        // Use setProperty with 'important' to override CSS !important
        this._pageElement.style.setProperty('grid-template-columns', `250px 1fr ${width}px`, 'important');

        // Update resize handle position to match
        if (this._resizeHandle) {
            this._resizeHandle.style.right = `${width}px`;
        }
    },

    _setupOverlayObserver: function() {
        // Watch for changes to overlay-panel elements (class changes)
        const overlayContainer = document.querySelector('.overlay-container');
        if (!overlayContainer) return;

        this._observer = new MutationObserver(() => {
            this._updateGridBasedOnOverlayState();
        });

        // Observe the overlay container for attribute changes (class changes on children)
        this._observer.observe(overlayContainer, {
            attributes: true,
            attributeFilter: ['class'],
            subtree: true
        });
    },

    _updateGridBasedOnOverlayState: function() {
        // Check if any overlay is visible
        const hasVisibleOverlay = document.querySelector('.overlay-panel.visible') !== null;

        if (hasVisibleOverlay) {
            // Expand to saved width (or current width if user is resizing)
            if (!this._isResizing) {
                this._applyColumnWidth(this._savedWidth);
            }
        } else {
            // Collapse to 0px when no overlay is visible
            this._applyColumnWidth(0);
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
