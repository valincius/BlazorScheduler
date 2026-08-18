// Pointer-driven drag handling for the DataScheduler<TItem>.
//
// Two drag surfaces are supported:
//  - The month grid: a press on an empty day starts a day-span create drag;
//    a press on an appointment starts a reschedule drag. Days are whole dates.
//  - The week-view time grid: a press on an empty spot in a day column starts
//    a timed create drag within that column. The pointer's Y position is
//    converted to a minute-of-day (snapped to 15 minutes), and horizontal
//    drift is ignored. Presses on timed appointments are clicks only for now
//    (time-accurate item rescheduling is not wired up yet).
//
// Design notes:
//  - A drag may only start from a press on the calendar grid itself (a day
//    row or a week time-grid column), never on header controls or other
//    interactive elements. This keeps the Today / previous / next buttons and
//    the view switcher clickable, even though the pointer handler listens on
//    the whole scheduler root.
//  - Pointer capture is engaged lazily, only once the pointer actually moves
//    past a small threshold. A plain press never captures, so the browser's
//    click event always reaches its original target (item clicks, buttons) in
//    every browser, regardless of how capture retargets click events.

const DRAG_SURFACE_SELECTOR = '.week:not(.header)';
const DAY_CELL_SELECTOR = '[data-scheduler-day]';
const ITEM_SELECTOR = '[data-scheduler-item]';
const WEEK_GRID_SELECTOR = '[data-scheduler-time-grid]';
const WEEK_COLUMN_SELECTOR = '.day-column';
const INTERACTIVE_SELECTOR =
    'button, select, input, textarea, a[href], [contenteditable], [data-scheduler-interactive]';
const DRAG_THRESHOLD = 4; // px of movement before a press becomes a drag
const WEEK_SNAP_MINUTES = 15; // drag positions in the week grid snap to this

/** True when the press target is (or sits inside) an interactive control. */
export function isInteractiveTarget(target) {
    return !!(target && typeof target.closest === 'function' && target.closest(INTERACTIVE_SELECTOR));
}

/**
 * Resolves the day under the given viewport coordinates, or null when the
 * pointer is not inside the day grid at all (header, gutter, outside edges).
 */
export function dayAt(root, clientX, clientY) {
    const weeks = Array.from(root.querySelectorAll(DRAG_SURFACE_SELECTOR));
    if (weeks.length === 0) return null;

    const days = weeks[0].querySelector('.days');
    if (!days) return null;
    const daysRect = days.getBoundingClientRect();
    if (daysRect.width <= 0) return null;

    const column = Math.floor((clientX - daysRect.left) / (daysRect.width / 7));
    if (column < 0 || column >= 7) return null;

    let row = -1;
    for (let index = 0; index < weeks.length; index++) {
        const rect = weeks[index].getBoundingClientRect();
        if (clientY >= rect.top && clientY < rect.bottom) {
            row = index;
            break;
        }
    }
    if (row < 0) return null;

    const cells = weeks[row].querySelectorAll(DAY_CELL_SELECTOR);
    return cells[column]?.dataset.schedulerDay ?? null;
}

/**
 * Resolves the week-view time-grid position under the given viewport
 * coordinates, or null when the pointer is not inside a day column.
 * Returns { day, minutes } where minutes is the snapped minute-of-day.
 */
export function weekViewAt(root, clientX, clientY) {
    const grid = root.querySelector(WEEK_GRID_SELECTOR);
    if (!grid) return null;
    const startHour = Number(grid.dataset.schedulerViewStart ?? 0);
    const endHour = Number(grid.dataset.schedulerViewEnd ?? 24);
    if (endHour <= startHour) return null;

    const columns = Array.from(grid.querySelectorAll(WEEK_COLUMN_SELECTOR));
    for (const column of columns) {
        const rect = column.getBoundingClientRect();
        if (clientX < rect.left || clientX >= rect.right || clientY < rect.top || clientY >= rect.bottom) {
            continue;
        }
        const fraction = rect.height > 0 ? (clientY - rect.top) / rect.height : 0;
        const rawMinutes = startHour * 60 + fraction * (endHour - startHour) * 60;
        const snapped = Math.round(rawMinutes / WEEK_SNAP_MINUTES) * WEEK_SNAP_MINUTES;
        const minutes = Math.max(startHour * 60, Math.min(endHour * 60, snapped));
        return { day: column.dataset.schedulerDay, minutes };
    }
    return null;
}

/**
 * Decides whether a pointer press should begin a drag, and what it targets.
 * Returns { day, item, week? } when the press lands on a drag surface,
 * otherwise null. item is the data-scheduler-item source index, or null for a
 * plain surface press. week is true for week-view time-grid presses.
 */
