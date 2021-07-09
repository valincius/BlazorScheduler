// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

function throttle(fn, delay) {
    let lastCall = 0;
    return function (...args) {
        const now = (new Date).getTime();
        if (now - lastCall < delay) {
            return;
        }
        lastCall = now;
        return fn(...args);
    }
}

window.attachSchedulerMouseEventsHandler = objRef => {
    document.addEventListener('mouseup', e => objRef.invokeMethodAsync('OnMouseUp', e.button));

    document.addEventListener('mousemove', throttle(e => {
        let distances = [];

        let elements = Array.from(document.querySelectorAll('.day'));
        elements.forEach(elem => {
            let rect = elem.getBoundingClientRect();
            let x = rect.x + rect.width / 2;
            let y = rect.y + rect.height / 2;
            let distance = Math.hypot(x - parseInt(e.clientX), y - parseInt(e.clientY));
            distances.push(parseInt(distance));
        });

        let closestLinkIndex = distances.indexOf(Math.min(...distances));
        if (closestLinkIndex >= 0) {
            objRef.invokeMethodAsync('OnMouseMove', elements[closestLinkIndex].dataset.date);
        }
    }, 50));
};