// Unit tests for the scheduler pointer module (BlazorScheduler/wwwroot/js/scheduler.js).
//
// These cover the regression where a press anywhere inside the scheduler — even
// on header controls that are not a day — started a drag and captured the
// pointer, making the Today / previous / next buttons and the view switcher
// unclickable on month view.
//
// Run with: node --test tests/js/

import test from 'node:test';
import assert from 'node:assert/strict';
import {
    isInteractiveTarget,
    dayAt,
    weekViewAt,
    doubleClickDayAt,
    resolveDragStart,
    SchedulerPointerHandler,
} from '../../BlazorScheduler/wwwroot/js/scheduler.js';

// ---------------------------------------------------------------------------
// Minimal fake DOM (enough for the selectors and geometry scheduler.js uses)
// ---------------------------------------------------------------------------

const INTERACTIVE_SELECTOR =
    'button, select, input, textarea, a[href], [contenteditable], [data-scheduler-interactive]';

function hasClass(el, name) {
    return (el.className || '').split(/\s+/).includes(name);
}

function matchesSelector(el, selector) {
    if (selector === '.week:not(.header)') {
        return hasClass(el, 'week') && !hasClass(el, 'header');
    }
    if (selector.startsWith('.')) {
        return hasClass(el, selector.slice(1));
    }
    if (selector === '[data-scheduler-day]') return el.dataset.schedulerDay !== undefined;
    if (selector === '[data-scheduler-item]') return el.dataset.schedulerItem !== undefined;
    if (selector === '[data-scheduler-time-grid]') return el.dataset.schedulerTimeGrid !== undefined;
    if (selector === '[data-scheduler-interactive]') return el.dataset.schedulerInteractive !== undefined;
    if (selector === '[contenteditable]') return el.attributes.contenteditable !== undefined;
    if (selector === 'a[href]') return el.tagName === 'A' && el.attributes.href !== undefined;
    if (selector === 'button' || selector === 'select' || selector === 'input' || selector === 'textarea') {
        return el.tagName === selector.toUpperCase();
    }
    if (selector === INTERACTIVE_SELECTOR) {
        return INTERACTIVE_SELECTOR.split(',').some(part => matchesSelector(el, part.trim()));
    }
    throw new Error(`unsupported selector: ${selector}`);
}

function collectDescendants(el, selector, out) {
    for (const child of el._children) {
        if (matchesSelector(child, selector)) out.push(child);
        collectDescendants(child, selector, out);
    }
}

function makeElement({ tag = 'div', className = '', dataset = {}, attributes = {}, rect = null, children = [] } = {}) {
    const el = {
        tagName: tag.toUpperCase(),
        className,
        dataset: { ...dataset },
        attributes: { ...attributes },
        _rect: rect,
        _children: children,
        parentNode: null,
    };
    for (const child of children) child.parentNode = el;
    el.matches = selector => matchesSelector(el, selector);
    el.closest = selector => {
        let cur = el;
        while (cur) {
            if (matchesSelector(cur, selector)) return cur;
            cur = cur.parentNode;
        }
        return null;
    };
    el.querySelector = selector => {
        const found = [];
        collectDescendants(el, selector, found);
        return found[0] ?? null;
    };
    el.querySelectorAll = selector => {
        const found = [];
        collectDescendants(el, selector, found);
        return found;
    };
    el.getBoundingClientRect = () => {
        if (!el._rect) throw new Error(`no rect for ${el.className || el.tagName}`);
        const { left, top, width, height } = el._rect;
        return { left, top, width, height, right: left + width, bottom: top + height };
    };
    return el;
}

