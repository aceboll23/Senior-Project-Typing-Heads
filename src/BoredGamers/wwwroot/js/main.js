/* ============================================================================
   BoredGamers - Main JavaScript (jQuery 4)
   Handles modal switching, demo login simulation, and basic interactivity
   ============================================================================ */

$(document).ready(function() {
    initModals();
    initDemoLogin();
    initNavbarScroll();
    initSearchResults();
});

/* ============================================================================
   Modal Switching
   Toggle between login and register modals
   ============================================================================ */

function initModals() {
    // Switch from Login modal to Register modal
    $('#showRegisterLink').on('click', function(e) {
        e.preventDefault();
        var loginModal = bootstrap.Modal.getInstance($('#loginModal')[0]);
        if (loginModal) {
            loginModal.hide();
        }
        var registerModal = new bootstrap.Modal($('#registerModal')[0]);
        registerModal.show();
    });

    // Switch from Register modal to Login modal
    $('#showLoginLink').on('click', function(e) {
        e.preventDefault();
        var registerModal = bootstrap.Modal.getInstance($('#registerModal')[0]);
        if (registerModal) {
            registerModal.hide();
        }
        var loginModal = new bootstrap.Modal($('#loginModal')[0]);
        loginModal.show();
    });
}

/* ============================================================================
   Demo Login Simulation
   For testing the logged-in UI state (visual only, no real auth)
   ============================================================================ */

function initDemoLogin() {
    // Quick demo login button - simulates logged-in state visually
    $('#quickLoginBtn').on('click', function() {
        // Store demo login state in localStorage
        localStorage.setItem('boredgamers-demo-logged-in', 'true');

        // Close the login modal
        var loginModal = bootstrap.Modal.getInstance($('#loginModal')[0]);
        if (loginModal) {
            loginModal.hide();
        }

        // Show a message (since we can't actually change server-side auth state)
        alert('Demo mode: In a real app, you would now be logged in. Refresh the page after implementing ASP.NET Identity to see the logged-in UI.');
    });

    // Login form submit (for demo purposes)
    $('#loginForm').on('submit', function(e) {
        e.preventDefault();
        localStorage.setItem('boredgamers-demo-logged-in', 'true');

        var loginModal = bootstrap.Modal.getInstance($('#loginModal')[0]);
        if (loginModal) {
            loginModal.hide();
        }

        alert('Demo mode: Form submitted. Implement ASP.NET Identity for real authentication.');
    });

    // Register form submit (for demo purposes)
    $('#registerForm').on('submit', function(e) {
        e.preventDefault();
        localStorage.setItem('boredgamers-demo-logged-in', 'true');

        var registerModal = bootstrap.Modal.getInstance($('#registerModal')[0]);
        if (registerModal) {
            registerModal.hide();
        }

        alert('Demo mode: Registration submitted. Implement ASP.NET Identity for real authentication.');
    });
}

/* ============================================================================
   Navbar Scroll Effect
   Add shadow/style to navbar when page is scrolled
   ============================================================================ */

function initNavbarScroll() {
    var $navbar = $('.navbar');

    if ($navbar.length) {
        $(window).on('scroll', function() {
            if ($(window).scrollTop() > 10) {
                $navbar.addClass('scrolled');
            } else {
                $navbar.removeClass('scrolled');
            }
        });
    }
}

/* ============================================================================
   Search Results
   Reads query from URL, calls the search API, and renders game cards
   ============================================================================ */

function initSearchResults() {
    var $results = $('#searchResults');
    var $noResults = $('#noResults');

    // Only run on pages that have the search results container
    if (!$results.length) return;

    // Read "q" from the URL query string
    var params = new URLSearchParams(window.location.search);
    var query = params.get('q');

    if (!query || query.trim() === '') return;

    // Show loading state
    $results.html('<div class="text-center py-5"><div class="spinner-border text-light" role="status"></div><p class="mt-2 text-muted">Searching...</p></div>');

    // Call the existing search API
    $.get('/api/games/search?q=' + encodeURIComponent(query.trim()) + '&limit=10')
        .done(function(games) {
            if (games.length === 0) {
                $results.empty();
                $noResults.show();
                return;
            }

            var html = '<div class="row row-cols-2 row-cols-md-4 g-4">';

            games.forEach(function(game) {
                var thumbnail = game.thumbnailUrl || '/images/no-image.png';
                var year = game.yearPublished ? ' (' + game.yearPublished + ')' : '';
                var rank = game.bggRank ? '#' + game.bggRank : '';

                html += '<div class="col">';
                html += '  <div class="card bg-dark text-white h-100">';
                html += '    <img src="' + thumbnail + '" class="card-img-top" alt="' + game.name + '">';
                html += '    <div class="card-body">';
                html += '      <h6 class="card-title">' + game.name + year + '</h6>';
                if (rank) {
                    html += '      <small class="text-muted">BGG Rank: ' + rank + '</small>';
                }
                html += '    </div>';
                html += '  </div>';
                html += '</div>';
            });

            html += '</div>';
            $results.html(html);
        })
        .fail(function() {
            $results.html('<div class="text-center py-5 text-danger"><p>Something went wrong. Please try again.</p></div>');
        });
}
