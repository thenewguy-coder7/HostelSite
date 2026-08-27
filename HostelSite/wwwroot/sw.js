// Prosbee service worker.
// Scope: site root. Registered on every page (see _Layout.cshtml) so the
// site can be installed to a phone's home screen; only the Admin Dashboard
// actually subscribes it to push notifications.

self.addEventListener('install', function () {
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(self.clients.claim());
});

// A fetch handler — even one that just passes every request straight through
// to the network with no offline caching — is part of what Chrome/Android
// checks before it will treat the site as installable.
self.addEventListener('fetch', function (event) {
    event.respondWith(fetch(event.request));
});

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