/** A scheduler root: header (buttons + view select) above a two-week month grid. */
function buildScheduler() {
    const weeks = [];
    for (let w = 0; w < 2; w++) {
        const cells = [];
        for (let d = 0; d < 7; d++) {
            cells.push(makeElement({
                dataset: { schedulerDay: `202608${String(w * 7 + d + 1).padStart(2, '0')}` },
                rect: { left: 100 + d * 50, top: 200 + w * 100, width: 50, height: 60 },
            }));
        }
        const days = makeElement({ className: 'days', rect: { left: 100, top: 200 + w * 100, width: 350, height: 60 }, children: cells });
        const appointments = makeElement({ className: 'appointments', rect: { left: 100, top: 260 + w * 100, width: 350, height: 40 } });
        weeks.push(makeElement({ className: 'week', rect: { left: 100, top: 200 + w * 100, width: 350, height: 100 }, children: [days, appointments] }));
    }
    const headerWeek = makeElement({ className: 'week header', rect: { left: 100, top: 150, width: 350, height: 50 } });
    const month = makeElement({ className: 'month', rect: { left: 100, top: 150, width: 350, height: 250 }, children: [headerWeek, ...weeks] });

    const todayBtn = makeElement({ tag: 'button', className: 'btn today', rect: { left: 110, top: 20, width: 80, height: 30 } });
    const navBtn = makeElement({ tag: 'button', className: 'btn icon-btn', rect: { left: 200, top: 20, width: 30, height: 30 } });
    const actions = makeElement({ className: 'actions', rect: { left: 100, top: 10, width: 350, height: 50 }, children: [todayBtn, navBtn] });
    const viewSelect = makeElement({ tag: 'select', className: 'view-select', rect: { left: 110, top: 70, width: 100, height: 30 } });
    const viewSwitcher = makeElement({ className: 'view-switcher', rect: { left: 100, top: 65, width: 350, height: 40 }, children: [viewSelect] });
    const header = makeElement({ className: 'header', rect: { left: 100, top: 0, width: 350, height: 110 }, children: [actions, viewSwitcher] });

    const root = makeElement({ className: 'scheduler', rect: { left: 100, top: 0, width: 350, height: 360 }, children: [header, month] });
    return { root, month, weeks, todayBtn, viewSelect, header };
}

/** Adds the event/capture surface the handler class needs onto the fake root. */
function attachEvents(root, captures) {
    const target = { ...root };
    target.addEventListener = () => {};
    target.removeEventListener = () => {};
    target.setPointerCapture = id => captures.push(id);
    return target;
}

/**
 * A week-view fixture: a time grid from 8:00 to 18:00 (10 rows at 60px each)
 * with seven 100px-wide day columns. Each column spans the full grid height.
 */
function buildWeekView() {
    const columns = [];
    for (let d = 0; d < 7; d++) {
        columns.push(makeElement({
            className: 'day-column',
            dataset: { schedulerDay: `2026081${d}` },
            rect: { left: 150 + d * 100, top: 100, width: 100, height: 600 },
        }));
    }
    const timeGrid = makeElement({
        className: 'time-grid',
        dataset: { schedulerTimeGrid: '', schedulerViewStart: '8', schedulerViewEnd: '18' },
        rect: { left: 100, top: 100, width: 800, height: 600 },
        children: columns,
    });
    const allDayStrip = makeElement({ className: 'all-day-strip', rect: { left: 100, top: 60, width: 800, height: 40 } });
    const weekDayHeader = makeElement({ className: 'week-day-header', rect: { left: 100, top: 30, width: 800, height: 30 } });
    const weekView = makeElement({
        className: 'week-view',
        rect: { left: 100, top: 30, width: 800, height: 670 },
        children: [weekDayHeader, allDayStrip, timeGrid],
    });
    const root = makeElement({ className: 'scheduler', rect: { left: 100, top: 0, width: 800, height: 700 }, children: [weekView] });
    return { root, weekView, timeGrid, columns };
}

function fakeDotnet(calls) {
    return { invokeMethodAsync: (...args) => { calls.push(args); return Promise.resolve(); } };
}

function fakeWindow() {
    const listeners = {};
    return {
        addEventListener: (type, fn) => { listeners[type] = fn; },
        removeEventListener: () => {},
        _listeners: listeners,
    };
}

const press = (target, clientX, clientY, pointerId = 1) => ({ button: 0, clientX, clientY, pointerId, target });
const move = (clientX, clientY, pointerId = 1) => ({ clientX, clientY, pointerId });
const release = (pointerId = 1) => ({ pointerId });

// ---------------------------------------------------------------------------
// Pure helpers
// ---------------------------------------------------------------------------

test('dayAt resolves cells inside the grid', () => {
    const { root } = buildScheduler();
    assert.equal(dayAt(root, 125, 230), '20260801');
    assert.equal(dayAt(root, 300, 230), '20260805');
    assert.equal(dayAt(root, 125, 330), '20260808'); // second week
    assert.equal(dayAt(root, 449, 230), '20260807'); // last column edge
});

test('dayAt returns null outside the grid', () => {
    const { root } = buildScheduler();
    assert.equal(dayAt(root, 125, 30), null);  // header area (above the grid)
    assert.equal(dayAt(root, 125, 500), null); // below the grid
    assert.equal(dayAt(root, 10, 230), null);  // left of the grid
    assert.equal(dayAt(root, 600, 230), null); // right of the grid
    assert.equal(dayAt(root, 125, 160), null); // day-name header row inside .month
});

