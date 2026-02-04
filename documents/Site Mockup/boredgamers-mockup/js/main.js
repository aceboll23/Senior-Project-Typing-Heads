/* ============================================================================
   BoredGamers - Main JavaScript
   Handles theme toggle, login simulation, modals, and basic interactivity

   NOTE: This file uses javascript for minimal interactivity.
   Search for "javascript" to find all JS references if migrating to another language.
   ============================================================================ */

// javascript - Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
  initTheme();
  initAuth();
  initMobileNav();
  initModals();
  initDropdowns();
  initTabs();
  initNavbarScroll();
});

/* ============================================================================
   Theme Toggle (javascript)
   ============================================================================ */

function initTheme() {
  // javascript - Check for saved theme preference or default to dark
  const savedTheme = localStorage.getItem('boredgamers-theme') || 'dark';
  document.documentElement.setAttribute('data-theme', savedTheme);

  // javascript - Theme toggle button handler
  const themeToggle = document.getElementById('theme-toggle');
  if (themeToggle) {
    themeToggle.addEventListener('click', function() {
      const currentTheme = document.documentElement.getAttribute('data-theme');
      const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

      document.documentElement.setAttribute('data-theme', newTheme);
      localStorage.setItem('boredgamers-theme', newTheme);
    });
  }
}

/* ============================================================================
   Authentication Simulation (javascript)
   ============================================================================ */

function initAuth() {
  // javascript - Check if user is "logged in" from localStorage
  const isLoggedIn = localStorage.getItem('boredgamers-logged-in') === 'true';
  updateAuthState(isLoggedIn);

  // javascript - Login button handler (opens modal, but for now just logs in)
  const loginBtns = document.querySelectorAll('[data-action="login"]');
  loginBtns.forEach(function(btn) {
    btn.addEventListener('click', function(e) {
      e.preventDefault();
      // Open login modal
      openModal('login-modal');
    });
  });

  // javascript - Fake login form submission
  const loginForm = document.getElementById('login-form');
  if (loginForm) {
    loginForm.addEventListener('submit', function(e) {
      e.preventDefault();
      // Simulate successful login
      localStorage.setItem('boredgamers-logged-in', 'true');
      updateAuthState(true);
      closeAllModals();
    });
  }

  // javascript - Quick login button (skips form)
  const quickLoginBtns = document.querySelectorAll('[data-action="quick-login"]');
  quickLoginBtns.forEach(function(btn) {
    btn.addEventListener('click', function(e) {
      e.preventDefault();
      localStorage.setItem('boredgamers-logged-in', 'true');
      updateAuthState(true);
      closeAllModals();
    });
  });

  // javascript - Logout handler
  const logoutBtns = document.querySelectorAll('[data-action="logout"]');
  logoutBtns.forEach(function(btn) {
    btn.addEventListener('click', function(e) {
      e.preventDefault();
      localStorage.setItem('boredgamers-logged-in', 'false');
      updateAuthState(false);
      // Close any open dropdowns
      closeAllDropdowns();
    });
  });

  // javascript - Register form opens from login modal
  const showRegisterLinks = document.querySelectorAll('[data-action="show-register"]');
  showRegisterLinks.forEach(function(link) {
    link.addEventListener('click', function(e) {
      e.preventDefault();
      closeAllModals();
      openModal('register-modal');
    });
  });

  // javascript - Login form opens from register modal
  const showLoginLinks = document.querySelectorAll('[data-action="show-login"]');
  showLoginLinks.forEach(function(link) {
    link.addEventListener('click', function(e) {
      e.preventDefault();
      closeAllModals();
      openModal('login-modal');
    });
  });

  // javascript - Register form submission (fake)
  const registerForm = document.getElementById('register-form');
  if (registerForm) {
    registerForm.addEventListener('submit', function(e) {
      e.preventDefault();
      localStorage.setItem('boredgamers-logged-in', 'true');
      updateAuthState(true);
      closeAllModals();
    });
  }
}

// javascript - Update UI based on auth state
function updateAuthState(isLoggedIn) {
  if (isLoggedIn) {
    document.body.classList.add('logged-in');
  } else {
    document.body.classList.remove('logged-in');
  }
}

/* ============================================================================
   Mobile Navigation (javascript)
   ============================================================================ */

function initMobileNav() {
  const mobileToggle = document.getElementById('mobile-nav-toggle');
  const mobileOverlay = document.getElementById('mobile-nav-overlay');
  const mobileClose = document.getElementById('mobile-nav-close');

  if (mobileToggle && mobileOverlay) {
    // javascript - Open mobile nav
    mobileToggle.addEventListener('click', function() {
      mobileOverlay.classList.add('active');
      document.body.classList.add('nav-open');
    });

    // javascript - Close mobile nav
    function closeMobileNav() {
      mobileOverlay.classList.remove('active');
      document.body.classList.remove('nav-open');
    }

    if (mobileClose) {
      mobileClose.addEventListener('click', closeMobileNav);
    }

    // javascript - Close on overlay click
    mobileOverlay.addEventListener('click', function(e) {
      if (e.target === mobileOverlay) {
        closeMobileNav();
      }
    });

    // javascript - Close on escape key
    document.addEventListener('keydown', function(e) {
      if (e.key === 'Escape' && mobileOverlay.classList.contains('active')) {
        closeMobileNav();
      }
    });
  }
}

