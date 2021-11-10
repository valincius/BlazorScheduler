window.BlazorScheduler = window.BlazorScheduler || {
    mouseUpListener: null,
    mouseMoveListener: null,
};

window.BlazorScheduler.throttle = (fn, delay) => {
    let lastCall = 0;
    return function (...args) {
        const now = (new Date).getTime();
        if (now - lastCall < delay) {
            return;
        }
        lastCall = now;
        return fn(...args);
    }
};

window.BlazorScheduler.attachSchedulerMouseEventsHandler = objRef => {
    window.BlazorScheduler.destroySchedulerMouseEventsHandler();

    window.BlazorScheduler.mouseUpListener = e => objRef.invokeMethodAsync('OnMouseUp', e.button);
    window.BlazorScheduler.mouseMoveListener = window.BlazorScheduler.throttle(e => {
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
    }, 50);

    document.addEventListener('mouseup', window.BlazorScheduler.mouseUpListener);
    document.addEventListener('mousemove', window.BlazorScheduler.mouseMoveListener);
};

window.BlazorScheduler.destroySchedulerMouseEventsHandler = () => {
    if (window.BlazorScheduler.mouseUpListener !== null) {
        document.removeEventListener('mouseup', window.BlazorScheduler.mouseUpListener);
        window.BlazorScheduler.mouseUpListener = null;
    }
    if (window.BlazorScheduler.mouseMoveListener !== null) {
        document.removeEventListener('mousemove', window.BlazorScheduler.mouseMoveListener);
        window.BlazorScheduler.mouseMoveListener = null;
    }
};