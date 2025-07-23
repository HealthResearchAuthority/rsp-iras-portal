/**
 * Initializes the accessible autocomplete functionality for a given input field.
 *
 * @param {string} autoCompleteInputId - The ID of the input field for autocomplete.
 * @param {string} inputIdForSubmission - The ID of the hidden input field to store the selected value.
 * @param {string} defaultValue - The default value to prefill in the autocomplete input.
 * @param {string} apiUrl - The API endpoint to fetch autocomplete suggestions.
 * @param {string} containerId - The ID of the container to render the autocomplete component into.
 */
function initAutocomplete(autoCompleteInputId, inputIdForSubmission, defaultValue, apiUrl, containerId, autoCompleteEnabledId) {
    const beforeSuggestionsText = 'Suggestions'; // Message displayed before the suggestions.
    const afterSuggestionsText = 'Continue entering to improve suggestions'; // Message displayed after the suggestions.
    const noResultsText = 'No suggestions found.'; // Message displayed when no suggestions are found.
    let resultsFound = false; // Flag to indicate if results were found.
    let requestToken = 0; // A counter to identify the most recent AJAX request

    accessibleAutocomplete({
        element: document.getElementById(containerId), // The container element for the autocomplete.
        id: autoCompleteInputId, // The ID for the autocomplete input field.
        minLength: 1, // Minimum number of characters required to trigger suggestions.
        autoselect: false, // Prevents automatic selection of the first suggestion.
        displayMenu: 'overlay', // Displays the suggestion menu as an overlay.
        inputClasses: 'govuk-input govuk-!-width-three-quarters', // CSS classes for styling the input field.
        menuClasses: 'govuk-!-width-three-quarters', // CSS classes for styling the suggestion menu.
        defaultValue: defaultValue, // Sets the default value for the input field.
        confirmOnBlur: false, // Prevents confirmation of a suggestion when the input loses focus.

        source: function (query, populateResults) {
            requestToken++; // Increment the token for each new request
            const currentToken = requestToken; // Capture this request's token

            // If fewer than 3 characters, hide menu and show guidance
            if (query.length < 3) {
                populateResults([]); // No suggestions to populate
                $(".autocomplete__menu").attr('data-before-suggestions', ''); // Clear message before suggestions.
                $(".autocomplete__menu").attr('data-after-suggestions', afterSuggestionsText); // Show message after suggestions.
                return;
            }

            // Fetch suggestions from the API based on the user's query.
            $.ajax({
                url: apiUrl, // The API endpoint for fetching suggestions.
                method: 'GET',
                data: { name: query }, // Query parameter sent to the API.
                dataType: 'json',
                success: function (data) {
                    // Ignore this response if a newer request has been made
                    if (currentToken !== requestToken) {
                        return;
                    }

                    // No results to populate
                    const currentValue = $(`#${autoCompleteInputId}`).val();

                    if (!currentValue || currentValue.length < 3) {
                        return;
                    }
                    if (!data || data.length === 0) {
                        populateResults([]); // No results to show
                        return;
                    }

                    resultsFound = true; // Set flag to indicate that results were found.
                    $(".autocomplete__menu").attr('data-before-suggestions', beforeSuggestionsText); // Show message before suggestions.
                    populateResults(data); // Populate the suggestion list with the API response.
                    $(".autocomplete__menu").attr('data-after-suggestions', afterSuggestionsText); // Show message after suggestions.
                    $(`#${inputIdForSubmission}`).attr('value', ''); // Clear the hidden input; only populated when a suggestion is selected.
                },
                error: function (xhr, status, error) {
                    // Ignore if this is a stale request
                    if (currentToken !== requestToken) {
                        return;
                    }

                    console.error(`Error fetching suggestions from ${apiUrl}`, error); // Log errors to the console.
                    populateResults([]); // Clear the suggestion list on error.
                }
            });
        },
        templates: {
            // Customize the display of suggestions in the dropdown menu.
            suggestion: function (suggestion) {
                if (resultsFound) {
                    const query = $(`#${autoCompleteInputId}`).val(); // Get the current input value.
                    const escapedQuery = RegExp.escape(query); // Escape special characters in the query string.
                    let regex = new RegExp('(' + escapedQuery + ')', 'gi'); // Highlight matching text in suggestions.
                    return suggestion.replace(regex, '<strong>$1</strong>'); // Wrap matches in <strong> tags.
                }

                $(".autocomplete__menu").attr('data-before-suggestions', ''); // Clear message before suggestions.
                $(".autocomplete__menu").attr('data-after-suggestions', ''); // Clear message after suggestions.
                return ""; // Return an empty string if no results were found.
            }
        },
        onConfirm: function (suggestion) {
            // Handle the selection of a suggestion.
            if (suggestion == undefined) {
                $(`#${inputIdForSubmission}`).attr('value', ''); // Clear the hidden input if no suggestion is selected.
                return;
            }

            $(`#${inputIdForSubmission}`).attr('value', suggestion); // Set the hidden input value to the selected suggestion.
        },
        tNoResults: function () {
            // Message displayed when no suggestions are found.
            const query = $(`#${autoCompleteInputId}`).val(); // Get the current input value.
            if (query.length < 3) { // If the input is less than 3 characters, prompt user to type more characters.
                $(".autocomplete__menu").attr('data-before-suggestions', afterSuggestionsText); // Show message before suggestions.
                $(".autocomplete__menu").attr('data-after-suggestions', ''); // Clear message after suggestions.
                return;
            }

            // If no results are found, clear the suggestion list and show no suggestions found message.
            $(".autocomplete__menu").attr('data-before-suggestions', ''); // Clear message before suggestions.
            $(".autocomplete__menu").attr('data-after-suggestions', ''); // Clear message after suggestions.
            $(`#${inputIdForSubmission}`).attr('value', ''); // Clear the hidden input value.
            return noResultsText; // Return the message for no results.
        }
    });

    // If JavaScript is enabled, show the input and its label
    $(`label[for="${autoCompleteInputId}"]`).show();

    // Set the hidden input to indicate that autocomplete is enabled.)
    $(`#${autoCompleteEnabledId}`).attr('value', 'true');

    $(`#${autoCompleteInputId}`).on('input', function () {
        if (!this.value) {
            $(`#${inputIdForSubmission}`).attr('value', ''); // Clears the hidden input value.
            $(".autocomplete__menu").html(''); // Clear the suggestion list.)
            resultsFound = false; // Reset the resultsFound flag.
        }
    });
}