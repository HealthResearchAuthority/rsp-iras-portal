﻿// checkbox-counter.js
function initCheckboxCount(groupName, hintId) {
    const checkboxes = document.querySelectorAll(`input[name="${groupName}"]`);
    const hint = document.getElementById(hintId);
    if (!hint || !checkboxes.length) return;

    function updateHint() {
        const count = Array.from(checkboxes).filter(cb => cb.checked).length;
        hint.textContent = `${count} selected`;
    }

    checkboxes.forEach(cb => cb.addEventListener("change", updateHint));
    updateHint(); // Initial update on page load
}