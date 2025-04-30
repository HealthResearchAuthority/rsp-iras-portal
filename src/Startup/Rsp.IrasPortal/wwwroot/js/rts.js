/// <reference path="../lib/jquery/dist/jquery.js" />

function initAutocomplete(elementId, inputName, apiUrl) {
    accessibleAutocomplete({
        element: document.querySelector('.autocomplete-wrapper'),
        id: elementId,
        name: inputName,
        minLength: 3, // Start suggesting immediately
        source: [
            'United Kingdom',
            'United States',
            'Canada',
            'Australia',
            'India',
            'Germany',
            'France'
        ],
        templates: {
            suggestion: function (suggestion) {
                return suggestion;
            }
        }
    });
}

//function initAutocomplete(elementId, inputName, apiUrl) {
//    accessibleAutocomplete({
//        element: document.querySelector(`#${elementId}-wrapper`),
//        id: elementId,
//        name: inputName,
//        minLength: 3,
//        source: function (query, populateResults) {
//            if (query.length < 3) {
//                populateResults([]);
//                return;
//            }

//            $.ajax({
//                url: apiUrl,
//                method: 'GET',
//                data: { query: query },
//                dataType: 'json',
//                success: function (data) {
//                    populateResults(data);
//                },
//                error: function (xhr, status, error) {
//                    console.error('Error fetching suggestions:', error);
//                    populateResults([]);
//                }
//            });
//        },
//        templates: {
//            suggestion: function (suggestion) {
//                return suggestion;
//            }
//        }
//    });
//}