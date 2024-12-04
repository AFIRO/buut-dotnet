// Add a resize listener and debounce the resize events
let resizeTimeout;
window.addResizeListener = (dotNetHelper) => {
    window.addEventListener("resize", () => {
        // Clear the previous timeout to wait for the next resize event
        clearTimeout(resizeTimeout);

        // Set a new timeout to call Blazor's OnWindowResize after 300ms
        resizeTimeout = setTimeout(() => {
            const newWidth = window.innerWidth;
            dotNetHelper.invokeMethodAsync("OnWindowResize", newWidth)
                .catch(err => console.error('Error calling OnWindowResize:', err));
        }, 300); // Adjust debounce time as needed (e.g., 300ms)
    });
};

// Get the initial window width to send to Blazor on component initialization
window.getWindowWidth = () => {
    return window.innerWidth;
};
