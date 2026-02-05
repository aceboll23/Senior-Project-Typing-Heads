/* ============================================================================
   BoredGamers - Main JavaScript (jQuery 4)
   Handles modal switching, demo login simulation, and basic interactivity
   ============================================================================ */

$(document).ready(function() {
    initModals();
    initDemoLogin();
    initNavbarScroll();
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
