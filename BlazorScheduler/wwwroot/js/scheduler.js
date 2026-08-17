class SchedulerPointerHandler {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.active = false;
        this.lastDay = null;
        this.pointerDown = this.pointerDown.bind(this);
        this.pointerMove = this.pointerMove.bind(this);
        this.pointerUp = this.pointerUp.bind(this);
        root.addEventListener('pointerdown', this.pointerDown);
        root.addEventListener('pointermove', this.pointerMove);
        root.addEventListener('pointerup', this.pointerUp);
        root.addEventListener('pointercancel', this.pointerUp);
    }

    dayAt(clientX, clientY) {
        const weeks = Array.from(this.root.querySelectorAll('.week:not(.header)'));
        if (weeks.length === 0) return null;
        const firstDays = weeks[0].querySelector('.days').getBoundingClientRect();
        const rowHeight = weeks[0].getBoundingClientRect().height;
        const column = Math.max(0, Math.min(6, Math.floor((clientX - firstDays.left) / (firstDays.width / 7))));
        const row = Math.max(0, Math.min(weeks.length - 1, Math.floor((clientY - firstDays.top) / rowHeight)));
        return weeks[row].querySelectorAll('[data-scheduler-day]')[column]?.dataset.schedulerDay ?? null;
    }

    pointerDown(event) {
        if (event.button !== 0) return;
        const day = this.dayAt(event.clientX, event.clientY);
        if (!day) return;
        const item = event.target.closest('[data-scheduler-item]');
        this.active = true;
        this.lastDay = day;
        this.root.setPointerCapture?.(event.pointerId);
        if (item) this.dotnet.invokeMethodAsync('BeginItemDrag', Number(item.dataset.schedulerItem), day);
        else this.dotnet.invokeMethodAsync('BeginDayDrag', day);
    }

    pointerMove(event) {
        if (!this.active) return;
        const day = this.dayAt(event.clientX, event.clientY);
        if (day && day !== this.lastDay) {
            this.lastDay = day;
            this.dotnet.invokeMethodAsync('DragTo', day);
        }
    }

    pointerUp() {
        if (!this.active) return;
        this.active = false;
        this.lastDay = null;
        this.dotnet.invokeMethodAsync('CompleteDrag');
    }

    dispose() {
        this.root.removeEventListener('pointerdown', this.pointerDown);
        this.root.removeEventListener('pointermove', this.pointerMove);
        this.root.removeEventListener('pointerup', this.pointerUp);
        this.root.removeEventListener('pointercancel', this.pointerUp);
        this.root = null;
        this.dotnet = null;
    }
}

export function create(element, reference) {
    return new SchedulerPointerHandler(element, reference);
}