test('isInteractiveTarget identifies controls', () => {
    const cases = [
        makeElement({ tag: 'button' }),
        makeElement({ tag: 'select' }),
        makeElement({ tag: 'input' }),
        makeElement({ tag: 'textarea' }),
        makeElement({ tag: 'a', attributes: { href: '#' } }),
        makeElement({ attributes: { contenteditable: '' } }),
        makeElement({ dataset: { schedulerInteractive: '' } }),
    ];
    for (const el of cases) assert.equal(isInteractiveTarget(el), true);
    assert.equal(isInteractiveTarget(makeElement({})), false);
    assert.equal(isInteractiveTarget(null), false);
});

test('resolveDragStart only starts drags from the grid surface', () => {
    const { root, todayBtn, header, weeks } = buildScheduler();
    const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];

    // Header controls are never drag starts, even though they live in the scheduler root.
    assert.equal(resolveDragStart(root, todayBtn, 140, 35), null);
    assert.equal(resolveDragStart(root, header, 140, 35), null);
    // Presses outside the grid bounds are not drag starts either.
    assert.equal(resolveDragStart(root, dayCell, 125, 30), null);

    // A day cell press is a day drag.
    assert.deepEqual(resolveDragStart(root, dayCell, 125, 230), { day: '20260801', item: null });

    // An appointment press is an item drag.
    const appointments = weeks[0].querySelector('.appointments');
    const item = makeElement({ dataset: { schedulerItem: '2' }, rect: { left: 120, top: 270, width: 150, height: 24 } });
    appointments._children.push(item);
    item.parentNode = appointments;
    assert.deepEqual(resolveDragStart(root, item, 150, 280), { day: '20260802', item: 2 });

    // An overflow button inside the grid is interactive: no drag.
    const overflow = makeElement({ tag: 'button', className: 'scheduler-overflow', rect: { left: 120, top: 270, width: 150, height: 24 } });
    appointments._children.push(overflow);
    overflow.parentNode = appointments;
    assert.equal(resolveDragStart(root, overflow, 150, 280), null);
});

test('a week view without a time grid has no drag surface', () => {
    const weekView = makeElement({ className: 'week-view' });
    const root = makeElement({ className: 'scheduler', children: [weekView] });
    assert.equal(dayAt(root, 100, 100), null);
    assert.equal(resolveDragStart(root, weekView, 100, 100), null);
});

test('weekViewAt resolves day and snapped minutes inside a column', () => {
    const { root } = buildWeekView();
    // Column 0 spans x [150, 250), y [100, 700). y=160 is 60px in = 1 hour.
    assert.deepEqual(weekViewAt(root, 200, 160), { day: '20260810', minutes: 540 }); // 9:00
    // y=250 is 150px in = 2.5 hours -> 10:30.
    assert.deepEqual(weekViewAt(root, 200, 250), { day: '20260810', minutes: 630 });
    // Snapping: 53px in -> 8:53 raw -> rounds to 9:00 (540).
    assert.deepEqual(weekViewAt(root, 200, 153), { day: '20260810', minutes: 540 });
    // A later column.
    assert.deepEqual(weekViewAt(root, 350, 250), { day: '20260812', minutes: 630 });
});

test('weekViewAt returns null outside the time grid', () => {
    const { root } = buildWeekView();
    assert.equal(weekViewAt(root, 90, 250), null);   // left of the columns
    assert.equal(weekViewAt(root, 200, 50), null);   // above the grid (all-day strip / header)
    assert.equal(weekViewAt(root, 200, 750), null);  // below the grid
    assert.equal(weekViewAt(root, 900, 250), null);  // right of the columns
});

test('doubleClickDayAt resolves the month day under a double-click', () => {
    const { root, weeks } = buildScheduler();
    const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];
    assert.equal(doubleClickDayAt(root, dayCell, 125, 230), '20260801');
    assert.equal(doubleClickDayAt(root, dayCell, 300, 330), '20260812'); // second week

    // Header controls are ignored.
    const { todayBtn } = buildScheduler();
    assert.equal(doubleClickDayAt(root, todayBtn, 140, 35), null);

    // Double-clicks outside the grid resolve to nothing.
    assert.equal(doubleClickDayAt(root, dayCell, 125, 30), null);

    // Appointment presses are ignored: the click is for editing, not creating.
    const appointments = weeks[0].querySelector('.appointments');
    const item = makeElement({ dataset: { schedulerItem: '2' }, rect: { left: 120, top: 270, width: 150, height: 24 } });
    appointments._children.push(item);
    item.parentNode = appointments;
    assert.equal(doubleClickDayAt(root, item, 150, 280), null);
});

