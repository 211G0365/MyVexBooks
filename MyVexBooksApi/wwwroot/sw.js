/* =========================
   CONFIG
========================= */
const CACHE_NAME = "cache-libros-v4";
const FOTO_CACHE = "foto-perfil-cache-v2";

/* =========================
   PRECACHE
========================= */
const PRECACHE_URLS = [
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
   INSTALL
========================= */
self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_URLS))
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
                    if (![CACHE_NAME, FOTO_CACHE].includes(k)) {
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
    event.respondWith(handleFetch(event.request));
});

async function handleFetch(request) {

    /* ===== 1. NAVEGACIÓN HTML (CACHE FIRST REAL) ===== */
    if (request.mode === "navigate") {
        const cache = await caches.open(CACHE_NAME);

        // 1️⃣ responder inmediato desde cache
        const cached = await cache.match(request.url);
        if (cached) {
            // 2️⃣ actualizar en background si hay red
            eventWaitUntilSafe(
                fetch(request).then(res => {
                    if (res && res.ok) cache.put(request.url, res.clone());
                }).catch(() => { })
            );
            return cached;
        }

        // 3️⃣ si no está en cache, intenta red
        try {
            const response = await fetch(request);
            cache.put(request.url, response.clone());
            return response;
        } catch {
            return cache.match("offline.html");
        }
    }


    /* ===== 2. API GET (CACHE FIRST) ===== */
    if (
        request.method === "GET" &&
        request.url.includes("/api/") &&
        !request.url.includes("/api/Usuarios/perfil")
    ) {

        const cache = await caches.open(CACHE_NAME);

        // 1️⃣ responder cache inmediato
        const cached = await cache.match(request);
        if (cached) return cached;

        // 2️⃣ red → cache
        try {
            const response = await fetch(request);
            cache.put(request, response.clone());
            return response;
        } catch {
            // 3️⃣ fallback perfil vacío controlado
            if (request.url.includes("/Usuarios/perfil")) {
                return new Response(JSON.stringify({
                    nombre: "Sin conexión",
                    correo: "",
                    offline: true
                }), { headers: { "Content-Type": "application/json" } });
            }

            return new Response(JSON.stringify([]), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }


    /* ===== 3. LIKE OFFLINE ===== */
    if (request.method === "POST" && request.url.includes("/like")) {
        try {
            return await fetch(request);
        } catch {
            const idParte = request.url.split("/parte/")[1].split("/")[0];
            const db = await abrirLikesDB();
            db.transaction("pendientes", "readwrite")
                .objectStore("pendientes")
                .add({ idParte, fecha: Date.now() });

            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-likes");
            }

            return new Response(JSON.stringify({ offline: true }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    /* ===== 4. PERFIL PUT OFFLINE ===== */
    if (request.method === "PUT" && request.url.includes("/api/Usuarios/perfil")) {
        try {
            return await fetch(request);
        } catch {
            const body = await request.clone().json();
            const campo = request.url.includes("nombre") ? "nombre" : "correo";

            const db = await abrirPerfilDB();
            db.transaction("perfilPendiente", "readwrite")
                .objectStore("perfilPendiente")
                .add({ campo, valor: body.Nombre || body.Correo });

            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-perfil");
            }

            return new Response(JSON.stringify({ offline: true }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    /* ===== 5. FOTO PERFIL (CACHE FIRST REAL) ===== */
    if (
        request.method === "GET" &&
        (
            request.url.includes("/Usuarios/perfil/foto") ||
            request.url.includes("/fotoPerfil/")
        )
    ) {
        const cache = await caches.open(FOTO_CACHE);

        // 1️⃣ cache inmediato
        const cached = await cache.match(request);
        if (cached) return cached;

        // 2️⃣ red → cache
        try {
            const response = await fetch(request);
            cache.put(request, response.clone());
            return response;
        } catch {
            // 3️⃣ fallback imagen default
            return caches.match("fotoPerfil/perfil.png");
        }
    }


    /* ===== 6. ESTÁTICOS ===== */
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    if (cached) return cached;

    try {
        return await fetch(request);
    } catch {
        return new Response("Offline", { status: 503 });
    }
}

/* =========================
   BACKGROUND SYNC
========================= */
self.addEventListener("sync", event => {
    if (event.tag === "sync-likes") event.waitUntil(enviarLikesPendientes());
    if (event.tag === "sync-perfil") event.waitUntil(enviarPerfilPendiente());
   
});

self.addEventListener("message", event => {
     if (event.data?.tipo === "FORZAR_SYNC_PERFIL") {
        enviarPerfilPendiente();
    }
    if (event.data?.tipo === "TOKEN") {
        self.token = event.data.token;
    }
});


/* =========================
   DB HELPERS
========================= */
function abrirLikesDB() {
    return new Promise(resolve => {
        const req = indexedDB.open("likesDB", 1);
        req.onupgradeneeded = e =>
            e.target.result.createObjectStore("pendientes", { autoIncrement: true });
        req.onsuccess = () => resolve(req.result);
    });
}

function abrirPerfilDB() {
    return new Promise(resolve => {
        const req = indexedDB.open("perfilDB", 1);
        req.onupgradeneeded = e =>
            e.target.result.createObjectStore("perfilPendiente", { autoIncrement: true });
        req.onsuccess = () => resolve(req.result);
    });
}

function eventWaitUntilSafe(promise) {
    try {
        self.registration.active && self.registration.active.waitUntil?.(promise);
    } catch { }
}

async function enviarPerfilPendiente() {
    const db = await new Promise(resolve => {
        const req = indexedDB.open("perfilDB", 2);
        req.onsuccess = () => resolve(req.result);
    });

    const tx = db.transaction("perfilPendiente", "readwrite");
    const store = tx.objectStore("perfilPendiente");

    const pendientes = await new Promise(resolve => {
        const req = store.getAll();
        req.onsuccess = () => resolve(req.result);
    });

    if (!pendientes.length) return;

    for (const item of pendientes) {
        try {

            /* ===== FOTO PERFIL ===== */
            if (item.campo === "foto") {
                const blob = base64ToBlob(item.valor);
                const formData = new FormData();
                formData.append("Archivo", blob, "perfil.jpg");

                await fetch("/api/Usuarios/perfil/foto", {
                    method: "POST",
                    headers: {
                        "Authorization": self.token ? `Bearer ${self.token}` : ""
                    },
                    body: formData
                });

                store.delete(item.id); // 👈 AHORA SÍ SE BORRA
                continue;
            }


            /* ===== CAMPOS TEXTO ===== */
            let url = "";
            let body = {};

            if (item.campo === "nombre") {
                url = "/api/Usuarios/perfil/nombre";
                body = { Nombre: item.valor };
            }

            if (item.campo === "correo") {
                url = "/api/Usuarios/perfil/correo";
                body = { Correo: item.valor };
            }

            await fetch(url, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": self.token ? `Bearer ${self.token}` : ""
                },
                body: JSON.stringify(body)
            });

            store.delete(item.id);

        } catch (err) {
            console.warn("Aún offline, se reintentará luego");
        }
    }
}

