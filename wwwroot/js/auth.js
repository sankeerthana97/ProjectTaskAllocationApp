document.addEventListener('DOMContentLoaded', function() {
    // Login form handling
    const loginForm = document.querySelector('#loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const email = document.querySelector('#email').value;
            const password = document.querySelector('#password').value;

            try {
                const response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email, password })
                });

                if (response.ok) {
                    const data = await response.json();
                    localStorage.setItem('token', data.token);
                    window.location.href = data.redirectUrl;
                } else {
                    const error = await response.json();
                    document.querySelector('.error-message').textContent = error.message;
                }
            } catch (error) {
                console.error('Login error:', error);
                document.querySelector('.error-message').textContent = 'An error occurred during login';
            }
        });
    }

    // Register form handling
    const registerForm = document.querySelector('#registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const formData = new FormData(this);
            const userData = {};
            formData.forEach((value, key) => userData[key] = value);

            try {
                const response = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(userData)
                });

                if (response.ok) {
                    const data = await response.json();
                    localStorage.setItem('token', data.token);
                    window.location.href = data.redirectUrl;
                } else {
                    const error = await response.json();
                    document.querySelector('.error-message').textContent = error.message;
                }
            } catch (error) {
                console.error('Registration error:', error);
                document.querySelector('.error-message').textContent = 'An error occurred during registration';
            }
        });
    }

    // Logout functionality
    const logoutButton = document.querySelector('#logoutButton');
    if (logoutButton) {
        logoutButton.addEventListener('click', function() {
            localStorage.removeItem('token');
            window.location.href = '/login';
        });
    }
});
