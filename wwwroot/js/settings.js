// Global variables
let isLoggedIn = false;
let authToken = '';
let antiForgeryToken = '';

// DOM Elements
const loginSection = document.getElementById('loginSection');
const settingsSection = document.getElementById('settingsSection');
const loginForm = document.getElementById('loginForm');
const databaseConfigForm = document.getElementById('databaseConfigForm');
const settingsAuthForm = document.getElementById('settingsAuthForm');
const querySlotForm = document.getElementById('querySlotForm');
const querySlotSelect = document.getElementById('querySlotSelect');
const querySlotsTableBody = document.getElementById('querySlotsTableBody');
const testConnectionBtn = document.getElementById('testConnectionBtn');
const apiKeyForm = document.getElementById('apiKeyForm');
const apiKeysTableBody = document.getElementById('apiKeysTableBody');
const logoutBtn = document.getElementById('logoutBtn');

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    // Check for saved authentication token
    checkSavedAuth();
    
    // Load saved settings if available
    loadSettings();
    
    // Populate query slots table
    loadQuerySlots();
    
    // Load API keys
    // loadApiKeys(); // Moved to showSettingsSection() to ensure it's called after authentication
    
    // Debugging: Add event listener to apiKeyName input field
    const apiKeyNameElement = document.getElementById('apiKeyName');
    if (apiKeyNameElement) {
        apiKeyNameElement.addEventListener('input', function(e) {
            console.log('apiKeyName input value changed:', e.target.value);
        });
        
        // Debugging: Add event listener to log when the input field is focused
        apiKeyNameElement.addEventListener('focus', function(e) {
            console.log('apiKeyName input field focused');
        });
        
        // Debugging: Add event listener to log when the input field is blurred
        apiKeyNameElement.addEventListener('blur', function(e) {
            console.log('apiKeyName input field blurred');
        });
        
        // Debugging: Add event listener to log when a key is pressed in the input field
        apiKeyNameElement.addEventListener('keydown', function(e) {
            console.log('apiKeyName keydown event:', e.key);
        });
    }
});

// Get anti-forgery token from server
async function getAntiForgeryToken() {
    console.log('Getting anti-forgery token...');
    try {
        const response = await fetch('/api/settings/antiforgery-token', {
            method: 'GET',
            headers: {
                'Authorization': 'Basic ' + authToken
            }
        });
        
        console.log('Anti-forgery token response:', response);
        if (response.ok) {
            const data = await response.json();
            antiForgeryToken = data.token;
            console.log('Anti-forgery token retrieved:', antiForgeryToken);
            return data.token; // Return the token
        } else {
            console.error('Failed to retrieve anti-forgery token, status:', response.status);
            throw new Error('Failed to retrieve anti-forgery token');
        }
    } catch (error) {
        console.error('Error retrieving anti-forgery token:', error);
        throw error; // Re-throw the error
    }
}

loginForm.addEventListener('submit', function(e) {
    e.preventDefault();
    handleLogin();
});

databaseConfigForm.addEventListener('submit', function(e) {
    e.preventDefault();
    saveDatabaseConfig();
});

settingsAuthForm.addEventListener('submit', function(e) {
    e.preventDefault();
    updateSettingsAuth();
});

querySlotForm.addEventListener('submit', function(e) {
    e.preventDefault();
    saveQuerySlot();
});

querySlotSelect.addEventListener('change', function() {
    loadQuerySlotData(this.value);
});

testConnectionBtn.addEventListener('click', function() {
    testDatabaseConnection();
});

apiKeyForm.addEventListener('submit', function(e) {
    // Debugging: Log when the form is submitted
    console.log('apiKeyForm submitted');
    console.log('Event details:', e);
    
    // Debugging: Check if the input field has focus when the form is submitted
    const apiKeyNameElement = document.getElementById('apiKeyName');
    console.log('apiKeyNameElement found:', apiKeyNameElement);
    if (apiKeyNameElement) {
        console.log('apiKeyNameElement has focus:', document.activeElement === apiKeyNameElement);
    }
    
    e.preventDefault();
    generateApiKey();
});

// Functions
function handleLogin() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    // Make API call to authenticate
    const loginData = {
        username: username,
        password: password
    };
    
    fetch('/api/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(loginData)
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else if (response.status === 401) {
            throw new Error('Invalid credentials');
        } else {
            throw new Error('Login failed');
        }
    })
    .then(data => {
        isLoggedIn = true;
        authToken = btoa(username + ':' + password); // Base64 encode for Basic Auth
        // Debugging: Log the authToken
        console.log('authToken set to:', authToken);
        showSettingsSection();
        document.getElementById('loginError').classList.add('d-none');
    })
    .catch(error => {
        document.getElementById('loginError').textContent = error.message;
        document.getElementById('loginError').classList.remove('d-none');
    });
}

