function initAutocomplete(autoCompleteInputId, inputIdForSubmission, defaultValue, apiUrl, containerId) {
    const beforeSuggestionsText = 'Suggestions';
    const afterSuggestionsText = 'Continue entering to improve suggestions';
    const noResultsText = 'No suggestions found.';
    let resultsFound = false;
    let requestToken = 0;

    accessibleAutocomplete({
        element: document.getElementById(containerId),
        id: autoCompleteInputId,
        defaultValue: defaultValue,
        minLength: 1,
        autoselect: false,
        displayMenu: 'overlay',
        inputClasses: 'govuk-input govuk-!-width-three-quarters',
        menuClasses: 'govuk-!-width-three-quarters',
        confirmOnBlur: false,

        source: function (query, populateResults) {
            requestToken++;
            const currentToken = requestToken;

            if (query.length < 3) {
                populateResults([]);
                $('.autocomplete__menu').attr('data-before-suggestions', '');
                $('.autocomplete__menu').attr('data-after-suggestions', afterSuggestionsText);
                return;
            }

            $.ajax({
                url: apiUrl,
                method: 'GET',
                data: { name: query },
                dataType: 'json',
                success: function (data) {
                    if (currentToken !== requestToken) return;
                    if (!data || data.length === 0) {
                        populateResults([]);
                        return;
                    }

                    resultsFound = true;
                    $('.autocomplete__menu').attr('data-before-suggestions', beforeSuggestionsText);
                    $('.autocomplete__menu').attr('data-after-suggestions', afterSuggestionsText);
                    populateResults(data);
                    $(`#${inputIdForSubmission}`).val('');
                },
                error: function () {
                    if (currentToken !== requestToken) return;
                    console.error('Error fetching suggestions from', apiUrl);
                    populateResults([]);
                }
            });
        },

        onConfirm: function (suggestion) {
            $(`#${inputIdForSubmission}`).val(suggestion || '');
        },

        templates: {
            suggestion: function (suggestion) {
                const $input = $(`#${autoCompleteInputId}`);
                const query = $input.length ? $input.val() : '';

                if (!query || !resultsFound) {
                    return suggestion;
                }

                const escapedQuery = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                const regex = new RegExp(`(${escapedQuery})`, 'gi');
                return suggestion.replace(regex, '<strong>$1</strong>');
            }
        },

        tNoResults: function () {
            const $input = $(`#${autoCompleteInputId}`);
            const query = $input.length ? $input.val() : '';

            if (!query || query.length < 3) return '';

            $(`#${inputIdForSubmission}`).val('');
            return noResultsText;
        }
    });

    // Hide fallback input/label just in case
    $(`#${inputIdForSubmission}`).addClass('js-hidden');
    $(`label[for="${inputIdForSubmission}"]`).hide();

    // Clear hidden field if input is cleared
    $(`#${autoCompleteInputId}`).on('input', function () {
        if (!this.value) {
            $(`#${inputIdForSubmission}`).val('');
            $('.autocomplete__menu').html('');
            resultsFound = false;
        }
    });
}