/* ============================================================================
   Modals (javascript)
   ============================================================================ */

function initModals() {
  // javascript - Modal open buttons
  const modalTriggers = document.querySelectorAll('[data-modal]');
  modalTriggers.forEach(function(trigger) {
    trigger.addEventListener('click', function(e) {
      e.preventDefault();
      const modalId = this.getAttribute('data-modal');
      openModal(modalId);
    });
  });

  // javascript - Modal close buttons
  const modalCloses = document.querySelectorAll('[data-modal-close]');
  modalCloses.forEach(function(closeBtn) {
    closeBtn.addEventListener('click', function() {
      const modal = this.closest('.modal-overlay');
      if (modal) {
        modal.classList.remove('active');
        document.body.classList.remove('modal-open');
      }
    });
  });

  // javascript - Close modal on overlay click
  const modalOverlays = document.querySelectorAll('.modal-overlay');
  modalOverlays.forEach(function(overlay) {
    overlay.addEventListener('click', function(e) {
      if (e.target === overlay) {
        overlay.classList.remove('active');
        document.body.classList.remove('modal-open');
      }
    });
  });

  // javascript - Close modal on escape key
  document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
      closeAllModals();
    }
  });
}

// javascript - Open a specific modal
function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.add('active');
    document.body.classList.add('modal-open');
    // Focus first input if exists
    const firstInput = modal.querySelector('input, textarea, select');
    if (firstInput) {
      setTimeout(function() { firstInput.focus(); }, 100);
    }
  }
}

// javascript - Close all modals
function closeAllModals() {
  const modals = document.querySelectorAll('.modal-overlay.active');
  modals.forEach(function(modal) {
    modal.classList.remove('active');
  });
  document.body.classList.remove('modal-open');
}

/* ============================================================================
   Dropdowns (javascript)
   ============================================================================ */

function initDropdowns() {
  const dropdownTriggers = document.querySelectorAll('[data-dropdown]');

  dropdownTriggers.forEach(function(trigger) {
    trigger.addEventListener('click', function(e) {
      e.stopPropagation();
      const dropdown = this.closest('.dropdown') || this.closest('.user-dropdown');

      if (dropdown) {
        // Close other dropdowns first
        const allDropdowns = document.querySelectorAll('.dropdown.active, .user-dropdown.active');
        allDropdowns.forEach(function(d) {
          if (d !== dropdown) d.classList.remove('active');
        });

        // Toggle this dropdown
        dropdown.classList.toggle('active');
      }
    });
  });

  // javascript - Close dropdowns when clicking outside
  document.addEventListener('click', function() {
    closeAllDropdowns();
  });
}

// javascript - Close all dropdowns
function closeAllDropdowns() {
  const dropdowns = document.querySelectorAll('.dropdown.active, .user-dropdown.active');
  dropdowns.forEach(function(dropdown) {
    dropdown.classList.remove('active');
  });
}

/* ============================================================================
   Tabs (javascript)
   ============================================================================ */

function initTabs() {
  const tabButtons = document.querySelectorAll('[data-tab]');

  tabButtons.forEach(function(btn) {
    btn.addEventListener('click', function() {
      const tabGroup = this.closest('.tabs-container') || this.closest('section');
      const targetTab = this.getAttribute('data-tab');

      if (tabGroup) {
        // Remove active from all tabs in this group
        const allTabs = tabGroup.querySelectorAll('[data-tab]');
        allTabs.forEach(function(tab) {
          tab.classList.remove('active');
        });

        // Remove active from all tab contents in this group
        const allContents = tabGroup.querySelectorAll('.tab-content');
        allContents.forEach(function(content) {
          content.classList.remove('active');
        });

        // Activate clicked tab
        this.classList.add('active');

        // Activate corresponding content
        const targetContent = tabGroup.querySelector('#' + targetTab);
        if (targetContent) {
          targetContent.classList.add('active');
        }
      }
    });
  });
}

/* ============================================================================
   Navbar Scroll Effect (javascript)
   ============================================================================ */

function initNavbarScroll() {
  const navbar = document.querySelector('.navbar');

  if (navbar) {
    // javascript - Add shadow on scroll
    window.addEventListener('scroll', function() {
      if (window.scrollY > 10) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    });
  }
}

/* ============================================================================
   Utility: Prevent body scroll when modal/nav open (javascript)
   ============================================================================ */

// CSS handles this with body.modal-open and body.nav-open classes
