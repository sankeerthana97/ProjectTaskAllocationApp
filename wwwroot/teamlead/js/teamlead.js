document.addEventListener('DOMContentLoaded', function() {
    // Initialize user name
    const token = localStorage.getItem('token');
    if (token) {
        const decodedToken = JSON.parse(atob(token.split('.')[1]));
        document.getElementById('userName').textContent = decodedToken.name;
    }

    // Load projects
    loadProjects();

    // Handle logout
    document.getElementById('logoutButton').addEventListener('click', function() {
        localStorage.removeItem('token');
        window.location.href = '/login';
    });
});

async function loadProjects() {
    try {
        const response = await fetch('/api/project', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const projects = await response.json();
            displayProjects(projects);
        } else {
            const error = await response.json();
            showError('Failed to load projects: ' + error.message);
        }
    } catch (error) {
        console.error('Error loading projects:', error);
        showError('Failed to load projects');
    }
}

function displayProjects(projects) {
    const table = document.createElement('table');
    table.className = 'table';

    // Create table header
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    ['Project Name', 'Status', 'Start Date', 'End Date', 'Tasks', 'Actions'].forEach(headerText => {
        const th = document.createElement('th');
        th.textContent = headerText;
        headerRow.appendChild(th);
    });
    thead.appendChild(headerRow);
    table.appendChild(thead);

    // Create table body
    const tbody = document.createElement('tbody');
    projects.forEach(project => {
        const row = document.createElement('tr');
        
        // Project Name
        const nameCell = document.createElement('td');
        nameCell.textContent = project.name;
        row.appendChild(nameCell);

        // Status
        const statusCell = document.createElement('td');
        statusCell.textContent = project.status;
        row.appendChild(statusCell);

        // Start Date
        const startDateCell = document.createElement('td');
        startDateCell.textContent = new Date(project.startDate).toLocaleDateString();
        row.appendChild(startDateCell);

        // End Date
        const endDateCell = document.createElement('td');
        endDateCell.textContent = new Date(project.endDate).toLocaleDateString();
        row.appendChild(endDateCell);

        // Tasks
        const tasksCell = document.createElement('td');
        tasksCell.textContent = project.tasks?.length || 0;
        row.appendChild(tasksCell);

        // Actions
        const actionsCell = document.createElement('td');
        const viewBtn = document.createElement('button');
        viewBtn.className = 'btn btn-sm btn-primary me-2';
        viewBtn.textContent = 'View';
        viewBtn.onclick = () => viewProject(project.id);
        actionsCell.appendChild(viewBtn);

        const editBtn = document.createElement('button');
        editBtn.className = 'btn btn-sm btn-warning me-2';
        editBtn.textContent = 'Edit';
        editBtn.onclick = () => editProject(project.id);
        actionsCell.appendChild(editBtn);
    });
}

