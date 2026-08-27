// Prosbee admin push notifications service worker.
// Scope: site root, but only the Admin Dashboard ever subscribes to it.

self.addEventListener('push', function (event) {
    let data = { title: 'Prosbee', body: 'You have a new update.', url: '/Admin/Dashboard' };
    try {
        if (event.data) data = event.data.json();
    } catch (e) {
        // Fall back to the default above if the payload wasn't JSON
    }

    const options = {
        body: data.body,
        icon: '/images/prosbee-icon.png',
        badge: '/images/prosbee-icon.png',
        data: { url: data.url || '/Admin/Dashboard' }
    };

    event.waitUntil(self.registration.showNotification(data.title, options));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const targetUrl = (event.notification.data && event.notification.data.url) || '/Admin/Dashboard';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            for (const client of clientList) {
                if (client.url.includes(targetUrl) && 'focus' in client) {
                    return client.focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow(targetUrl);
            }
        })
    );
});
