class Timeline {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.events = [];
    }

    async loadProjectTimeline(projectId) {
        try {
            const response = await fetch(`/api/timeline/projects/${projectId}`);
            if (!response.ok) throw new Error('Failed to load timeline');
            
            this.events = await response.json();
            this.render();
        } catch (error) {
            console.error('Error loading timeline:', error);
            this.showError('Failed to load timeline. Please try again.');
        }
    }

    render() {
        this.container.innerHTML = '';
        
        if (this.events.length === 0) {
            this.container.innerHTML = '<p class="text-muted">No timeline events yet.</p>';
            return;
        }

        this.events.forEach(event => {
            const item = document.createElement('div');
            item.className = 'timeline-item';
            
            item.innerHTML = `
                <div class="timeline-event" style="border-left-color: ${event.color}">
                    <div class="timeline-event-header">
                        <h6>${event.event}</h6>
                        <small class="text-muted">${new Date(event.date).toLocaleString()}</small>
                    </div>
                    <p>${event.description}</p>
                    ${event.comments ? `<p class="text-muted">${event.comments}</p>` : ''}
                    <div class="timeline-event-footer">
                        <small class="text-muted">By ${event.changedBy}</small>
                    </div>
                </div>
            `;
            
            this.container.appendChild(item);
        });
    }

    showError(message) {
        this.container.innerHTML = `
            <div class="alert alert-danger">
                ${message}
            </div>
        `;
    }
}