export function resolveDragStart(root, target, clientX, clientY) {
    if (isInteractiveTarget(target)) return null;
    const inWeekGrid = !!target.closest(WEEK_GRID_SELECTOR);
    if (!inWeekGrid && !target.closest(DRAG_SURFACE_SELECTOR)) return null;
    if (inWeekGrid) {
        // Timed appointments in the week view are click-only for now; time-
        // accurate item rescheduling is not wired up yet.
        if (target.closest(ITEM_SELECTOR)) return null;
        const position = weekViewAt(root, clientX, clientY);
        return position ? { day: position.day, minutes: position.minutes, item: null, week: true } : null;
    }
    const day = dayAt(root, clientX, clientY);
    if (!day) return null;
    const item = target.closest(ITEM_SELECTOR);
    return item ? { day, item: Number(item.dataset.schedulerItem) } : { day, item: null };
}

export class SchedulerPointerHandler {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.active = false; // a drag is in progress (capture engaged)
        this.pending = null; // a press awaiting movement, { day, item, week? }
        this.pointerId = null;
        this.pressX = 0;
        this.pressY = 0;
        this.mode = null; // 'month' | 'week'
        this.lastValue = null; // last reported day (month) or minutes (week)
        this.pointerDown = this.pointerDown.bind(this);
        this.pointerMove = this.pointerMove.bind(this);
        this.pointerUp = this.pointerUp.bind(this);
        root.addEventListener('pointerdown', this.pointerDown);
        window.addEventListener('pointermove', this.pointerMove);
        window.addEventListener('pointerup', this.pointerUp);
        window.addEventListener('pointercancel', this.pointerUp);
    }

    pointerDown(event) {
        if (event.button !== 0) return;
        const drag = resolveDragStart(this.root, event.target, event.clientX, event.clientY);
        if (!drag) return;
        this.pending = drag;
        this.pointerId = event.pointerId;
        this.pressX = event.clientX;
        this.pressY = event.clientY;
        this.mode = drag.week ? 'week' : 'month';
        this.lastValue = drag.week ? drag.minutes : drag.day;
        if (drag.week) {
            this.dotnet.invokeMethodAsync('BeginWeekDrag', `${drag.day}|${drag.minutes}`);
        } else if (drag.item !== null) {
            this.dotnet.invokeMethodAsync('BeginItemDrag', drag.item, drag.day);
        } else {
            this.dotnet.invokeMethodAsync('BeginDayDrag', drag.day);
        }
    }

    pointerMove(event) {
        if (this.pointerId !== event.pointerId) return;
        if (this.pending) {
            const dx = event.clientX - this.pressX;
            const dy = event.clientY - this.pressY;
            if (Math.abs(dx) < DRAG_THRESHOLD && Math.abs(dy) < DRAG_THRESHOLD) return;
            // The press became a real drag: engage capture so the rest of the
            // gesture keeps flowing to us even outside the scheduler.
            this.active = true;
            this.pending = null;
            this.root.setPointerCapture?.(event.pointerId);
        }
        if (!this.active) return;
        if (this.mode === 'week') {
            // Only minute changes matter: the appointment stays in the anchor
            // column, so horizontal drift is intentionally ignored.
            const position = weekViewAt(this.root, event.clientX, event.clientY);
            if (position && position.minutes !== this.lastValue) {
                this.lastValue = position.minutes;
                this.dotnet.invokeMethodAsync('DragWeekTo', String(position.minutes));
            }
            return;
        }
        const day = dayAt(this.root, event.clientX, event.clientY);
        if (day && day !== this.lastValue) {
            this.lastValue = day;
            this.dotnet.invokeMethodAsync('DragTo', day);
        }
    }

    pointerUp(event) {
        const wasTracking = this.pointerId === event.pointerId;
        this.active = false;
        this.pending = null;
        this.lastValue = null;
        this.mode = null;
        this.pointerId = null;
        // Always tell .NET the gesture ended: it decides whether the press was
        // a click (no movement) or a completed drag, and clears its preview.
        if (wasTracking) {
            this.dotnet.invokeMethodAsync('CompleteDrag');
        }
    }

    dispose() {
        this.root.removeEventListener('pointerdown', this.pointerDown);
        window.removeEventListener('pointermove', this.pointerMove);
        window.removeEventListener('pointerup', this.pointerUp);
        window.removeEventListener('pointercancel', this.pointerUp);
        this.root = null;
        this.dotnet = null;
    }
}

export function create(element, reference) {
    return new SchedulerPointerHandler(element, reference);
}
