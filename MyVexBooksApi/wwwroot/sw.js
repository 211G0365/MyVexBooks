/* =========================
   ARCHIVOS PRECACHE
========================= */
const urls = [
    "offline.html",
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

/* =========================
   CACHES & DB
========================= */
const CACHE_NAME = "cache-libros-v3";
const FOTO_CACHE = "foto-perfil-cache-v2";

const DB_NAME = "likesDB";
const STORE_NAME = "pendientes";

const PROFILE_DB = "perfilDB";
const PROFILE_QUEUE = "perfilPendiente";

const FOTO_DB = "fotoDB";
const FOTO_STORE = "fotoPendiente";

let tokenGlobal = null;

/* =========================
   INSTALL
========================= */
self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(urls))
    );
    self.skipWaiting();
});

/* =========================
   ACTIVATE
========================= */
self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys.map(k => {
                    if (k !== CACHE_NAME && k !== FOTO_CACHE) {
                        return caches.delete(k);
                    }
                })
            )
        )
    );
    self.clients.claim();
});

/* =========================
   FETCH
========================= */
self.addEventListener("fetch", event => {
    event.respondWith(manejarFetch(event.request));
});

async function manejarFetch(request) {
    const cache = await caches.open(CACHE_NAME);

    /* ===== API GET → cache first ===== */
    if (request.method === "GET" && request.url.includes("/api/")) {
        const cached = await cache.match(request);
        if (cached) return cached;

        try {
            const response = await fetch(request);
            cache.put(request, response.clone());
            return response;
        } catch {
            return new Response(JSON.stringify([]), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    /* ===== LIKE OFFLINE ===== */
    if (request.method === "POST" && request.url.includes("/like")) {
        try {
            return await fetch(request);
        } catch {
            const idParte = request.url.split("/parte/")[1].split("/")[0];
            const db = await abrirLikesDB();
            const tx = db.transaction(STORE_NAME, "readwrite");
            tx.objectStore(STORE_NAME).add({ idParte, fecha: Date.now() });

            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-likes");
            }

            return new Response(JSON.stringify({ offline: true }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    /* ===== PERFIL PUT OFFLINE ===== */
    if (request.method === "PUT" && request.url.includes("/api/Usuarios/perfil")) {
        try {
            return await fetch(request);
        } catch {
            const body = await request.clone().json();
            const campo = request.url.includes("nombre") ? "nombre" : "correo";

            const db = await abrirPerfilDB();
            const tx = db.transaction(PROFILE_QUEUE, "readwrite");
            tx.objectStore(PROFILE_QUEUE).add({
                campo,
                valor: body.Nombre || body.Correo
            });

            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-perfil");
            }

            return new Response(JSON.stringify({ offline: true }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    /* ===== IMÁGENES PERFIL ===== */
    if (request.destination === "image" && request.url.includes("/fotoPerfil/")) {
        const fotoCache = await caches.open(FOTO_CACHE);
        const cached = await fotoCache.match(request);
        if (cached) return cached;

        try {
            const response = await fetch(request);
            fotoCache.put(request, response.clone());
            return response;
        } catch {
            return cache.match("fotoPerfil/perfil.png");
        }
    }

    /* ===== ARCHIVOS ESTÁTICOS ===== */
    const cached = await cache.match(request);
    if (cached) return cached;

    try {
        return await fetch(request);
    } catch {
        return new Response("Offline", { status: 503 });
    }
}

/* =========================
   SYNC
========================= */
self.addEventListener("sync", event => {
    if (event.tag === "sync-likes") {
        event.waitUntil(enviarLikesPendientes());
    }
    if (event.tag === "sync-perfil") {
        event.waitUntil(enviarPerfilPendiente());
    }
});

/* =========================
   DB HELPERS
========================= */
function abrirLikesDB() {
    return new Promise(resolve => {
        const req = indexedDB.open(DB_NAME, 2);
        req.onupgradeneeded = e => {
            e.target.result.createObjectStore(STORE_NAME, {
                keyPath: "id",
                autoIncrement: true
            });
        };
        req.onsuccess = () => resolve(req.result);
    });
}

function abrirPerfilDB() {
    return new Promise(resolve => {
        const req = indexedDB.open(PROFILE_DB, 2);
        req.onupgradeneeded = e => {
            e.target.result.createObjectStore(PROFILE_QUEUE, {
                keyPath: "id",
                autoIncrement: true
            });
        };
        req.onsuccess = () => resolve(req.result);
    });
}

/* =========================
   ENVIAR LIKES
========================= */
async function enviarLikesPendientes() {
    const token = await obtenerToken();
    if (!token) return;

    const db = await abrirLikesDB();
    const tx = db.transaction(STORE_NAME, "readonly");
    const likes = await tx.objectStore(STORE_NAME).getAll();

    for (const l of likes) {
        await fetch(
            `https://myvexbooks2.duckdns.org/api/Libros/parte/${l.idParte}/like`,
            { method: "POST", headers: { Authorization: `Bearer ${token}` } }
        );
    }

    db.transaction(STORE_NAME, "readwrite").objectStore(STORE_NAME).clear();
}

/* =========================
   ENVIAR PERFIL
========================= */
async function enviarPerfilPendiente() {
    const token = await obtenerToken();
    if (!token) return;

    const db = await abrirPerfilDB();
    const tx = db.transaction(PROFILE_QUEUE, "readonly");
    const cambios = await tx.objectStore(PROFILE_QUEUE).getAll();

    for (const c of cambios) {
        const url = c.campo === "nombre"
            ? "/api/Usuarios/perfil/nombre"
            : "/api/Usuarios/perfil/correo";

        const body = c.campo === "nombre"
            ? { Nombre: c.valor }
            : { Correo: c.valor };

        await fetch(url, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify(body)
        });
    }

    db.transaction(PROFILE_QUEUE, "readwrite")
        .objectStore(PROFILE_QUEUE)
        .clear();
}

/* =========================
   TOKEN
========================= */
async function obtenerToken() {
    if (tokenGlobal) return tokenGlobal;

    const clientsList = await clients.matchAll({ type: "window" });
    clientsList.forEach(c => c.postMessage({ tipo: "PEDIR_TOKEN" }));

    return new Promise(resolve => {
        const i = setInterval(() => {
            if (tokenGlobal) {
                clearInterval(i);
                resolve(tokenGlobal);
            }
        }, 100);
    });
}

self.addEventListener("message", e => {
    if (e.data?.tipo === "TOKEN") {
        tokenGlobal = e.data.token;
    }
});
