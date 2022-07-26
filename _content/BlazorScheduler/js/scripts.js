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

        let mouseX = parseInt(e.clientX);
        let mouseY = parseInt(e.clientY);

        let elements = Array.from(document.querySelectorAll('.day'));
        for (elem of elements) {
            let rect = elem.getBoundingClientRect();

            if (rect.x <= mouseX && (rect.x + rect.width) >= mouseX
                && rect.y <= mouseY && (rect.y + rect.height) >= mouseY) {
                objRef.invokeMethodAsync('OnMouseMove', elem.dataset.date);
                return;
            }

            let x = rect.x + rect.width / 2;
            let y = rect.y + rect.height / 2;
            let distance = Math.hypot(x - mouseX, y - mouseY);
            distances.push({ elem, dist: parseInt(distance) });
        }

        distances.sort((a, b) => a.dist - b.dist);
        let closest = distances[0];
        if (closest !== null) {
            objRef.invokeMethodAsync('OnMouseMove', closest.elem.dataset.date);
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