function showSettingsSection() {
    loginSection.classList.add('d-none');
    settingsSection.classList.remove('d-none');
    
    // Load current settings
    loadDatabaseConfig();
    loadSettingsAuth();
    loadApiKeys(); // Load API keys after authentication
}

function loadSettings() {
    // In a real implementation, you would load settings from the server
    // For this example, we'll just initialize the forms with empty values
    document.getElementById('server').value = '';
    document.getElementById('database').value = '';
    document.getElementById('dbUsername').value = '';
    document.getElementById('dbPassword').value = '';
    document.getElementById('settingsUsername').value = 'newadmin';
}

function loadSettingsAuth() {
    fetch('/api/settings/auth', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => response.json())
    .then(data => {
        document.getElementById('settingsUsername').value = data.username || '';
        // Note: We don't load the password for security reasons
        document.getElementById('settingsPassword').value = '';
        document.getElementById('confirmPassword').value = '';
    })
    .catch(error => {
        console.error('Error loading settings auth:', error);
        // Initialize with default values if there's an error
        document.getElementById('settingsUsername').value = 'admin';
        document.getElementById('settingsPassword').value = '';
        document.getElementById('confirmPassword').value = '';
    });
}

function loadDatabaseConfig() {
    fetch('/api/settings/database', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => response.json())
    .then(data => {
        document.getElementById('server').value = data.server || '';
        document.getElementById('database').value = data.database || '';
        document.getElementById('dbUsername').value = data.username || '';
        document.getElementById('dbPassword').value = data.password || '';
    })
    .catch(error => {
        console.error('Error loading database config:', error);
    });
}

function saveDatabaseConfig() {
    const config = {
        server: document.getElementById('server').value,
        database: document.getElementById('database').value,
        username: document.getElementById('dbUsername').value,
        password: document.getElementById('dbPassword').value
    };
    
    fetch('/api/settings/database', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Basic ' + authToken,
            'RequestVerificationToken': antiForgeryToken
        },
        body: JSON.stringify(config)
    })
    .then(response => {
        if (response.ok) {
            showMessage('dbConfigMessage', 'Database configuration saved successfully!', 'success');
        } else {
            showMessage('dbConfigMessage', 'Error saving database configuration', 'danger');
        }
    })
    .catch(error => {
        console.error('Error saving database config:', error);
        showMessage('dbConfigMessage', 'Error saving database configuration', 'danger');
    });
}

function testDatabaseConnection() {
    const config = {
        server: document.getElementById('server').value,
        database: document.getElementById('database').value,
        username: document.getElementById('dbUsername').value,
        password: document.getElementById('dbPassword').value
    };
    
    fetch('/api/settings/test-connection', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Basic ' + authToken,
            'RequestVerificationToken': antiForgeryToken
        },
        body: JSON.stringify(config)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showMessage('dbConfigMessage', `Connection successful! Read-only: ${data.isReadOnly}`, 'success');
        } else {
            showMessage('dbConfigMessage', `Connection failed: ${data.message}`, 'danger');
        }
    })
    .catch(error => {
        console.error('Error testing database connection:', error);
        showMessage('dbConfigMessage', 'Error testing database connection', 'danger');
    });
}

function updateSettingsAuth() {
    const settingsUsername = document.getElementById('settingsUsername').value;
    const settingsPassword = document.getElementById('settingsPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    
    if (settingsPassword !== confirmPassword) {
        showMessage('authMessage', 'Passwords do not match', 'danger');
        return;
    }
    
    const auth = {
        username: settingsUsername,
        password: settingsPassword
    };
    
    fetch('/api/settings/auth', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Basic ' + authToken,
            'RequestVerificationToken': antiForgeryToken
        },
        body: JSON.stringify(auth)
    })
    .then(response => {
        if (response.ok) {
            showMessage('authMessage', 'Settings authentication updated successfully!', 'success');
        } else {
            showMessage('authMessage', 'Error updating settings authentication', 'danger');
        }
    })
    .catch(error => {
        console.error('Error updating settings auth:', error);
        showMessage('authMessage', 'Error updating settings authentication', 'danger');
    });
}

