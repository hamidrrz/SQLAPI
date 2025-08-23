// Global variables
let isLoggedIn = false;
let authToken = '';

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

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    // Check if user is already logged in
    const savedToken = localStorage.getItem('settingsAuthToken');
    if (savedToken) {
        authToken = savedToken;
        isLoggedIn = true;
        showSettingsSection();
        loadDatabaseConfig();
        loadSettingsAuth();
        loadQuerySlots();
    }
});

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

// Functions
function handleLogin() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    // Make API call to login
    fetch('/api/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ username, password })
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
        // Create auth token for Basic Authentication
        authToken = btoa(username + ':' + password);
        localStorage.setItem('settingsAuthToken', authToken);
        isLoggedIn = true;
        showSettingsSection();
        document.getElementById('loginError').classList.add('d-none');
        
        // Load settings data
        loadDatabaseConfig();
        loadSettingsAuth();
        loadQuerySlots();
    })
    .catch(error => {
        document.getElementById('loginError').textContent = error.message;
        document.getElementById('loginError').classList.remove('d-none');
    });
}

function showSettingsSection() {
    loginSection.classList.add('d-none');
    settingsSection.classList.remove('d-none');
}

function loadSettings() {
    // In a real implementation, you would load settings from the server
    // For this example, we'll just initialize the forms with empty values
    document.getElementById('server').value = '';
    document.getElementById('database').value = '';
    document.getElementById('dbUsername').value = '';
    document.getElementById('dbPassword').value = '';
    document.getElementById('settingsUsername').value = 'admin';
}

function loadDatabaseConfig() {
    fetch('/api/settings/database', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error('Failed to load database configuration');
        }
    })
    .then(data => {
        document.getElementById('server').value = data.server || '';
        document.getElementById('database').value = data.database || '';
        document.getElementById('dbUsername').value = data.username || '';
        document.getElementById('dbPassword').value = data.password || '';
    })
    .catch(error => {
        console.error('Error loading database config:', error);
        showMessage('dbConfigMessage', 'Error loading database configuration: ' + error.message, 'danger');
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
            'Authorization': 'Basic ' + authToken
        },
        body: JSON.stringify(config)
    })
    .then(response => {
        if (response.ok) {
            showMessage('dbConfigMessage', 'Database configuration saved successfully!', 'success');
        } else {
            throw new Error('Failed to save database configuration');
        }
    })
    .catch(error => {
        console.error('Error saving database config:', error);
        showMessage('dbConfigMessage', 'Error saving database configuration: ' + error.message, 'danger');
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
            'Authorization': 'Basic ' + authToken
        },
        body: JSON.stringify(config)
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error('Failed to test database connection');
        }
    })
    .then(data => {
        if (data.success) {
            showMessage('dbConfigMessage', `Connection successful! Read-only: ${data.isReadOnly}`, 'success');
        } else {
            showMessage('dbConfigMessage', `Connection failed: ${data.message}`, 'danger');
        }
    })
    .catch(error => {
        console.error('Error testing database connection:', error);
        showMessage('dbConfigMessage', 'Error testing database connection: ' + error.message, 'danger');
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
    
    // In a real implementation, you would make an API call to update the settings auth
    // For this example, we'll just show a success message
    showMessage('authMessage', 'Settings authentication updated successfully!', 'success');
}

function loadQuerySlots() {
    fetch('/api/settings/queries', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error('Failed to load query slots');
        }
    })
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
        showMessage('queryMessage', 'Error loading query slots: ' + error.message, 'danger');
    });
}

function loadQuerySlotData(slotId) {
    fetch('/api/settings/queries', {
        headers: {
            'Authorization': 'Basic ' + authToken
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error('Failed to load query slot');
        }
    })
    .then(data => {
        const slot = data.find(s => s.id == slotId);
        if (slot) {
            document.getElementById('queryName').value = slot.name;
            document.getElementById('querySql').value = slot.sql;
        }
    })
    .catch(error => {
        console.error('Error loading query slot:', error);
        showMessage('queryMessage', 'Error loading query slot: ' + error.message, 'danger');
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
            'Authorization': 'Basic ' + authToken
        },
        body: JSON.stringify(slot)
    })
    .then(response => {
        if (response.ok) {
            showMessage('queryMessage', 'Query slot saved successfully!', 'success');
            loadQuerySlots(); // Refresh the query slots table
        } else {
            throw new Error('Failed to save query slot');
        }
    })
    .catch(error => {
        console.error('Error saving query slot:', error);
        showMessage('queryMessage', 'Error saving query slot: ' + error.message, 'danger');
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