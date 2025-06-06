document.addEventListener('DOMContentLoaded', function() {
    // Initialize user name
    const token = localStorage.getItem('token');
    if (token) {
        const decodedToken = JSON.parse(atob(token.split('.')[1]));
        document.getElementById('userName').textContent = decodedToken.name;
    }

// Initialize dashboard
async function initializeDashboard() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = '/login';
            return;
        }

        // Get user info
        const userInfo = await fetchUserInfo();
        if (!userInfo) return;

        // Get projects
        const projects = await fetchProjects();
        if (projects) {
            updateProjectStats(projects);
            displayProjects(projects);
        }

        // Get team leads
        await loadTeamLeads();

        // Set up event listeners
        document.getElementById('logoutBtn').addEventListener('click', logout);
    } catch (error) {
        console.error('Error initializing dashboard:', error);
        alert('Failed to load dashboard. Please try again.');
    }
}

// Load team leads for project creation
async function loadTeamLeads() {
    try {
        const response = await fetch('/api/users/role/TeamLead', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch team leads');
        const teamLeads = await response.json();

        const select = document.getElementById('teamLead');
        select.innerHTML = '<option value="">Select Team Lead</option>';
        teamLeads.forEach(lead => {
            const option = document.createElement('option');
            option.value = lead.id;
            option.textContent = `${lead.name} (${lead.email})`;
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading team leads:', error);
        alert('Failed to load team leads. Please try again.');
    }
}

// Create project
async function createProject() {
    const projectName = document.getElementById('projectName').value;
    const projectDescription = document.getElementById('projectDescription').value;
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;
    const category = document.getElementById('category').value;
    const teamLeadId = document.getElementById('teamLead').value;
    const requirements = document.getElementById('requirements').value;

    try {
        const response = await fetch('/api/projects', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: projectName,
                description: projectDescription,
                startDate: startDate,
                endDate: endDate,
                category: category,
                teamLeadId: teamLeadId,
                requirements: requirements
            })
        });

        if (!response.ok) throw new Error('Failed to create project');
        
        // Close modal and refresh projects
        const modal = bootstrap.Modal.getInstance(document.getElementById('createProjectModal'));
        modal.hide();
        
        // Reset form
        document.getElementById('createProjectForm').reset();
        
        // Refresh projects
        fetchProjects().then(projects => {
            updateProjectStats(projects);
            displayProjects(projects);
        });
    } catch (error) {
        console.error('Error creating project:', error);
        alert('Failed to create project. Please try again.');
    }
}

// Update project statistics
function updateProjectStats(projects) {
    const stats = {
        total: projects.length,
        completed: 0,
        active: 0,
        delayed: 0
    };

    projects.forEach(project => {
        if (project.status === 'Completed') stats.completed++;
        else if (project.status === 'Active') stats.active++;
        
        // Check for delayed projects
        if (project.status !== 'Completed' && 
            project.endDate && 
            new Date(project.endDate) < new Date()) {
            stats.delayed++;
        }
    });

    // Update DOM elements
    document.getElementById('totalProjects').textContent = stats.total;
    document.getElementById('completedProjects').textContent = stats.completed;
    document.getElementById('activeProjects').textContent = stats.active;
    document.getElementById('delayedProjects').textContent = stats.delayed;
}

