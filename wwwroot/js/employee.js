class EmployeeProfile {
    constructor() {
        this.initializeEventListeners();
        this.loadEmployeeData();
    }

    initializeEventListeners() {
        document.getElementById('profileForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveProfile();
        });
    }

    async loadEmployeeData() {
        try {
            const response = await fetch('/api/employees/me');
            if (!response.ok) throw new Error('Failed to load employee data');
            
            const employee = await response.json();
            this.populateForm(employee);
            this.updateStatusDisplay(employee);
        } catch (error) {
            console.error('Error loading employee data:', error);
            this.showError('Failed to load profile data. Please try again.');
        }
    }

    populateForm(employee) {
        document.getElementById('name').value = employee.name;
        document.getElementById('skills').value = employee.skills.join(', ');
        document.getElementById('experience').value = employee.experience;
        document.getElementById('bio').value = employee.bio || '';
    }

    updateStatusDisplay(employee) {
        const performance = employee.performance || 0;
        const workload = employee.workload || 0;
        const isAvailable = employee.isAvailable || false;

        // Update performance
        const performanceBar = document.getElementById('performanceBar');
        performanceBar.style.width = `${performance}%`;
        document.getElementById('performanceText').textContent = `Performance: ${performance}%`;

        // Update workload
        const workloadBar = document.getElementById('workloadBar');
        workloadBar.style.width = `${workload}%`;
        workloadBar.className = `progress-bar ${this.getWorkloadColorClass(workload)}`;
        document.getElementById('workloadText').textContent = `Workload: ${workload}%`;

        // Update availability
        const availabilityBadge = document.getElementById('availabilityBadge');
        const availabilityText = document.getElementById('availabilityText');
        
        if (isAvailable) {
            availabilityBadge.className = 'badge bg-success';
            availabilityBadge.textContent = 'Available';
            availabilityText.textContent = 'Employee is available for new projects';
        } else {
            availabilityBadge.className = 'badge bg-danger';
            availabilityBadge.textContent = 'Not Available';
            availabilityText.textContent = 'Employee is not available for new projects';
        }
    }

    getWorkloadColorClass(workload) {
        if (workload >= 80) return 'bg-danger';
        if (workload >= 50) return 'bg-warning';
        return 'bg-success';
    }

    async saveProfile() {
        try {
            const formData = {
                Name: document.getElementById('name').value,
                Skills: JSON.stringify(document.getElementById('skills').value.split(',').map(skill => skill.trim())),
                Experience: parseInt(document.getElementById('experience').value),
                Bio: document.getElementById('bio').value
            };

            const response = await fetch('/api/employees/me', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            if (!response.ok) throw new Error('Failed to save profile');
            
            const updatedEmployee = await response.json();
            this.updateStatusDisplay(updatedEmployee);
            this.showSuccess('Profile updated successfully');
        } catch (error) {
            console.error('Error saving profile:', error);
            this.showError('Failed to save profile. Please try again.');
        }
    }

    showSuccess(message) {
        this.showAlert('alert-success', message);
    }

    showError(message) {
        this.showAlert('alert-danger', message);
    }

    showAlert(type, message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert ${type} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const form = document.getElementById('profileForm');
        form.insertBefore(alertDiv, form.firstChild);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new EmployeeProfile();
});
