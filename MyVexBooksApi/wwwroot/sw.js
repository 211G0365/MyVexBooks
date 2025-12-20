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
    "/",
    "fotoPerfil/perfil.png",      
    "img/logo.png",
    "img/fondo.jpg",
    "img/book.png",
    "img/book_120714.png",
    "img/busqueda.png",
    "img/editar.png",
    "img/heart-icon_34407.png",
    "img/home.png",
    "img/libro-abierto.png",
    "img/libro-cerrado.png",
    "img/likes.png",
    "img/lista.png",
    "img/lupa.png",
    "img/MyVEXBooks.png",
    "img/notificacion.png"
];

// INSTALACIÓN
async function descargarInstalar() {
    const cache = await caches.open("cacheLibros");
    await cache.addAll(urls);
    self.skipWaiting();
}

// OBTENER DESDE CACHE (Cache First + API dinámico)
async function obtenerDesdeCache(request) {
    const cache = await caches.open("cacheLibros");

    // API dinámica GET
    if (request.url.includes("myvexbooks2.duckdns.org/api/") && request.method === "GET") {
        try {
            const respuesta = await fetch(request);
            cache.put(request, respuesta.clone());
            return respuesta;
        } catch {
            const cached = await cache.match(request);
            if (cached) return cached;
            return new Response(JSON.stringify({ error: "Offline y sin datos cacheados" }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    // API dinámica POST, PUT, DELETE → no cachear
    if (request.url.includes("myvexbooks2.duckdns.org/api/")) {
        return fetch(request);
    }

    // Foto de perfil dinámica (REAL)
    if (request.url.includes("/fotoPerfil/")) {
        const cleanRequest = new Request(
            request.url.split("?")[0],
            { method: "GET" }
        );

        const cached = await cache.match(cleanRequest);
        if (cached) return cached;

        try {
            const response = await fetch(cleanRequest);
            cache.put(cleanRequest, response.clone());
            return response;
        } catch {
            const fallback = await cache.match("fotoPerfil/perfil.png");
            return fallback || new Response("", { status: 204 });
        }
    }



    
    if (request.destination === "image") {
        const cached = await cache.match(request);
        try {
            return cached || await fetch(request);
        } catch {
            return new Response("Imagen no disponible offline", { status: 503 });
        }
    }

    //  Cache First
    const respuestaCache = await cache.match(request);
    try {
        return respuestaCache || await fetch(request);
    } catch {
        return new Response("Archivo no disponible offline", { status: 503 });
    }
}

// install
self.addEventListener("install", function (event) {
    event.waitUntil(descargarInstalar());
});

// fetch
self.addEventListener("fetch", function (event) {
    event.respondWith(obtenerDesdeCache(event.request));
});

// notis
self.addEventListener("push", function (event) {
    event.waitUntil(mostrarNotificacion(event));
});

async function mostrarNotificacion(event) {
    if (!event.data) return;

    let data = event.data.json();
    const windows = await clients.matchAll({ type: "window" });
    const appVisible = windows.some(w => w.visibilityState === "visible");

    if (appVisible) {
        for (let w of windows) {
            if (w.visibilityState === "visible") {
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
    event.waitUntil(clients.openWindow("home.html"));
});
