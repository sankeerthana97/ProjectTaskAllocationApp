class TeamLeadDashboard {
    constructor() {
        this.initializeEventListeners();
        this.loadProjects();
    }

    initializeEventListeners() {
        // Project selection
        document.getElementById('projectSelect').addEventListener('change', () => this.loadProjectTasks());

        // Create task modal
        const modal = new bootstrap.Modal(document.getElementById('createTaskModal'));
        document.getElementById('createTaskModal').addEventListener('show.bs.modal', () => this.loadAssignedEmployees());

        // Create task form submission
        document.getElementById('createTaskForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.createTask();
        });
    }

    async loadProjects() {
        try {
            const response = await fetch('/api/projects');
            if (!response.ok) throw new Error('Failed to load projects');
            
            const projects = await response.json();
            this.populateProjectSelect(projects);
        } catch (error) {
            console.error('Error loading projects:', error);
            this.showError('Failed to load projects. Please try again.');
        }
    }

    populateProjectSelect(projects) {
        const select = document.getElementById('projectSelect');
        select.innerHTML = '<option value="">Select a project...</option>';

        projects.forEach(project => {
            const option = document.createElement('option');
            option.value = project.id;
            option.textContent = `${project.name} (${project.category})`;
            select.appendChild(option);
        });
    }

    async loadAssignedEmployees() {
        try {
            const projectId = document.getElementById('projectSelect').value;
            if (!projectId) return;

            const response = await fetch(`/api/tasks/${projectId}/assigned-employees`);
            if (!response.ok) throw new Error('Failed to load employees');
            
            const employees = await response.json();
            this.populateEmployeeSelect(employees);
        } catch (error) {
            console.error('Error loading employees:', error);
            this.showError('Failed to load employees. Please try again.');
        }
    }

    populateEmployeeSelect(employees) {
        const select = document.getElementById('assignedTo');
        select.innerHTML = '<option value="">Select Employee</option>';

        employees.forEach(employee => {
            const option = document.createElement('option');
            option.value = employee.id;
            option.textContent = `${employee.name} (${employee.workload}% workload)`;
            select.appendChild(option);
        });
    }

    async loadProjectTasks() {
        try {
            const projectId = document.getElementById('projectSelect').value;
            if (!projectId) return;

            const response = await fetch(`/api/tasks`);
            if (!response.ok) throw new Error('Failed to load tasks');
            
            const tasks = await response.json();
            this.renderTaskList(tasks);
        } catch (error) {
            console.error('Error loading tasks:', error);
            this.showError('Failed to load tasks. Please try again.');
        }
    }

    renderTaskList(tasks) {
        const container = document.getElementById('taskList');
        container.innerHTML = '';

        if (tasks.length === 0) {
            container.innerHTML = '<p class="text-muted">No tasks in this project yet.</p>';
            return;
        }

        tasks.forEach(task => {
            const card = document.createElement('div');
            card.className = 'card mb-3';
            
            card.innerHTML = `
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <h6 class="card-title">${task.name}</h6>
                            <p class="card-text">
                                <small class="text-muted">
                                    Priority: ${task.priority}<br>
                                    Status: <span class="badge ${this.getTaskStatusClass(task.status)}">
                                        ${task.status}
                                    </span><br>
                                    Assigned To: ${task.assignedTo}
                                </small>
                            </p>
                        </div>
                        <div>
                            <button class="btn btn-sm ${this.getTaskActionClass(task.status)}" 
                                    onclick="handleTaskAction('${task.status}', ${task.id})">
                                ${this.getTaskActionText(task.status)}
                            </button>
                        </div>
                    </div>
                </div>
            `;
            
            container.appendChild(card);
        });
    }

    async createTask() {
        try {
            const projectId = document.getElementById('projectSelect').value;
            const name = document.getElementById('taskName').value;
            const description = document.getElementById('taskDescription').value;
            const priority = document.getElementById('taskPriority').value;
            const assignedTo = document.getElementById('assignedTo').value;

            const response = await fetch('/api/tasks', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    ProjectId: projectId,
                    Name: name,
                    Description: description,
                    Priority: priority,
                    AssignedToId: assignedTo
                })
            });

            if (!response.ok) throw new Error('Failed to create task');
            
            // Reload tasks after creation
            this.loadProjectTasks();
            bootstrap.Modal.getInstance(document.getElementById('createTaskModal')).hide();
            
            this.showSuccess('Task created successfully');
        } catch (error) {
            console.error('Error creating task:', error);
            this.showError('Failed to create task. Please try again.');
        }
    }

    getTaskStatusClass(status) {
        switch (status.toLowerCase()) {
            case 'todo': return 'bg-secondary';
            case 'inprogress': return 'bg-primary';
            case 'review': return 'bg-warning';
            case 'done': return 'bg-success';
            default: return 'bg-secondary';
        }
    }

    getTaskActionClass(status) {
        switch (status.toLowerCase()) {
            case 'todo': return 'btn-primary';
            case 'inprogress': return 'btn-success';
            case 'review': return 'btn-warning';
            default: return 'btn-secondary';
        }
    }

    getTaskActionText(status) {
        switch (status.toLowerCase()) {
            case 'todo': return 'Start Task';
            case 'inprogress': return 'Complete Task';
            case 'review': return 'Waiting for Review';
            default: return 'View Task';
        }
    }

    showError(message) {
        this.showAlert('alert-danger', message);
    }

    showSuccess(message) {
        this.showAlert('alert-success', message);
    }

    showAlert(type, message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert ${type} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const container = document.querySelector('.container');
        container.insertBefore(alertDiv, container.firstChild);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new TeamLeadDashboard();
});
