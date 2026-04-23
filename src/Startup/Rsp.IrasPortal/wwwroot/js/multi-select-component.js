(function (window, $) {
    function initSelectSearch(containerSelector) {
        const $container = $(containerSelector);
        if (!$container.length) return;

        const $select = $container.find("select");
        const $enhanced = $container.find(".select-search__enhanced");
        const $input = $container.find(".select-search__input");
        const $dropdown = $container.find(".select-search__dropdown");
        const $tags = $container.find(".select-search__tags");
        const $modelPropertyName = $container.data("model-property-name");

        // Build data from <select>
        const data = $select.find("option").map(function () {
            return {
                value: this.value,
                text: $(this).text()
            };
        }).get();

        // 🔥 IMPORTANT: initialise selected from DOM
        let selected = $select.find("option:selected").map(function () {
            return {
                value: this.value,
                text: $(this).text()
            };
        }).get();

        //syncHiddenInputs();

        // Enable enhanced mode
        $select.addClass("enhanced");
        $enhanced.removeAttr("hidden");

        function syncSelect() {
            $select.val(selected.map(x => x.value));
        }

        function syncHiddenInputs() {
            const $hiddenContainer = $container.find(".select-search__hidden");

            // Only build once
            if ($hiddenContainer.children().length === 0) {
                data.forEach((item, index) => {
                    const isSelected = selected.some(s => s.value === item.value);

                    $hiddenContainer.append(`
                <input type="hidden" name="${$modelPropertyName}[${index}].Id" value="${item.value}" />
                <input type="hidden"
                       name="${$modelPropertyName}[${index}].IsSelected"
                       value="${isSelected}"
                       data-id="${item.value}" />
            `);
                });

                return;
            }

            // Update only IsSelected values
            $hiddenContainer.find("input[name$='.IsSelected']").each(function () {
                const id = $(this).data("id");
                const isSelected = selected.some(s => s.value === id);

                $(this).val(isSelected.toString());
            });
        }

        function renderTags() {
            $tags.empty();

            selected.forEach(item => {
                const $tag = $(`
                    <div class="select-search__tag">
                        ${item.text}
                        <button type="button">&times;</button>
                    </div>
                `);

                $tag.find("button").on("click", function () {
                    selected = selected.filter(x => x.value !== item.value);
                    syncSelect();
                    syncHiddenInputs();
                    renderTags();
                });

                $tags.append($tag);
            });
        }

        function renderList(filter = "") {
            const filtered = data.filter(item =>
                item.text.toLowerCase().includes(filter.toLowerCase()) &&
                !selected.some(s => s.value === item.value)
            );

            $dropdown.empty();

            if (!filtered.length) {
                $dropdown.append('<li class="select-search__no-results">No results</li>');
            } else {
                filtered.forEach(item => {
                    const $item = $(`
                        <li class="select-search__item" data-value="${item.value}">
                            ${item.text}
                        </li>
                    `);

                    $item.on("mousedown", function () {
                        selectItem(item);
                    });

                    $dropdown.append($item);
                });
            }

            $dropdown.show();
        }

        function selectItem(item) {
            selected.push(item);
            syncSelect();
            syncHiddenInputs();
            renderTags();
            $input.val("");
            $dropdown.hide();
        }

        $input.on("input", function () {
            renderList($(this).val());
        });

        $input.on("focus", function () {
            renderList($(this).val());
        });

        $input.on("keydown", function (e) {
            if (e.key === "Backspace" && !$input.val()) {
                selected.pop();
                syncSelect();
                syncHiddenInputs();
                renderTags();
            }
        });

        $(document).on("click", function (e) {
            if (!$(e.target).closest(containerSelector).length) {
                $dropdown.hide();
            }
        });

        // Initial render
        renderTags();
    }

    window.initSelectSearch = initSelectSearch;
})(window, jQuery);