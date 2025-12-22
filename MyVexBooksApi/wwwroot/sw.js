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

const CACHE_NAME = "cacheLibros";
const DB_NAME = "likesDB";
const STORE_NAME = "pendientes";


function limpiarURL(request) {
    const url = new URL(request.url);
    url.search = "";
    return url.toString();
}


function abrirDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, 1);

        req.onupgradeneeded = e => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME, {
                    keyPath: "id",
                    autoIncrement: true
                });
            }
        };

        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}


async function descargarInstalar() {
    const cache = await caches.open(CACHE_NAME);
    await cache.addAll(urls);
    self.skipWaiting();
}

async function obtenerToken() {
    if (tokenGlobal) return tokenGlobal;

    const allClients = await clients.matchAll({ type: "window" });
    for (const client of allClients) {
        client.postMessage({ tipo: "PEDIR_TOKEN" });
    }

    return new Promise(resolve => {
        const interval = setInterval(() => {
            if (tokenGlobal) {
                clearInterval(interval);
                resolve(tokenGlobal);
            }
        }, 100);
    });
}




async function obtenerDesdeCache(request) {
    const cache = await caches.open(CACHE_NAME);


    if (
        request.method === "POST" &&
        request.url.includes("/api/Usuarios/perfil/foto")
    ) {
        return fetch(request);
    }

 
    if (
        request.method === "POST" &&
        request.url.includes("/api/Libros/parte/") &&
        request.url.includes("/like")
    ) {
        try {
            return await fetch(request);
        } catch {
            const idParte = request.url.split("/parte/")[1].split("/")[0];

            const db = await abrirDB();
            const tx = db.transaction(STORE_NAME, "readwrite");
            tx.objectStore(STORE_NAME).add({
                idParte,
                accion: "toggle", 
                fecha: Date.now()
            });


  
            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-likes");
            }

            return new Response(
                JSON.stringify({ offline: true }),
                { headers: { "Content-Type": "application/json" } }
            );
        }
    }

    
    if (
        request.url.includes("myvexbooks2.duckdns.org/api/") &&
        request.method === "GET"
    ) {
        try {
            const response = await fetch(request);
            await cache.put(request, response.clone());
            return response;
        } catch {
            const cached = await cache.match(request);
            if (cached) return cached;
            return new Response(
                JSON.stringify({ error: "Offline y sin datos cacheados" }),
                { headers: { "Content-Type": "application/json" } }
            );
        }
    }


    if (request.url.includes("myvexbooks2.duckdns.org/api/")) {
        return fetch(request);
    }

 
    if (request.url.includes("/fotoPerfil/usuario_")) {
        const urlLimpia = limpiarURL(request);

        try {
            const response = await fetch(urlLimpia, { cache: "no-store" });

            const keys = await cache.keys();
            for (const req of keys) {
                if (req.url.includes("/fotoPerfil/usuario_")) {
                    await cache.delete(req);
                }
            }

            await cache.put(urlLimpia, response.clone());
            return response;
        } catch {
            const keys = await cache.keys();
            const ultimaFoto = keys.find(k =>
                k.url.includes("/fotoPerfil/usuario_")
            );

            if (ultimaFoto) return cache.match(ultimaFoto);
            return cache.match("fotoPerfil/perfil.png");
        }
    }

    
    if (request.destination === "image") {
        const cached = await cache.match(limpiarURL(request));
        try {
            return cached || await fetch(request);
        } catch {
            return cached || cache.match("fotoPerfil/perfil.png");
        }
    }

    
    const cached = await cache.match(request);
    try {
        return cached || await fetch(request);
    } catch {
        return new Response("Archivo no disponible offline", { status: 503 });
    }
}


self.addEventListener("install", event => {
    event.waitUntil(descargarInstalar());
});


self.addEventListener("fetch", event => {
    event.respondWith(obtenerDesdeCache(event.request));
});


self.addEventListener("sync", event => {
    if (event.tag === "sync-likes") {
        event.waitUntil(enviarLikesPendientes());
    }
});

let tokenGlobal = null;

self.addEventListener("message", event => {
    if (event.data?.tipo === "TOKEN") {
        tokenGlobal = event.data.token;
    }
});


async function enviarLikesPendientes() {
    const token = await obtenerToken();
    if (!token) return;

    const db = await abrirDB();
    const tx = db.transaction(STORE_NAME, "readonly");
    const store = tx.objectStore(STORE_NAME);

    const likes = await new Promise((resolve, reject) => {
        const req = store.getAll();
        req.onsuccess = () => resolve(req.result || []);
        req.onerror = () => reject(req.error);
    });

    if (!likes.length) return;

    for (const like of likes) {
        try {
            await fetch(
                `https://myvexbooks2.duckdns.org/api/Libros/parte/${like.idParte}/like`,
                {
                    method: "POST",
                    headers: {
                        "Authorization": `Bearer ${token}`
                    }
                }
            );
        } catch (e) {
            console.warn("Aún offline, no se pueden enviar", e);
            return; 
        }
    }

    const clearTx = db.transaction(STORE_NAME, "readwrite");
    clearTx.objectStore(STORE_NAME).clear();
}




self.addEventListener("push", event => {
    event.waitUntil(mostrarNotificacion(event));
});

async function mostrarNotificacion(event) {
    if (!event.data) return;

    const data = event.data.json();
    const windows = await clients.matchAll({ type: "window" });
    const appVisible = windows.some(w => w.visibilityState === "visible");

    if (appVisible) {
        for (const w of windows) {
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
            body: data.mensaje
        });
    }
}

self.addEventListener("notificationclick", event => {
    event.notification.close();
    event.waitUntil(clients.openWindow("home.html"));
});
