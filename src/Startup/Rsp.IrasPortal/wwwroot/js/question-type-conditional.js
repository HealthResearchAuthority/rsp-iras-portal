document.addEventListener('DOMContentLoaded', function () {
    const wrappers = document.querySelectorAll('[data-conditional-target="question-type"]');

    const optionMap = {
        "look-up list": [
            { value: "checkbox", label: "Checkbox" },
            { value: "radio button", label: "Radio button" }
        ],
        "text": [
            { value: "text", label: "Text" },
            { value: "email", label: "Email" }
        ],
        "boolean": [],
        "date": [],
        "rts:org_lookup": []
    };

    wrappers.forEach(wrapper => {
        const select = wrapper.querySelector('select');
        if (!select) return;

        const wrapperName = wrapper.getAttribute('name');
        const indexMatch = wrapperName?.match(/\[(\d+)\]/);
        const index = indexMatch ? indexMatch[1] : "0";

        const dataTypeWrapper = document.querySelector(`.conditional-datatype[data-index="${index}"]`);
        const dataTypeSelect = dataTypeWrapper?.querySelector('select');

        function updateDataTypeVisibility() {
            const selectedQuestionType = (select.value || "").toLowerCase();

            if (!optionMap[selectedQuestionType] || optionMap[selectedQuestionType].length === 0) {
                dataTypeWrapper.style.display = "none";
                if (dataTypeSelect) dataTypeSelect.innerHTML = "";
                return;
            }

            if (dataTypeSelect) {
                const currentValue = dataTypeSelect.value; // ← preserve current value

                dataTypeWrapper.style.display = "block";

                const defaultOption = `<option value="">Please select a data type</option>`;
                const optionHtml = optionMap[selectedQuestionType]
                    .map(opt => `<option value="${opt.value}">${opt.label}</option>`)
                    .join("\n");

                dataTypeSelect.innerHTML = defaultOption + optionHtml;

                // Re-assign the original value if it still exists
                if (currentValue) {
                    dataTypeSelect.value = currentValue;
                }
            }
        }

        updateDataTypeVisibility(); // Init on load
        select.addEventListener('change', updateDataTypeVisibility);
    });
});
