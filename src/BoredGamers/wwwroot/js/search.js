$(document).ready(function () {
        // 'games' is default for search
        let searchMode = 'games'; 
        let searchTimeout = null;
        const isAuthenticated = $('#gameSearchForm').data('authenticated');

        // --- Toggle between game and user search ---
        $('#searchTypeMenu .dropdown-item').on('click', function (e) {
            e.preventDefault();
            searchMode = $(this).data('type');

            // If switching to user search, verify they're logged in first
            if ($(this).data('type') === 'users' && !isAuthenticated) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            // Update button label
            $('#searchTypeBtn').html($(this).html());

            // Update active state
            $('#searchTypeMenu .dropdown-item').removeClass('active');
            $(this).addClass('active');

            // Update placeholder and form action
            if (searchMode === 'users') {
                $('#searchInput').attr('placeholder', 'Search users...');
                $('#gameSearchForm').attr('action', '#'); // prevent form submit for user search
                $('#searchBtn').hide(); // hide submit button
            } else {
                $('#searchInput').attr('placeholder', 'Search games...');
                $('#gameSearchForm').attr('action', '/Games/SearchResults');
                $('#searchBtn').show();
                clearUserResults();
            }

            $('#searchInput').val('').focus();
        });

        // --- Live user search (activates after 2 chars) ---
        $('#searchInput').on('input', function () {
            if (searchMode !== 'users') return;

            const q = $(this).val().trim();
            clearTimeout(searchTimeout);
            
            if (q.length < 2) {
                clearUserResults();
                return;
            }

            // Small debounce to not fire on every keystroke
            searchTimeout = setTimeout(function () {
                fetchUsers(q);
            }, 250);
        });

        // --- Prevent game form submit in user mode ---
        $('#gameSearchForm').on('submit', function (e) {
            if (searchMode === 'users') e.preventDefault();
        });

        // --- Hide results when clicking outside ---
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#gameSearchForm, #userSearchResults').length) {
                clearUserResults();
            }
        });

        function fetchUsers(q) {
            $.getJSON('/UserSearch/Search', { q: q }, function (data) {
                renderUserResults(data, q);
            }).fail(function () {
                // If request fails (unauthenticated), redirect to login
                window.location.href = '/Account/Login';
            });
        }
        
        // Builds and displays the search results dropdown from the returned user list
        function renderUserResults(users, q) {
            const $container = $('#userSearchResults');
            $container.empty();
            
            // Show a "no results" message if the server returned an empty list
            if (users.length === 0) {
                $container.append(
                    '<span class="dropdown-item text-muted">No users found</span>'
                );
            } else {
                // Loop through each returned user and build a dropdown item for them
                users.forEach(function (user) {
                    const avatar = user.avatarUrl
                        ? `<img src="${user.avatarUrl}" class="rounded-circle me-2" width="32" height="32" style="object-fit:cover;">`
                        : `<div class="rounded-circle bg-secondary text-white d-inline-flex align-items-center justify-content-center me-2" style="width:32px;height:32px;font-size:1rem;">${user.username.charAt(0).toUpperCase()}</div>`;


                    const $item = $(`
                        <a class="dropdown-item d-flex align-items-center py-2" href="/Profile/${encodeURIComponent(user.username)}">
                            ${avatar}
                            <span>${escapeHtml(user.username)}</span>
                        </a>
                    `);

                    $container.append($item);
                });
            }

            $container.show();
        }

        // Hides and empties the results dropdown
        function clearUserResults() {
            $('#userSearchResults').hide().empty();
        }

        // Basic XSS guard for rendered usernames
        function escapeHtml(str) {
            return $('<div>').text(str).html();
        }
    });