// Show project details
async function showProjectDetails(projectId) {
    try {
        const response = await fetch(`/api/projects/${projectId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch project details');
        const project = await response.json();

        // Create and show details modal
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'projectDetailsModal';
        modal.innerHTML = `
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Project Details</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <h4>${project.name}</h4>
                                <p><strong>Description:</strong> ${project.description}</p>
                                <p><strong>Category:</strong> ${project.category}</p>
                                <p><strong>Start Date:</strong> ${new Date(project.startDate).toLocaleDateString()}</p>
                                <p><strong>End Date:</strong> ${new Date(project.endDate).toLocaleDateString()}</p>
                                <p><strong>Status:</strong> <span class="badge bg-${getStatusBadgeClass(project.status)}">${project.status}</span></p>
                                <p><strong>Requirements:</strong> ${project.requirements}</strong></p>
                                <p><strong>Progress:</strong> <span class="badge bg-primary">${project.completedTasks}/${project.totalTasks}</span></p>
                            </div>
                            <div class="col-md-6">
                                <h5>Task Statistics</h5>
                                <div class="d-flex gap-2 mb-3">
                                    <div class="badge bg-success">${project.completedTasks} Completed</div>
                                    <div class="badge bg-warning">${project.inProgressTasks} In Progress</div>
                                    <div class="badge bg-secondary">${project.pendingTasks} Pending</div>
                                </div>
                                <button class="btn btn-primary" onclick="showAddTaskModal(${project.id})">Add Task</button>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        new bootstrap.Modal(modal).show();
    } catch (error) {
        console.error('Error showing project details:', error);
        alert('Failed to load project details. Please try again.');
    }
}

// Show tasks for a project
async function showTasks(projectId) {
    try {
        const response = await fetch(`/api/projects/${projectId}/tasks`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch tasks');
        const tasks = await response.json();

        // Create and show tasks modal
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'tasksModal';
        modal.innerHTML = `
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Project Tasks</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Task Name</th>
                                    <th>Description</th>
                                    <th>Assigned To</th>
                                    <th>Status</th>
                                    <th>Priority</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="tasksTable"></tbody>
                        </table>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        const modalInstance = new bootstrap.Modal(modal);
        modalInstance.show();

        // Display tasks
        displayTasks(tasks);
    } catch (error) {
        console.error('Error showing tasks:', error);
        alert('Failed to load tasks. Please try again.');
    }
}

// Display tasks in table
function displayTasks(tasks) {
    const tableBody = document.getElementById('tasksTable');
    tableBody.innerHTML = '';

    tasks.forEach(task => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${task.name}</td>
            <td>${task.description}</td>
            <td>${task.employee?.name || 'Not assigned'}</td>
            <td><span class="badge bg-${getTaskStatusBadgeClass(task.status)}">${task.status}</span></td>
            <td>${task.priority}</td>
            <td>
                <button class="btn btn-sm btn-info me-1" onclick="showTaskDetails(${task.id})">Details</button>
                <button class="btn btn-sm btn-success me-1" onclick="assignTask(${task.id})">Assign</button>
                <button class="btn btn-sm btn-danger" onclick="deleteTask(${task.id})">Delete</button>
            </td>
        `;
        tableBody.appendChild(row);
    });
}

// Get task status badge class
function getTaskStatusBadgeClass(status) {
    const statusMap = {
        'ToDo': 'secondary',
        'InProgress': 'primary',
        'Review': 'warning',
        'Done': 'success'
    };
    return statusMap[status] || 'secondary';
}

// Show task details
async function showTaskDetails(taskId) {
    try {
        const response = await fetch(`/api/tasks/${taskId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch task details');
        const task = await response.json();

        // Create and show details modal
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'taskDetailsModal';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Task Details</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <h4>${task.name}</h4>
                        <p><strong>Description:</strong> ${task.description}</p>
                        <p><strong>Assigned To:</strong> ${task.employee?.name || 'Not assigned'}</p>
                        <p><strong>Status:</strong> <span class="badge bg-${getTaskStatusBadgeClass(task.status)}">${task.status}</span></p>
                        <p><strong>Priority:</strong> ${task.priority}</p>
                        <p><strong>Deadline:</strong> ${task.deadline ? new Date(task.deadline).toLocaleDateString() : 'Not set'}</p>
                        <p><strong>Review Comments:</strong> ${task.reviewComments || 'No comments'}</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        new bootstrap.Modal(modal).show();
    } catch (error) {
        console.error('Error showing task details:', error);
        alert('Failed to load task details. Please try again.');
    }
}

// Add task functionality
async function showAddTaskModal(projectId) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'addTaskModal';
    modal.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Add New Task</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <form id="addTaskForm">
                        <input type="hidden" id="projectId" value="${projectId}">
                        <div class="mb-3">
                            <label for="taskName" class="form-label">Task Name</label>
                            <input type="text" class="form-control" id="taskName" required>
                        </div>
                        <div class="mb-3">
                            <label for="taskDescription" class="form-label">Description</label>
                            <textarea class="form-control" id="taskDescription" rows="3" required></textarea>
                        </div>
                        <div class="mb-3">
                            <label for="priority" class="form-label">Priority</label>
                            <select class="form-select" id="priority" required>
                                <option value="High">High</option>
                                <option value="Medium">Medium</option>
                                <option value="Low">Low</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label for="deadline" class="form-label">Deadline</label>
                            <input type="date" class="form-control" id="deadline">
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    <button type="button" class="btn btn-primary" onclick="createTask()">Create Task</button>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    new bootstrap.Modal(modal).show();
}

// Create task
async function createTask() {
    const projectId = document.getElementById('projectId').value;
    const taskName = document.getElementById('taskName').value;
    const taskDescription = document.getElementById('taskDescription').value;
    const priority = document.getElementById('priority').value;
    const deadline = document.getElementById('deadline').value;

    try {
        const response = await fetch(`/api/projects/${projectId}/tasks`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: taskName,
                description: taskDescription,
                priority: priority,
                deadline: deadline
            })
        });

        if (!response.ok) throw new Error('Failed to create task');
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('addTaskModal'));
        modal.hide();
        
        // Refresh tasks
        showTasks(projectId);
    } catch (error) {
        console.error('Error creating task:', error);
        alert('Failed to create task. Please try again.');
    }
}

// Delete task
async function deleteTask(taskId) {
    if (!confirm('Are you sure you want to delete this task?')) return;

    try {
        const response = await fetch(`/api/tasks/${taskId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to delete task');
        
        // Refresh tasks
        const projectId = document.getElementById('projectId').value;
        showTasks(projectId);
    } catch (error) {
        console.error('Error deleting task:', error);
        alert('Failed to delete task. Please try again.');
    }
}

// Assign task to employee
async function assignTask(taskId) {
    try {
        // Get available employees
        const response = await fetch('/api/employees', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch employees');
        const employees = await response.json();

        // Create assignment modal
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'assignTaskModal';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Assign Task</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <form id="assignTaskForm">
                            <input type="hidden" id="taskId" value="${taskId}">
                            <div class="mb-3">
                                <label for="employee" class="form-label">Select Employee</label>
                                <select class="form-select" id="employee" required>
                                    ${employees.map(emp => `
                                        <option value="${emp.id}">${emp.name} (${emp.email})</option>
                                    `).join('')}
                                </select>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-primary" onclick="doAssignTask()">Assign</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        new bootstrap.Modal(modal).show();
    } catch (error) {
        console.error('Error assigning task:', error);
        alert('Failed to load employees. Please try again.');
    }
}

// Perform task assignment
async function doAssignTask() {
    const taskId = document.getElementById('taskId').value;
    const employeeId = document.getElementById('employee').value;

    try {
        const response = await fetch(`/api/tasks/${taskId}/assign`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ employeeId })
        });

        if (!response.ok) throw new Error('Failed to assign task');
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('assignTaskModal'));
        modal.hide();
        
        // Refresh tasks
        const projectId = document.getElementById('projectId').value;
        showTasks(projectId);
    } catch (error) {
        console.error('Error assigning task:', error);
        alert('Failed to assign task. Please try again.');
    }
}