test('doubleClickDayAt resolves the week column and ignores timed items', () => {
    const { root, columns } = buildWeekView();
    assert.equal(doubleClickDayAt(root, columns[0], 200, 250), '20260810');

    // A timed item inside the column is ignored.
    const item = makeElement({ dataset: { schedulerItem: '0' }, rect: { left: 160, top: 200, width: 80, height: 40 } });
    columns[0]._children.push(item);
    item.parentNode = columns[0];
    assert.equal(doubleClickDayAt(root, item, 200, 220), null);

    // Outside the grid: nothing.
    assert.equal(doubleClickDayAt(root, columns[0], 200, 50), null);
});

test('a double-click on an empty day invokes DayDoubleClicked once', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];
        handler.dblClick({ target: dayCell, clientX: 125, clientY: 230 });
        assert.deepEqual(calls, [['DayDoubleClicked', '20260801']]);
        assert.deepEqual(captures, []); // never captures on a double-click
    } finally {
        delete globalThis.window;
    }
});

test('a double-click on an item or control never invokes DayDoubleClicked', () => {
    const { root, weeks, todayBtn } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const appointments = weeks[0].querySelector('.appointments');
        const item = makeElement({ dataset: { schedulerItem: '2' }, rect: { left: 120, top: 270, width: 150, height: 24 } });
        appointments._children.push(item);
        item.parentNode = appointments;
        handler.dblClick({ target: item, clientX: 150, clientY: 280 });
        handler.dblClick({ target: todayBtn, clientX: 140, clientY: 35 });
        assert.deepEqual(calls, []);
    } finally {
        delete globalThis.window;
    }
});

test('resolveDragStart starts a week drag from an empty column', () => {
    const { root, columns } = buildWeekView();
    assert.deepEqual(resolveDragStart(root, columns[0], 200, 250), { day: '20260810', minutes: 630, item: null, week: true });
});

test('resolveDragStart ignores timed items and overflow buttons in the week grid', () => {
    const { root, columns } = buildWeekView();
    const column = columns[0];

    const item = makeElement({ dataset: { schedulerItem: '0' }, rect: { left: 160, top: 200, width: 80, height: 40 } });
    column._children.push(item);
    item.parentNode = column;
    assert.equal(resolveDragStart(root, item, 200, 220), null);

    const overflow = makeElement({ tag: 'button', className: 'scheduler-overflow', rect: { left: 160, top: 640, width: 80, height: 20 } });
    column._children.push(overflow);
    overflow.parentNode = column;
    assert.equal(resolveDragStart(root, overflow, 200, 650), null);
});

test('pressing an empty week column starts a week drag without capturing', () => {
    const { root, columns } = buildWeekView();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        handler.pointerDown(press(columns[0], 200, 160));
        assert.deepEqual(calls, [['BeginWeekDrag', '20260810|540']]);
        assert.deepEqual(captures, []);
        assert.equal(handler.active, false);
        assert.deepEqual(handler.pending, { day: '20260810', minutes: 540, item: null, week: true });
    } finally {
        delete globalThis.window;
    }
});

test('movement in the week grid drags to snapped minutes only', () => {
    const { root, columns } = buildWeekView();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        handler.pointerDown(press(columns[0], 200, 160));
        // Sub-threshold jitter does not engage capture or drag.
        handler.pointerMove(move(202, 161));
        assert.deepEqual(captures, []);
        // Real vertical movement: 60px -> one hour -> 600 minutes.
        handler.pointerMove(move(200, 220));
        assert.deepEqual(captures, [1]);
        assert.deepEqual(calls, [['BeginWeekDrag', '20260810|540'], ['DragWeekTo', '600']]);
        // Horizontal drift into another column at the same snapped minute does nothing.
        handler.pointerMove(move(350, 220));
        assert.deepEqual(calls, [['BeginWeekDrag', '20260810|540'], ['DragWeekTo', '600']]);
        handler.pointerUp(release());
        assert.deepEqual(calls, [['BeginWeekDrag', '20260810|540'], ['DragWeekTo', '600'], ['CompleteDrag']]);
        assert.equal(handler.active, false);
        assert.equal(handler.pointerId, null);
    } finally {
        delete globalThis.window;
    }
});

test('a plain click on a week column completes without capture', () => {
    const { root, columns } = buildWeekView();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        handler.pointerDown(press(columns[0], 200, 160));
        handler.pointerUp(release());
        assert.deepEqual(captures, []);
        assert.deepEqual(calls, [['BeginWeekDrag', '20260810|540'], ['CompleteDrag']]);
    } finally {
        delete globalThis.window;
    }
});

