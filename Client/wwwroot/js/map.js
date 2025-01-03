let map;

window.initializeMap = function (events) {
    // Initialize the map
    map = L.map('map').setView([50.4501, 30.5234], 13); // Centered on Kyiv

    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // Add markers for each event
    events.forEach(event => {
        if (event.latitude && event.longitude) {
            const marker = L.marker([event.latitude, event.longitude])
                .addTo(map)
                .bindPopup(`
                    <div class="event-popup">
                        <h4>${event.title}</h4>
                        <p>${event.description}</p>
                        <p><strong>Date:</strong> ${new Date(event.date).toLocaleDateString()}</p>
                        <button onclick="window.location.href='/events/${event.id}'">
                            View Details
                        </button>
                    </div>
                `);

            // Add hover effect
            marker.on('mouseover', function (e) {
                this.openPopup();
            });
        }
    });
};
