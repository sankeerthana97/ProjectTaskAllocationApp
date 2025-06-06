class ManagerDashboard {
    constructor() {
        this.initializeEventListeners();
        this.loadProjects();
        this.loadEmployees();
        this.loadTasksToReview();
    }

    initializeEventListeners() {
        // Project table sorting
        document.querySelectorAll('.sort-column').forEach(header => {
            header.addEventListener('click', () => this.sortProjects(header.dataset.column));
        });

        // Employee filter
        document.getElementById('employeeFilter').addEventListener('input', () => this.filterEmployees());

        // Task review actions
        document.querySelectorAll('.review-task').forEach(button => {
            button.addEventListener('click', () => this.handleTaskReview(button.dataset.taskId));
        });
    }

    async loadProjects() {
        try {
            const response = await fetch('/api/projects');
            if (!response.ok) throw new Error('Failed to load projects');
            
            const projects = await response.json();
            this.renderProjects(projects);
        } catch (error) {
            console.error('Error loading projects:', error);
            this.showError('Failed to load projects. Please try again.');
        }
    }

    renderProjects(projects) {
        const tbody = document.getElementById('projectsTableBody');
        tbody.innerHTML = '';

        projects.forEach(project => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${project.name}</td>
                <td>${project.category}</td>
                <td>
                    <span class="badge ${this.getProjectStatusClass(project.status)}">
                        ${project.status}
                    </span>
                </td>
                <td>
                    <div class="progress">
                        <div class="progress-bar" 
                             role="progressbar" 
                             style="width: ${project.progress}%" 
                             aria-valuenow="${project.progress}" 
                             aria-valuemin="0" 
                             aria-valuemax="100">
                            ${project.progress}%
                        </div>
                    </div>
                </td>
                <td>
                    <button class="btn btn-sm btn-primary" 
                            onclick="window.location.href='/manager/project/${project.id}'">
                        View
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
    }

    getProjectStatusClass(status) {
        switch (status.toLowerCase()) {
            case 'active': return 'bg-primary';
            case 'completed': return 'bg-success';
            case 'delayed': return 'bg-warning';
            case 'cancelled': return 'bg-danger';
            default: return 'bg-secondary';
        }
    }

    async loadEmployees() {
        try {
            const response = await fetch('/api/employees');
            if (!response.ok) throw new Error('Failed to load employees');
            
            const employees = await response.json();
            this.renderEmployeeOverview(employees);
        } catch (error) {
            console.error('Error loading employees:', error);
            this.showError('Failed to load employee overview. Please try again.');
        }
    }

    renderEmployeeOverview(employees) {
        const container = document.getElementById('employeeOverview');
        container.innerHTML = '';

        employees.forEach(employee => {
            const card = document.createElement('div');
            card.className = 'card mb-3';
            
            card.innerHTML = `
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <h6 class="card-title mb-0">${employee.name}</h6>
                            <small class="text-muted">
                                ${employee.skills.join(', ')}<br>
                                ${employee.experience} years experience
                            </small>
                        </div>
                        <div>
                            <div class="progress mb-2" style="height: 6px">
                                <div class="progress-bar ${this.getWorkloadColorClass(employee.workload)}" 
                                     role="progressbar" 
                                     style="width: ${employee.workload}%">
                                </div>
                            </div>
                            <span class="badge ${this.getAvailabilityClass(employee.isAvailable)}">
                                ${employee.isAvailable ? 'Available' : 'Not Available'}
                            </span>
                        </div>
                    </div>
                </div>
            `;
            
            container.appendChild(card);
        });
    }

    getWorkloadColorClass(workload) {
        if (workload >= 80) return 'bg-danger';
        if (workload >= 50) return 'bg-warning';
        return 'bg-success';
    }

    getAvailabilityClass(isAvailable) {
        return isAvailable ? 'bg-success' : 'bg-danger';
    }

    async loadTasksToReview() {
        try {
            const response = await fetch('/api/tasks/to-review');
            if (!response.ok) throw new Error('Failed to load tasks to review');
            
            const tasks = await response.json();
            this.renderTasksToReview(tasks);
        } catch (error) {
            console.error('Error loading tasks to review:', error);
            this.showError('Failed to load tasks to review. Please try again.');
        }
    }

    renderTasksToReview(tasks) {
        const container = document.getElementById('tasksToReview');
        container.innerHTML = '';

        if (tasks.length === 0) {
            container.innerHTML = '<p class="text-muted">No tasks ready for review.</p>';
            return;
        }

        tasks.forEach(task => {
            const card = document.createElement('div');
            card.className = 'card mb-3';
            
            card.innerHTML = `
                <div class="card-body">
                    <h6 class="card-title">${task.name}</h6>
                    <p class="card-text">
                        <small class="text-muted">
                            Project: ${task.projectName}<br>
                            Employee: ${task.assignedTo}<br>
                            Priority: ${task.priority}
                        </small>
                    </p>
                    <div class="d-flex justify-content-end">
                        <button class="btn btn-sm btn-success me-2" 
                                onclick="reviewTask(${task.id}, true)">
                            Accept
                        </button>
                        <button class="btn btn-sm btn-danger" 
                                onclick="reviewTask(${task.id}, false)">
                            Reject
                        </button>
                    </div>
                </div>
            `;
            
            container.appendChild(card);
        });
    }

    sortProjects(column) {
        // Implementation for sorting projects
    }

    filterEmployees() {
        // Implementation for filtering employees
    }

    handleTaskReview(taskId) {
        // Implementation for handling task review
    }

    showError(message) {
        // Implementation for showing error messages
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ManagerDashboard();
});
