document.addEventListener('DOMContentLoaded', function() {
    // Initialize user name
    const token = localStorage.getItem('token');
    if (token) {
        const decodedToken = JSON.parse(atob(token.split('.')[1]));
        document.getElementById('userName').textContent = decodedToken.name;
    }

    // Handle logout
    document.getElementById('logoutButton').addEventListener('click', function() {
        localStorage.removeItem('token');
        window.location.href = '/login';
    });

    // Update performance metrics
    async function updatePerformance() {
        try {
            const response = await fetch('/api/users/me/performance', {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!response.ok) throw new Error('Failed to fetch performance data');
            const performance = await response.json();

            // Update performance bar and text
            const performanceBar = document.getElementById('performanceBar');
            const performanceText = document.getElementById('performanceText');
            performanceBar.style.width = `${performance.performance}%`;
            performanceText.textContent = `Performance: ${performance.performance}%`;

            // Add performance status
            const performanceStatus = document.getElementById('performanceStatus');
            if (performance.performance < 40) {
                performanceStatus.textContent = 'UNAVAILABLE';
                performanceStatus.className = 'text-danger';
            } else {
                performanceStatus.textContent = 'AVAILABLE';
                performanceStatus.className = 'text-success';
            }

            // Update workload bar and text
            const workloadBar = document.getElementById('workloadBar');
            const workloadText = document.getElementById('workloadText');
            workloadBar.style.width = `${performance.workload}%`;
            workloadText.textContent = `Workload: ${performance.workload}% (${Math.floor(performance.workload / 10)} tasks)`;

            // Update task count
            const taskCount = document.getElementById('taskCount');
            taskCount.textContent = `${Math.floor(performance.workload / 10)} active tasks`;

        } catch (error) {
            console.error('Error updating performance:', error);
            alert('Failed to load performance data. Please try again.');
        }
    }

    updatePerformance();
});

async function loadTasks() {
    try {
        const response = await fetch('/api/project/tasks/assigned', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const tasks = await response.json();
            updateTaskStats(tasks);
            displayTasks(tasks);
        } else {
            const error = await response.json();
            showError('Failed to load tasks: ' + error.message);
        }
    } catch (error) {
        console.error('Error loading tasks:', error);
        showError('Failed to load tasks');
    }
}

function updateTaskStats(tasks) {
    const total = tasks.length;
    const completed = tasks.filter(t => t.status === 'Completed').length;
    const inProgress = tasks.filter(t => t.status === 'InProgress').length;

    document.getElementById('totalTasks').textContent = total;
    document.getElementById('completedTasks').textContent = completed;
    document.getElementById('inProgressTasks').textContent = inProgress;
}

function displayTasks(tasks) {
    const table = document.createElement('table');
    table.className = 'table';

    // Create table header
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    ['Project', 'Task Name', 'Status', 'Deadline', 'Priority', 'Actions'].forEach(headerText => {
        const th = document.createElement('th');
        th.textContent = headerText;
        headerRow.appendChild(th);
    });
    thead.appendChild(headerRow);
    table.appendChild(thead);

    // Create table body
    const tbody = document.createElement('tbody');
    tasks.forEach(task => {
        const row = document.createElement('tr');
        
        // Project
        const projectCell = document.createElement('td');
        projectCell.textContent = task.project?.name || 'N/A';
        row.appendChild(projectCell);

        // Task Name
        const nameCell = document.createElement('td');
        nameCell.textContent = task.name;
        row.appendChild(nameCell);

        // Status
        const statusCell = document.createElement('td');
        statusCell.textContent = task.status;
        statusCell.className = `status-${task.status.toLowerCase().replace(' ', '-')}`;
        row.appendChild(statusCell);

        // Deadline
        const deadlineCell = document.createElement('td');
        deadlineCell.textContent = task.deadline ? new Date(task.deadline).toLocaleDateString() : '';
        row.appendChild(deadlineCell);

        // Priority
        const priorityCell = document.createElement('td');
        priorityCell.textContent = task.priority || 'Normal';
        priorityCell.className = `badge bg-${getPriorityBadgeClass(task.priority)}`;
        row.appendChild(priorityCell);

        // Actions
        const actionsCell = document.createElement('td');
        actionsCell.innerHTML = getTaskActionButtons(task);
        row.appendChild(actionsCell);
        tbody.appendChild(row);
    });

    table.appendChild(tbody);
    document.getElementById('tasksTable').innerHTML = '';
    document.getElementById('tasksTable').appendChild(table);
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'alert alert-danger';
    errorDiv.textContent = message;
    document.getElementById('tasksTable').innerHTML = '';
    document.getElementById('tasksTable').appendChild(errorDiv);
}

function showTaskDetails(task) {
    const modal = document.getElementById('taskDetailsModal');
    const modalBody = document.getElementById('taskDetails');
    
    modalBody.innerHTML = `
        <h5>${task.name}</h5>
        <p><strong>Project:</strong> ${task.project?.name || 'N/A'}</p>
        <p><strong>Description:</strong> ${task.description}</p>
        <p><strong>Status:</strong> ${task.status}</p>
        <p><strong>Priority:</strong> ${task.priority || 'Normal'}</p>
        <p><strong>Deadline:</strong> ${task.deadline ? new Date(task.deadline).toLocaleDateString() : 'N/A'}</p>
    `;

    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

function showUpdateStatusModal(task) {
    const modal = document.getElementById('taskDetailsModal');
    const modalBody = document.getElementById('taskDetails');
    
    modalBody.innerHTML = `
        <form id="statusForm">
            <div class="mb-3">
                <label for="status" class="form-label">Status</label>
                <select class="form-select" id="status" required>
                    <option value="NotStarted" ${task.status === 'NotStarted' ? 'selected' : ''}>Not Started</option>
                    <option value="InProgress" ${task.status === 'InProgress' ? 'selected' : ''}>In Progress</option>
                    <option value="Completed" ${task.status === 'Completed' ? 'selected' : ''}>Completed</option>
                    <option value="Blocked" ${task.status === 'Blocked' ? 'selected' : ''}>Blocked</option>
                </select>
            </div>
        </form>
    `;

    // Store task ID in modal data attribute
    modal.setAttribute('data-task-id', task.id);

    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

async function updateTaskStatus() {
    const modal = document.getElementById('taskDetailsModal');
    const taskId = modal.getAttribute('data-task-id');
    const status = document.getElementById('status').value;

    try {
        const response = await fetch(`/api/project/tasks/${taskId}/status`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ status })
        });

        if (response.ok) {
            // Close modal and reload tasks
            const bsModal = bootstrap.Modal.getInstance(modal);
            bsModal.hide();
            loadTasks();
        } else {
            const error = await response.json();
            showError('Failed to update task status: ' + error.message);
        }
    } catch (error) {
        console.error('Error updating task status:', error);
        showError('Failed to update task status');
    }
}
