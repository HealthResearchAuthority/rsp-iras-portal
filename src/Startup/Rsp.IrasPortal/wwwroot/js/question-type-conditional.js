document.addEventListener("DOMContentLoaded", function () {
    const questionTypeSelect = document.querySelector('[data-conditional-target="question-type"]');
    const dataTypeContainer = document.querySelector(".conditional-datatype");

    function updateDataTypeVisibility() {
        if (!questionTypeSelect || !dataTypeContainer) return;

        const selectedValue = questionTypeSelect.value.toLowerCase();

        const showDataTypeFor = {
            "look-up list": ["checkbox", "radio button"],
            "text": ["text", "email"]
        };

        if (showDataTypeFor[selectedValue]) {
            dataTypeContainer.style.display = "block";

            const dataTypeSelect = dataTypeContainer.querySelector("select");
            [...dataTypeSelect.options].forEach(opt => {
                opt.style.display = showDataTypeFor[selectedValue].includes(opt.value.toLowerCase())
                    ? "block" : "none";
            });

            // Reset value if current selection is not valid
            if (!showDataTypeFor[selectedValue].includes(dataTypeSelect.value.toLowerCase())) {
                dataTypeSelect.value = "";
            }
        } else {
            dataTypeContainer.style.display = "none";
        }
    }

    if (questionTypeSelect) {
        questionTypeSelect.addEventListener("change", updateDataTypeVisibility);
        updateDataTypeVisibility(); // Initial load
    }
});