// ---------------------------------------------------------------------------
// Handler wiring (capture timing, .NET invocations)
// ---------------------------------------------------------------------------

test('pressing a header button never starts a drag and never captures', () => {
    const { root, todayBtn } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        handler.pointerDown(press(todayBtn, 140, 35));
        assert.deepEqual(calls, []);
        assert.equal(handler.pending, null);
        assert.equal(handler.active, false);
        assert.deepEqual(captures, []);
    } finally {
        delete globalThis.window;
    }
});

test('pressing the view switcher never starts a drag', () => {
    const { root, viewSelect } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        handler.pointerDown(press(viewSelect, 130, 85));
        assert.deepEqual(calls, []);
        assert.deepEqual(captures, []);
    } finally {
        delete globalThis.window;
    }
});

test('pressing a day starts a day drag without capturing', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[2];
        handler.pointerDown(press(dayCell, 225, 230));
        assert.deepEqual(calls, [['BeginDayDrag', '20260803']]);
        assert.deepEqual(captures, []); // capture only engages on movement
        assert.equal(handler.active, false);
        assert.deepEqual(handler.pending, { day: '20260803', item: null });
    } finally {
        delete globalThis.window;
    }
});

test('movement beyond the threshold engages capture and drags across days', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];
        handler.pointerDown(press(dayCell, 125, 230));
        // Sub-threshold jitter must not engage capture or start dragging.
        handler.pointerMove(move(127, 231));
        assert.deepEqual(captures, []);
        assert.deepEqual(calls, [['BeginDayDrag', '20260801']]);
        // Real movement into another day.
        handler.pointerMove(move(300, 230));
        assert.deepEqual(captures, [1]);
        assert.deepEqual(calls, [['BeginDayDrag', '20260801'], ['DragTo', '20260805']]);
        assert.equal(handler.active, true);
    } finally {
        delete globalThis.window;
    }
});

test('a plain click on a day completes without capture (click reaches the target)', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];
        handler.pointerDown(press(dayCell, 125, 230));
        handler.pointerUp(release());
        assert.deepEqual(captures, []); // never captured -> browser click lands on the day
        assert.deepEqual(calls, [['BeginDayDrag', '20260801'], ['CompleteDrag']]);
    } finally {
        delete globalThis.window;
    }
});

test('a plain click on an item completes without capture so the click reaches the item', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const appointments = weeks[0].querySelector('.appointments');
        const item = makeElement({ dataset: { schedulerItem: '3' }, rect: { left: 120, top: 270, width: 150, height: 24 } });
        appointments._children.push(item);
        item.parentNode = appointments;
        handler.pointerDown(press(item, 150, 280));
        handler.pointerUp(release());
        assert.deepEqual(captures, []);
        assert.deepEqual(calls, [['BeginItemDrag', 3, '20260802'], ['CompleteDrag']]);
    } finally {
        delete globalThis.window;
    }
});

test('pressing an overflow button inside the grid does not start a drag', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const appointments = weeks[0].querySelector('.appointments');
        const overflow = makeElement({ tag: 'button', className: 'appointment scheduler-overflow', rect: { left: 120, top: 270, width: 150, height: 24 } });
        appointments._children.push(overflow);
        overflow.parentNode = appointments;
        handler.pointerDown(press(overflow, 150, 280));
        assert.deepEqual(calls, []);
        assert.equal(handler.pending, null);
        assert.deepEqual(captures, []);
    } finally {
        delete globalThis.window;
    }
});

test('a dragged gesture completes with CaptureDrag and releases state', () => {
    const { root, weeks } = buildScheduler();
    const captures = [];
    const calls = [];
    globalThis.window = fakeWindow();
    try {
        const handler = new SchedulerPointerHandler(attachEvents(root, captures), fakeDotnet(calls));
        const dayCell = weeks[0].querySelectorAll('[data-scheduler-day]')[0];
        handler.pointerDown(press(dayCell, 125, 230));
        handler.pointerMove(move(300, 230)); // engage capture + DragTo
        handler.pointerMove(move(300, 330)); // second week
        handler.pointerUp(release());
        assert.deepEqual(calls, [
            ['BeginDayDrag', '20260801'],
            ['DragTo', '20260805'],
            ['DragTo', '20260812'],
            ['CompleteDrag'],
        ]);
        assert.equal(handler.active, false);
        assert.equal(handler.pointerId, null);
    } finally {
        delete globalThis.window;
    }
});
