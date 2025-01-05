window.clickElement = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.dispatchEvent(new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            view: window
        }));
    }
};

window.dispatchCustomEvent = function (eventName) {
    const event = new Event(eventName);
    window.dispatchEvent(event);
};
