document.addEventListener('mousemove', function(e) {
    document.documentElement.style.setProperty('--cursor-x', e.clientX + 'px');
    document.documentElement.style.setProperty('--cursor-y', e.clientY + 'px');
});

window.copyToClipboard = async (text) => {
    if (navigator.clipboard) {
        await navigator.clipboard.writeText(text);
    } else {
        const el = document.createElement('textarea');
        el.value = text;
        document.body.appendChild(el);
        el.select();
        document.execCommand('copy');
        document.body.removeChild(el);
    }
};

window.wanderEditor = {
    insertText: function(element, before, after, placeholder) {
        const start = element.selectionStart;
        const end = element.selectionEnd;
        const selected = element.value.substring(start, end) || placeholder;
        const newValue = element.value.substring(0, start) + before + selected + after + element.value.substring(end);
        element.value = newValue;
        element.selectionStart = start + before.length;
        element.selectionEnd = start + before.length + selected.length;
        element.dispatchEvent(new Event('input', { bubbles: true }));
        element.focus();
    },
    getValue: function(element) {
        return element.value;
    }
};

window.getElementRect = (element) => {
    const rect = element.getBoundingClientRect();
    return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
};

const _enterSubmitAttached = new WeakSet();

window.attachEnterSubmit = (wrapperId, dotNetRef, methodName) => {
    const wrapper = document.getElementById(wrapperId);
    if (!wrapper) return;
    const textarea = wrapper.querySelector('textarea');
    if (!textarea || _enterSubmitAttached.has(textarea)) return;
    _enterSubmitAttached.add(textarea);
    textarea.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync(methodName);
        }
    });
};
