let urls = [
    "index.html",
    "busqueda.html",
    "estilos.css",
    "generos.html",
    "home.html",
    "lectura.html",
    "libro.html",
    "notificaciones.html",
    "perfil.html",
    "registrarse.html",
    "/"
];

// INSTALACIÓN
async function descargarInstalar() {
    let cache = await caches.open("cacheLibros");
    await cache.addAll(urls);
    self.skipWaiting();
}

// OBTENER DESDE CACHE (Cache First + API dinámico)
async function obtenerDesdeCache(request) {
    const cache = await caches.open("cacheLibros");

    // Si es API dinámica
    if (request.url.includes("myvexbooks2.duckdns.org/api/") && request.method === "GET") {
        try {
            const respuesta = await fetch(request);
            // Guardar solo GETs
            cache.put(request, respuesta.clone());
            return respuesta;
        } catch (err) {
            const cached = await cache.match(request);
            if (cached) return cached;
            return new Response(JSON.stringify({ error: "Offline y sin datos cacheados" }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    } else if (request.url.includes("myvexbooks2.duckdns.org/api/") && request.method !== "GET") {
        // No cachear POST, PUT, DELETE
        return fetch(request);
    }

    // Archivos estáticos → Cache First
    const respuestaCache = await cache.match(request);
    return respuestaCache || fetch(request);
}

// EVENTO INSTALL
self.addEventListener("install", function (event) {
    event.waitUntil(descargarInstalar());
});

// EVENTO FETCH
self.addEventListener("fetch", function (event) {
    event.respondWith(obtenerDesdeCache(event.request));
});







self.addEventListener("push", function (event) {
    event.waitUntil(mostrarNotificacion(event));
});

async function mostrarNotificacion(event) {
    if (!event.data) return;

    let data = event.data.json();

    const windows = await clients.matchAll({ type: "window" });
    const appVisible = windows.some(w => w.visibilityState == "visible");

    if (appVisible) {
      
        for (let w of windows) {
            if (w.visibilityState == "visible") {
                w.postMessage({
                    tipo: "RECIBIDA",
                    titulo: data.titulo,
                    mensaje: data.mensaje
                });
            }
        }
    } else {
     
        await self.registration.showNotification(data.titulo, {
            body: data.mensaje,
            data: {} 
        });
    }
}

self.addEventListener("notificationclick", function (event) {
    event.notification.close();

    event.waitUntil(
        clients.openWindow("Home.html")
    );
});
