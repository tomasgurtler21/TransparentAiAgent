// Auto-scroll utility for Transparency Viewer
window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
