// Pokes the rendered case like a user would: moves range inputs, types into text inputs, and clicks controls.
// These are synthetic DOM events, enough to run the components' .NET event handlers.
window.smoke = {
    async poke(containerId) {
        const pause = ms => new Promise(resolve => setTimeout(resolve, ms));
        const root = document.getElementById(containerId);
        if (!root) return 'no-root';
        const done = [];
        const setValue = (input, value) => {
            Object.getOwnPropertyDescriptor(Object.getPrototypeOf(input), 'value').set.call(input, value);
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        };
        for (const input of root.querySelectorAll('input[type="range"]')) {
            const min = Number(input.min || 0);
            const max = Number(input.max || 100);
            setValue(input, String(Math.round((min + max) / 2)));
            done.push('range');
            await pause(100);
        }
        for (const input of root.querySelectorAll('input:not([type]), input[type="text"], input[type="number"], input[type="search"], textarea')) {
            if (input.readOnly || input.disabled) continue;
            input.dispatchEvent(new FocusEvent('focus'));
            input.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
            setValue(input, '2');
            input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
            input.dispatchEvent(new KeyboardEvent('keyup', { key: 'ArrowUp', bubbles: true }));
            input.dispatchEvent(new FocusEvent('blur'));
            input.dispatchEvent(new FocusEvent('focusout', { bubbles: true }));
            done.push('text');
            await pause(100);
        }
        const selector = 'button, [role="button"], [role="switch"], [role="checkbox"], [role="radio"], [role="tab"], [role="option"], [role="treeitem"], [role="menuitem"], [role="slider"], input[type="checkbox"], input[type="radio"], .mud-input-control, .mud-chip, .mud-list-item, .mud-table-sort-label, .mud-rating-item, .mud-expand-panel-header, .mud-chart-serie, path, circle';
        let clicks = 0;
        for (const element of root.querySelectorAll(selector)) {
            if (clicks++ >= 10) break;
            for (const type of ['pointerdown', 'mousedown', 'pointerup', 'mouseup']) {
                element.dispatchEvent(new MouseEvent(type, { bubbles: true }));
            }
            element.dispatchEvent(new MouseEvent('click', { bubbles: true }));
            done.push('click');
            await pause(150);
        }
        // Popovers render in the provider, outside the case's container.
        const item = document.querySelector('.mud-popover-open .mud-list-item, .mud-popover-open [role="option"], .mud-popover-open [role="menuitem"], .mud-popover-open .mud-picker-calendar-day, .mud-popover-open button');
        if (item) {
            item.dispatchEvent(new MouseEvent('click', { bubbles: true }));
            done.push('popover');
        }
        await pause(300);
        return done.join(',') || 'nothing';
    }
};
