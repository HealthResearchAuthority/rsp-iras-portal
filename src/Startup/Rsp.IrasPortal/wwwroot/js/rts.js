/// <reference path="../lib/jquery/dist/jquery.js" />
/// <reference path="../assets/js/accessible-autocomplete.min.js" />

/**
 * Initializes the accessible autocomplete functionality for a given input field.
 *
 * @param {string} autoCompleteInputId - The ID of the input field for autocomplete.
 * @param {string} inputIdForSubmission - The ID of the hidden input field to store the selected value.
 * @param {string} defaultValue - The default value to prefill in the autocomplete input.
 * @param {string} apiUrl - The API endpoint to fetch autocomplete suggestions.
 */
function initAutocomplete(autoCompleteInputId, inputIdForSubmission, defaultValue, apiUrl) {
    accessibleAutocomplete({
        element: document.querySelector('#autocomplete-container'), // The container element for the autocomplete.
        id: autoCompleteInputId, // The ID for the autocomplete input field.
        minLength: 3, // Minimum number of characters required to trigger suggestions.
        autoselect: false, // Prevents automatic selection of the first suggestion.
        displayMenu: 'overlay', // Displays the suggestion menu as an overlay.
        inputClasses: 'govuk-input govuk-!-width-three-quarters', // CSS classes for styling the input field.
        menuClasses: 'govuk-!-width-three-quarters', // CSS classes for styling the suggestion menu.
        defaultValue: defaultValue, // Sets the default value for the input field.
        confirmOnBlur: false, // Prevents confirmation of a suggestion when the input loses focus.
        source: function (query, populateResults) {
            // Fetches suggestions from the API based on the user's query.
            $.ajax({
                url: apiUrl, // The API endpoint for fetching suggestions.
                method: 'GET', // HTTP method for the request.
                data: { name: query }, // Query parameter sent to the API.
                dataType: 'json', // Expected response format.
                success: function (data) {
                    populateResults(data); // Populates the suggestion list with the API response.
                },
                error: function (xhr, status, error) {
                    console.error('Error fetching suggestions:', error); // Logs errors to the console.
                    populateResults([]); // Clears the suggestion list on error.
                }
            });
        },
        templates: {
            // Customizes the display of suggestions in the dropdown menu.
            suggestion: function (suggestion) {
                const query = $(`#${autoCompleteInputId}`).val(); // Gets the current input value.
                let regex = new RegExp('(' + query + ')', 'gi'); // Highlights matching text in suggestions while retaining the case.
                return suggestion.replace(regex, '<strong>$1</strong>'); // Wraps matches in <strong> tags.
            }
        },
        onConfirm: function (suggestion) {
            // Handles the selection of a suggestion.
            if (suggestion == undefined) {
                $(`#${inputIdForSubmission}`).attr('value', ''); // Clears the hidden input if no suggestion is selected.
                return;
            }

            $(`#${inputIdForSubmission}`).attr('value', suggestion); // Sets the hidden input value to the selected suggestion.
        },
        tNoResults: function () {
            // Message displayed when no suggestions are found.
            return 'No suggestions found. You can enter your own answer';
        }
    });
}