// Display projects in table
function displayProjects(projects) {
    const tableBody = document.getElementById('projectsTable');
    tableBody.innerHTML = '';

    projects.forEach(project => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${project.name}</td>
            <td>${project.teamLead?.name || 'Not assigned'}</td>
            <td>${project.category}</td>
            <td>${new Date(project.startDate).toLocaleDateString()}</td>
            <td>${new Date(project.endDate).toLocaleDateString()}</td>
            <td><span class="badge bg-${getStatusBadgeClass(project.status)}">${project.status}</span></td>
            <td>
                <div class="d-flex align-items-center">
                    <div class="progress me-2" style="width: 100px;">
                        <div class="progress-bar" role="progressbar" style="width: ${project.progress}%;"></div>
                    </div>
                    <span class="badge bg-primary">${project.progress}%</span>
                </div>
            </td>
            <td>
                <button class="btn btn-sm btn-info me-1" onclick="showProjectDetails(${project.id})">Details</button>
                <button class="btn btn-sm btn-primary me-1" onclick="showTeamLeadPerformance(${project.teamLeadId})">Performance</button>
                <button class="btn btn-sm btn-danger" onclick="deleteProject(${project.id})">Delete</button>
            </td>
        `;
        tableBody.appendChild(row);
    });
}

// Get status badge class
function getStatusBadgeClass(status) {
    const statusMap = {
        'Active': 'success',
        'Completed': 'primary',
        'OnHold': 'warning',
        'Cancelled': 'danger'
    };
    return statusMap[status] || 'secondary';
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
                                <p><strong>Progress:</strong> ${project.progress}%</p>
                                <p><strong>Requirements:</strong> ${project.requirements}</strong></p>
                            </div>
                            <div class="col-md-6">
                                <h5>Team Lead</h5>
                                <p><strong>Name:</strong> ${project.teamLead?.name}</p>
                                <p><strong>Email:</strong> ${project.teamLead?.email}</p>
                                <p><strong>Performance:</strong> ${project.teamLead?.performance}%</p>
                                <p><strong>Workload:</strong> ${project.teamLead?.workload}%</p>
                                <button class="btn btn-primary mt-3" onclick="showTeamLeadPerformance('${project.teamLeadId}')">View Detailed Performance</button>
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

// Show team lead performance
async function showTeamLeadPerformance(teamLeadId) {
    try {
        const response = await fetch(`/api/users/${teamLeadId}/performance`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch performance data');
        const performance = await response.json();

        // Update performance modal
        const performanceBar = document.getElementById('performanceBar');
        const performanceText = document.getElementById('performanceText');
        const workloadBar = document.getElementById('workloadBar');
        const workloadText = document.getElementById('workloadText');

        performanceBar.style.width = `${performance.performance}%`;
        performanceText.textContent = `Performance: ${performance.performance}%`;
        workloadBar.style.width = `${performance.workload}%`;
        workloadText.textContent = `Workload: ${performance.workload}%`;

        // Update project statistics
        const completedProjectsCount = document.getElementById('completedProjectsCount');
        const activeProjectsCount = document.getElementById('activeProjectsCount');
        const totalProjectsCount = document.getElementById('totalProjectsCount');
        const delayedProjectsCount = document.getElementById('delayedProjectsCount');

        completedProjectsCount.textContent = performance.completedProjects;
        activeProjectsCount.textContent = performance.activeProjects;
        totalProjectsCount.textContent = performance.totalProjects;
        delayedProjectsCount.textContent = performance.delayedProjects;

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('performanceModal'));
        modal.show();
    } catch (error) {
        console.error('Error showing performance:', error);
        alert('Failed to load performance data. Please try again.');
    }
}

// Delete project
async function deleteProject(projectId) {
    if (!confirm('Are you sure you want to delete this project? This action cannot be undone.')) return;

    try {
        const response = await fetch(`/api/projects/${projectId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to delete project');
        
        // Refresh projects
        fetchProjects().then(projects => {
            updateProjectStats(projects);
            displayProjects(projects);
        });
    } catch (error) {
        console.error('Error deleting project:', error);
        alert('Failed to delete project. Please try again.');
    }
}

// Fetch projects from API
async function fetchProjects() {
    try {
        const response = await fetch('/api/projects', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch projects');
        return await response.json();
    } catch (error) {
        console.error('Error fetching projects:', error);
        alert('Failed to load projects. Please try again.');
        return [];
    }
}

// Fetch user info from API
async function fetchUserInfo() {
    try {
        const response = await fetch('/api/users/me', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (!response.ok) throw new Error('Failed to fetch user info');
        return await response.json();
    } catch (error) {
        console.error('Error fetching user info:', error);
        return null;
    }
}

// Logout function
async function logout() {
    try {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/login';
    } catch (error) {
        console.error('Error logging out:', error);
        alert('Failed to logout. Please try again.');
    }
}

// Initialize user name
const token = localStorage.getItem('token');
if (token) {
    const decodedToken = JSON.parse(atob(token.split('.')[1]));
    document.getElementById('userName').textContent = decodedToken.name;
}

// Load projects
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
    ['Project Name', 'Status', 'Start Date', 'End Date', 'Team Lead', 'Actions'].forEach(headerText => {
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

        // Team Lead
        const teamLeadCell = document.createElement('td');
        teamLeadCell.textContent = project.teamLead?.name || 'Not assigned';
        row.appendChild(teamLeadCell);

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

        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn btn-sm btn-danger';
        deleteBtn.textContent = 'Delete';
        deleteBtn.onclick = () => deleteProject(project.id);
        actionsCell.appendChild(deleteBtn);

        row.appendChild(actionsCell);
        tbody.appendChild(row);
    });

    table.appendChild(tbody);
    document.getElementById('projectsTable').innerHTML = '';
    document.getElementById('projectsTable').appendChild(table);
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'alert alert-danger';
    errorDiv.textContent = message;
    document.getElementById('projectsTable').innerHTML = '';
    document.getElementById('projectsTable').appendChild(errorDiv);
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
        } else {
            const error = await response.json();
            showError('Failed to load project: ' + error.message);
        }
    } catch (error) {
        console.error('Error viewing project:', error);
        showError('Failed to view project');
    }
}

async function editProject(projectId) {
    // Implement edit project functionality
}

async function deleteProject(projectId) {
    if (!confirm('Are you sure you want to delete this project?')) {
        return;
    }

    try {
        const response = await fetch(`/api/project/${projectId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.ok) {
            // Reload projects list
            loadProjects();
        } else {
            const error = await response.json();
            showError('Failed to delete project: ' + error.message);
        }
    } catch (error) {
        console.error('Error deleting project:', error);
        showError('Failed to delete project');
    }
}
