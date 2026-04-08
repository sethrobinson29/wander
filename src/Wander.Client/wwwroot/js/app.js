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
