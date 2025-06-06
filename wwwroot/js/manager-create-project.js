class ProjectCreation {
    constructor() {
        this.initializeEventListeners();
        this.loadEmployees();
    }

    initializeEventListeners() {
        // Form submission
        document.getElementById('projectForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.createProject();
        });

        // Date validation
        document.getElementById('startDate').addEventListener('change', () => this.validateDates());
        document.getElementById('endDate').addEventListener('change', () => this.validateDates());

        // Employee filters
        document.getElementById('skillsFilter').addEventListener('input', () => this.filterEmployees());
        document.getElementById('experienceFilter').addEventListener('input', () => this.filterEmployees());
        document.getElementById('performanceFilter').addEventListener('input', () => this.filterEmployees());
        document.getElementById('workloadFilter').addEventListener('input', () => this.filterEmployees());
    }

    async createProject() {
        try {
            const formData = {
                Name: document.getElementById('name').value,
                Description: document.getElementById('description').value,
                Requirements: document.getElementById('requirements').value,
                Category: document.getElementById('category').value,
                StartDate: document.getElementById('startDate').value,
                EndDate: document.getElementById('endDate').value,
                EstimatedHours: parseInt(document.getElementById('estimatedHours').value),
                EmployeeIds: Array.from(document.querySelectorAll('input[name="employeeIds"]:checked'))
                    .map(checkbox => checkbox.value)
            };

            const response = await fetch('/api/projects', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            if (!response.ok) throw new Error('Failed to create project');
            
            // Redirect to project details after creation
            window.location.href = '/manager/project/' + (await response.json()).id;
        } catch (error) {
            console.error('Error creating project:', error);
            this.showError('Failed to create project. Please try again.');
        }
    }

    async loadEmployees() {
        try {
            const response = await fetch('/api/employees/available');
            if (!response.ok) throw new Error('Failed to load employees');
            
            const employees = await response.json();
            this.renderEmployees(employees);
        } catch (error) {
            console.error('Error loading employees:', error);
            this.showError('Failed to load available employees. Please try again.');
        }
    }

    renderEmployees(employees) {
        const container = document.getElementById('employeeList');
        container.innerHTML = '';

        employees.forEach(employee => {
            const card = document.createElement('div');
            card.className = 'card mb-3';
            
            card.innerHTML = `
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <h5 class="card-title">${employee.name}</h5>
                            <p class="card-text">
                                <small class="text-muted">
                                    Skills: ${employee.skills.join(', ')}<br>
                                    Experience: ${employee.experience} years<br>
                                    Performance: ${employee.performance}%<br>
                                    Workload: ${employee.workload}%
                                </small>
                            </p>
                        </div>
                        <div class="form-check">
                            <input class="form-check-input" 
                                   type="checkbox" 
                                   name="employeeIds" 
                                   value="${employee.id}" 
                                   ${employee.isAvailable ? '' : 'disabled'}>
                            <label class="form-check-label">
                                ${employee.isAvailable ? 'Available' : 'Not Available'}
                            </label>
                        </div>
                    </div>
                </div>
            `;
            
            container.appendChild(card);
        });
    }

    validateDates() {
        const startDate = new Date(document.getElementById('startDate').value);
        const endDate = new Date(document.getElementById('endDate').value);

        if (startDate && endDate && startDate >= endDate) {
            this.showError('End date must be after start date');
            return false;
        }
        return true;
    }

    filterEmployees() {
        const skills = document.getElementById('skillsFilter').value.toLowerCase();
        const experience = parseInt(document.getElementById('experienceFilter').value) || 0;
        const performance = parseInt(document.getElementById('performanceFilter').value) || 0;
        const workload = parseInt(document.getElementById('workloadFilter').value) || 100;

        const cards = document.querySelectorAll('#employeeList .card');
        cards.forEach(card => {
            const employee = {
                skills: card.querySelector('.card-text small').textContent
                    .split('\n')[0].split(':')[1].trim().split(', '),
                experience: parseInt(card.querySelector('.card-text small').textContent
                    .split('\n')[1].split(':')[1].trim()),
                performance: parseInt(card.querySelector('.card-text small').textContent
                    .split('\n')[2].split(':')[1].trim()),
                workload: parseInt(card.querySelector('.card-text small').textContent
                    .split('\n')[3].split(':')[1].trim())
            };

            const matchesSkills = employee.skills.some(skill => 
                skill.toLowerCase().includes(skills)
            );
            const matchesExperience = employee.experience >= experience;
            const matchesPerformance = employee.performance >= performance;
            const matchesWorkload = employee.workload <= workload;

            card.style.display = matchesSkills && matchesExperience && matchesPerformance && matchesWorkload 
                ? '' : 'none';
        });
    }

    showError(message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-danger alert-dismissible fade show';
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const form = document.getElementById('projectForm');
        form.insertBefore(alertDiv, form.firstChild);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ProjectCreation();
});
