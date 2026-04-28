// for details on configuring this project to bundle and minify static web assets.
$(function () {
    function toggleConditionalField() {
        // Hide all conditional fields first
        $(".conditional-field").hide();

        // Get selected review body type
        const selectedReviewBodyType = $("#ReviewBodyType").val()?.toLowerCase();

        $(".conditional-field").each(function () {
            const $field = $(this);
            const conditionAttr = $field.data("condition");

            if (!conditionAttr) return;

            const shouldShow = conditionAttr.toLowerCase() === selectedReviewBodyType;

            $field.toggle(shouldShow);

            if (!shouldShow) {
                $field.find("input").val("");
            }
        });
    }

    // Run on page load
    toggleConditionalField();

    // Run on change
    $("#ReviewBodyType").on("change", toggleConditionalField);
});