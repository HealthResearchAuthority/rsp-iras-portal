// checkbox-counter.js
(function (root, factory) {
    if (typeof module === "object" && module.exports) {
        module.exports = factory();
    } else {
        root.initCheckboxCount = factory();
    }
})(typeof self !== "undefined" ? self : this, function () {

    /**
     * Initialize a checkbox counter for a group.
     * @param {string} groupName - Simple name (e.g., "Roles") OR complex prefix (e.g., "Search.ReviewBodies")
     * @param {string} hintId - ID of the hint element to update
     */
    function initCheckboxCount(groupName, hintId) {
        let hint = document.getElementById(hintId);
        if (!hint) return;

        let property = "IsSelected"; // fixed; handles MVC complex lists

        // Matches either:
        //  - Simple:  input[name="Roles"]
        //  - Complex: input[name^="Search.ReviewBodies["][name$=".IsSelected"]
        let simpleSel = 'input[type="checkbox"][name="' + groupName + '"]';
        let complexSel = 'input[type="checkbox"][name^="' + groupName + '["][name$=".' + property + '"]';
        let allSel = simpleSel + ", " + complexSel;

        function getCheckboxes() {
            let nodes = document.querySelectorAll(allSel);
            return Array.from(new Set(Array.from(nodes)));
        }

        let boxes = getCheckboxes();
        if (!boxes.length) return;

        function updateHint() {
            let count = getCheckboxes().filter(function (cb) { return cb.checked; }).length;
            hint.textContent = count + " selected";
        }

        boxes.forEach(function (cb) { cb.addEventListener("change", updateHint); });
        updateHint();
    }

    return initCheckboxCount;
});