// Get status badge class
function getStatusBadgeClass(status) {
    const statusMap = {
        'Not Started': 'secondary',
        'In Progress': 'primary',
        'On Hold': 'warning',
        'Completed': 'success'
    };
    return statusMap[status] || 'secondary';
}

async function viewProject(projectId) {
    try {
        const response = await fetch(`/api/project/${projectId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const project = await response.json();
            // Show project details in a modal or new page
            showProjectDetails(project);
        } else {
            const error = await response.json();
            showError('Failed to load project: ' + error.message);
        }
    } catch (error) {
        console.error('Error viewing project:', error);
        showError('Failed to view project');
    }
}

function showProjectDetails(project) {
    // Create a modal to show project details
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'projectDetailsModal';
    modal.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">${project.name}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <h6>Description:</h6>
                    <p>${project.description}</p>
                    <h6>Status: ${project.status}</h6>
                    <h6>Start Date: ${new Date(project.startDate).toLocaleDateString()}</h6>
                    <h6>End Date: ${new Date(project.endDate).toLocaleDateString()}</h6>
                    <h6>Tasks:</h6>
                    <div id="projectTasks"></div>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();

    // Load tasks
    loadProjectTasks(project.id);
}

async function loadProjectTasks(projectId) {
    try {
        const response = await fetch(`/api/project/${projectId}/tasks`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const tasks = await response.json();
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

function displayTasks(tasks) {
    const tasksDiv = document.getElementById('projectTasks');
    const table = document.createElement('table');
    table.className = 'table';

    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    ['Task Name', 'Status', 'Assigned To', 'Deadline', 'Actions'].forEach(headerText => {
        const th = document.createElement('th');
        th.textContent = headerText;
        headerRow.appendChild(th);
    });
    thead.appendChild(headerRow);
    table.appendChild(thead);

    const tbody = document.createElement('tbody');
    tasks.forEach(task => {
        const row = document.createElement('tr');
        
        const nameCell = document.createElement('td');
        nameCell.textContent = task.name;
        row.appendChild(nameCell);

        const statusCell = document.createElement('td');
        statusCell.textContent = task.status;
        row.appendChild(statusCell);

        const assignedToCell = document.createElement('td');
        assignedToCell.textContent = task.employee?.name || 'Not assigned';
        row.appendChild(assignedToCell);

        const deadlineCell = document.createElement('td');
        deadlineCell.textContent = task.deadline ? new Date(task.deadline).toLocaleDateString() : '';
        row.appendChild(deadlineCell);

        const actionsCell = document.createElement('td');
        const editBtn = document.createElement('button');
        editBtn.className = 'btn btn-sm btn-warning me-2';
        editBtn.textContent = 'Edit';
        editBtn.onclick = () => editTask(task.id);
        actionsCell.appendChild(editBtn);

        row.appendChild(actionsCell);
        tbody.appendChild(row);
    });

    table.appendChild(tbody);
    tasksDiv.innerHTML = '';
    tasksDiv.appendChild(table);
}

async function editProject(projectId) {
    try {
        const response = await fetch(`/api/project/${projectId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            const project = await response.json();
            // Populate form with project data
            document.getElementById('projectName').value = project.name;
            document.getElementById('projectDescription').value = project.description;
            document.getElementById('startDate').value = new Date(project.startDate).toISOString().split('T')[0];
            document.getElementById('endDate').value = new Date(project.endDate).toISOString().split('T')[0];

            // Show edit modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('addProjectModal'));
            modal.show();
        } else {
            const error = await response.json();
            showError('Failed to load project: ' + error.message);
        }
    } catch (error) {
        console.error('Error loading project:', error);
        showError('Failed to load project');
    }
}

async function saveProject() {
    const formData = {
        name: document.getElementById('projectName').value,
        description: document.getElementById('projectDescription').value,
        startDate: document.getElementById('startDate').value,
        endDate: document.getElementById('endDate').value
    };

    try {
        const method = document.getElementById('addProjectModal').dataset.projectId 
            ? 'PUT' 
            : 'POST';

        const response = await fetch('/api/project', {
            method: method,
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            // Close modal and reload projects
            const modal = bootstrap.Modal.getInstance(document.getElementById('addProjectModal'));
            modal.hide();
            loadProjects();
        } else {
            const error = await response.json();
            showError('Failed to save project: ' + error.message);
        }
    } catch (error) {
        console.error('Error saving project:', error);
        showError('Failed to save project');
    }
}

function showAddProjectModal() {
    // Clear form
    document.getElementById('projectName').value = '';
    document.getElementById('projectDescription').value = '';
    document.getElementById('startDate').value = '';
    document.getElementById('endDate').value = '';

    // Show modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('addProjectModal'));
    if (!modal) {
        new bootstrap.Modal(document.getElementById('addProjectModal')).show();
    } else {
        modal.show();
    }
}
