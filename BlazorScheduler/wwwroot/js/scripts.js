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
    let elements = Array.from(document.querySelectorAll('.day'));

    let linkCoords = elements.map(link => {
        let rect = link.getBoundingClientRect();
        return [rect.x + rect.width / 2, rect.y + rect.height / 2];
    });

    document.addEventListener('mouseup', e => objRef.invokeMethodAsync('OnMouseUp', e.button));

    document.addEventListener('mousemove', throttle(e => {
        let distances = [];

        linkCoords.forEach(linkCoord => {
            let distance = Math.hypot(linkCoord[0] - parseInt(e.clientX), linkCoord[1] - parseInt(e.clientY));
            distances.push(parseInt(distance));
        });

        let closestLinkIndex = distances.indexOf(Math.min(...distances));
        if (closestLinkIndex >= 0) {
            objRef.invokeMethodAsync('OnMouseMove', elements[closestLinkIndex].dataset.date);
        }
    }, 50));
};