function loadQuerySlots() {
    fetch('/api/settings/queries', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => response.json())
    .then(data => {
        // Clear existing table rows
        querySlotsTableBody.innerHTML = '';
        
        // Add rows for each query slot
        data.forEach(slot => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${slot.id}</td>
                <td>${slot.name}</td>
                <td>${slot.sql.substring(0, 50)}${slot.sql.length > 50 ? '...' : ''}</td>
            `;
            querySlotsTableBody.appendChild(row);
        });
    })
    .catch(error => {
        console.error('Error loading query slots:', error);
    });
}

function loadQuerySlotData(slotId) {
    fetch('/api/settings/queries', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => response.json())
    .then(data => {
        const slot = data.find(s => s.id == slotId);
        if (slot) {
            document.getElementById('queryName').value = slot.name;
            document.getElementById('querySql').value = slot.sql;
        }
    })
    .catch(error => {
        console.error('Error loading query slot:', error);
    });
}

function saveQuerySlot() {
    const slotId = parseInt(querySlotSelect.value);
    const slotName = document.getElementById('queryName').value;
    const slotSql = document.getElementById('querySql').value;
    
    const slot = {
        id: slotId,
        name: slotName,
        sql: slotSql
    };
    
    fetch(`/api/settings/queries/${slotId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Basic ' + authToken,
            'RequestVerificationToken': antiForgeryToken
        },
        body: JSON.stringify(slot)
    })
    .then(response => {
        if (response.ok) {
            showMessage('queryMessage', 'Query slot saved successfully!', 'success');
            loadQuerySlots(); // Refresh the query slots table
        } else {
            showMessage('queryMessage', 'Error saving query slot', 'danger');
        }
    })
    .catch(error => {
        console.error('Error saving query slot:', error);
        showMessage('queryMessage', 'Error saving query slot', 'danger');
    });
}

function loadApiKeys() {
    fetch('/api/settings/apikeys', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => response.json())
    .then(data => {
        // Clear existing table rows
        apiKeysTableBody.innerHTML = '';
        
        // Add rows for each API key
        data.forEach(key => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${key.name}</td>
                <td>${key.key.substring(0, 8)}...</td>
                <td>${new Date(key.created).toLocaleString()}</td>
                <td>${key.isActive ? 'Active' : 'Revoked'}</td>
                <td>
                    ${key.isActive ? 
                        `<button class="btn btn-sm btn-danger" onclick="revokeApiKey('${key.key}')">Revoke</button>` : 
                        'Revoked'
                    }
                </td>
            `;
            apiKeysTableBody.appendChild(row);
        });
    })
    .catch(error => {
        console.error('Error loading API keys:', error);
    });
}

function generateApiKey() {
    // Debugging: Log when the function is called
    console.log('generateApiKey function called');
    
    // Debugging: Add a small delay to see if that helps
    setTimeout(() => {
        // Debugging: Log the input field element
        const apiKeyNameElement = document.getElementById('apiKeyName');
        console.log('apiKeyNameElement:', apiKeyNameElement);
        
        // Debugging: Log the value and other properties of the input field
        if (apiKeyNameElement) {
            console.log('apiKeyName value:', apiKeyNameElement.value);
            console.log('apiKeyName trimmed value:', apiKeyNameElement.value.trim());
            console.log('apiKeyName length:', apiKeyNameElement.value.length);
            console.log('apiKeyName trimmed length:', apiKeyNameElement.value.trim().length);
        }
        
        const apiKeyName = document.getElementById('apiKeyName').value.trim();
        
        // Debugging: Log the apiKeyName value
        console.log('apiKeyName:', apiKeyName);
        console.log('authToken:', authToken);
        
        if (!apiKeyName) {
            showMessage('apiKeyMessage', 'Please enter a name for your API key', 'danger');
            return;
        }
        
        const request = {
            name: apiKeyName
        };
        
        fetch('/api/settings/apikeys', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Basic ' + authToken,
                'RequestVerificationToken': antiForgeryToken
            },
            body: JSON.stringify(request)
        })
        .then(response => {
            // Debugging: Log the response status and headers
            console.log('Response status:', response.status);
            console.log('Response headers:', response.headers);
            if (response.ok) {
                return response.json();
            } else {
                // Debugging: Log the response text for non-OK responses
                return response.text().then(text => {
                    console.log('Response text:', text);
                    throw new Error(`HTTP error! status: ${response.status}, message: ${text}`);
                });
            }
        })
        .then(data => {
            showMessage('apiKeyMessage', `API key generated successfully! Key: ${data.key}`, 'success');
            document.getElementById('apiKeyName').value = '';
            loadApiKeys(); // Refresh the API keys table
        })
        .catch(error => {
            console.error('Error generating API key:', error);
            showMessage('apiKeyMessage', 'Error generating API key: ' + error.message, 'danger');
        });
    }, 100); // 100ms delay
}

function revokeApiKey(key) {
    if (!confirm('Are you sure you want to revoke this API key?')) {
        return;
    }
    
    fetch(`/api/settings/apikeys/${key}/revoke`, {
        method: 'POST',
        headers: {
            'Authorization': 'Basic ' + authToken,
            'RequestVerificationToken': antiForgeryToken
        }
    })
    .then(response => response.json())
    .then(data => {
        showMessage('apiKeyMessage', data.message, 'success');
        loadApiKeys(); // Refresh the API keys table
    })
    .catch(error => {
        console.error('Error revoking API key:', error);
        showMessage('apiKeyMessage', 'Error revoking API key', 'danger');
    });
}

function showMessage(elementId, message, type) {
    const element = document.getElementById(elementId);
    element.textContent = message;
    element.className = `alert alert-${type} mt-3`;
    element.classList.remove('d-none');
    
    // Hide the message after 5 seconds
    setTimeout(() => {
        element.classList.add('d-none');
    }, 5000);
}

// Add event listener for logout button if it exists
if (logoutBtn) {
    logoutBtn.addEventListener('click', function(e) {
        e.preventDefault();
        handleLogout();
    });
}

// Check for saved authentication token in localStorage
function checkSavedAuth() {
    const savedAuthToken = localStorage.getItem('sqlapi_auth_token');
    if (savedAuthToken) {
        // Verify the token is still valid by making a simple API call
        fetch('/api/settings/database', {
            headers: {
                'Authorization': 'Basic ' + savedAuthToken
            }
        })
        .then(response => {
            if (response.ok) {
                // Token is valid, use it
                authToken = savedAuthToken;
                isLoggedIn = true;
                showSettingsSection();
            } else {
                // Token is invalid, clear it
                localStorage.removeItem('sqlapi_auth_token');
            }
        })
        .catch(error => {
            console.error('Error verifying saved auth token:', error);
            // Clear the token if there's an error
            localStorage.removeItem('sqlapi_auth_token');
        });
    }
}

// Handle logout
function handleLogout() {
    // Clear authentication variables
    isLoggedIn = false;
    authToken = '';
    
    // Remove saved authentication token from localStorage
    localStorage.removeItem('sqlapi_auth_token');
    
    // Show login section and hide settings section
    loginSection.classList.remove('d-none');
    settingsSection.classList.add('d-none');
    
    // Clear login form
    document.getElementById('username').value = '';
    document.getElementById('password').value = '';
    document.getElementById('loginError').classList.add('d-none');
}

// Modified handleLogin to save the authentication token
function handleLogin() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    // Make API call to authenticate
    const loginData = {
        username: username,
        password: password
    };
    
    fetch('/api/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(loginData)
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else if (response.status === 401) {
            throw new Error('Invalid credentials');
        } else {
            throw new Error('Login failed');
        }
    })
    .then(data => {
        isLoggedIn = true;
        authToken = btoa(username + ':' + password); // Base64 encode for Basic Auth
        // Save the authentication token to localStorage
        localStorage.setItem('sqlapi_auth_token', authToken);
        // Debugging: Log the authToken
        console.log('authToken set to:', authToken);
        showSettingsSection();
        document.getElementById('loginError').classList.add('d-none');
    })
    .catch(error => {
        document.getElementById('loginError').textContent = error.message;
        document.getElementById('loginError').classList.remove('d-none');
    });
}

// Modified showSettingsSection to save the authentication token
function showSettingsSection() {
    loginSection.classList.add('d-none');
    settingsSection.classList.remove('d-none');
    
    // Get anti-forgery token first
    getAntiForgeryToken().then(() => {
        console.log('Anti-forgery token retrieved successfully:', antiForgeryToken);
        // Load current settings after anti-forgery token is retrieved
        loadDatabaseConfig();
        loadSettingsAuth();
        loadApiKeys(); // Load API keys after authentication
    }).catch(error => {
        console.error('Error getting anti-forgery token:', error);
        // Still load settings even if anti-forgery token retrieval fails
        loadDatabaseConfig();
        loadSettingsAuth();
        loadApiKeys();
    });
}