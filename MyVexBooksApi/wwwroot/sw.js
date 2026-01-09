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
    event.respondWith(handleFetch(event));
});

async function handleFetch(event) {
    const request = event.request;

    // 🚨 NUNCA INTERCEPTAR PUSH / NOTIFICACIONES
    if (request.url.includes("/api/Notificaciones")) {
        return fetch(request.clone());
    }

    // 🚨 NUNCA INTERCEPTAR OPTIONS (CORS preflight)
    if (request.method === "OPTIONS") {
        return fetch(request.clone());
    }

    /* ===== 1. NAVEGACIÓN HTML (CACHE FIRST REAL) ===== */
    /* ===== 1. NAVEGACIÓN HTML (NETWORK FIRST) ===== */
    if (request.mode === "navigate") {
        try {
            return await fetch(request);
        } catch {
            const cache = await caches.open(CACHE_NAME);
            return await cache.match("offline.html");
        }
    }

   

    /* ===== API PARTE (NETWORK FIRST + CACHE) ===== */
    if (
        request.method === "GET" &&
        request.url.includes("/api/Libros/parte/") &&
        !request.url.includes("/like")
    ) {
        const cache = await caches.open(CACHE_NAME);

        try {
            const response = await fetch(request);

            // ✅ guardar copia fresca para offline
            if (response.ok) {
                cache.put(request, response.clone());
            }

            return response;
        } catch {
            // 📴 offline → usar cache
            const cached = await cache.match(request);
            if (cached) return cached;

            return new Response(JSON.stringify({
                offline: true,
                error: "Sin conexión"
            }), { headers: { "Content-Type": "application/json" } });
        }
    }


    /* ===== 2. API GET (CACHE FIRST) ===== */
    if (
        request.method === "GET" &&
        request.url.includes("/api/") &&
        !request.url.includes("/Usuarios/perfil") &&
        !request.url.includes("/Notificaciones")
    ) {


        const cache = await caches.open(CACHE_NAME);

        const cached = await cache.match(request);
        if (cached) return cached;

        try {
            const response = await fetch(request);
            cache.put(request, response.clone());
            return response;
        } catch {
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
            const url = new URL(request.url);
            const idParte = url.pathname.split("/parte/")[1]?.split("/")[0];

            let accion = "like";
            try {
                const body = await request.clone().json();
                accion = body?.accion || "like";
            } catch { }

            const db = await abrirLikesDB();
            const tx = db.transaction("pendientes", "readwrite");
            const store = tx.objectStore("pendientes");

            // 🔁 sobrescribir acción previa
            store.put({
                idParte,
                accion,
                fecha: Date.now()
            }, idParte);

            if ("sync" in self.registration) {
                await self.registration.sync.register("sync-likes");
            }

            return new Response(JSON.stringify({ offline: true }), {
                headers: { "Content-Type": "application/json" }
            });
        }
    }




    /* ===== 4. PERFIL PUT OFFLINE ===== */
    if (
        request.method === "PUT" &&
        (
            request.url.includes("/api/Usuarios/perfil/nombre") ||
            request.url.includes("/api/Usuarios/perfil/correo")
        )
    ) {

        try {
            return await fetch(request);
        } catch {
            const body = await request.clone().json();
            let campo = null;
            if (request.url.includes("nombre")) campo = "nombre";
            if (request.url.includes("correo")) campo = "correo";
            if (!campo) return fetch(request);


            const db = await abrirPerfilDB();
            const store = db.transaction("perfilPendiente", "readwrite")
                .objectStore("perfilPendiente");

            store.add({
                id: Date.now(),
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

   


    if (
        request.method === "GET" &&
        request.destination === "image" &&
        request.url.includes("/fotoPerfil/")
    ) {
        const cache = await caches.open(FOTO_CACHE);

        try {
            const res = await fetch(request);
            if (res.ok) cache.put(request, res.clone());
            return res;
        } catch {
            return await cache.match(request) ||
               await caches.match("fotoPerfil/perfil.png");
        }
    }





    /* ===== 6. ESTÁTICOS (SOLO GET) ===== */
    if (request.method === "GET") {
        const cache = await caches.open(CACHE_NAME);
        const cached = await cache.match(request);
        if (cached) return cached;

        try {
            return await fetch(request);
        } catch {
            return new Response("Offline", { status: 503 });
        }
    }

    // 🔴 cualquier POST que llegue aquí → red directa
    return fetch(request);
}

/* =========================
   BACKGROUND SYNC
========================= */
self.addEventListener("sync", event => {
    switch (event.tag) {
        case "sync-likes":
            event.waitUntil(enviarLikesPendientes());
            break;
        case "sync-perfil":
            event.waitUntil(enviarPerfilPendiente());
            break;
    }
});


self.addEventListener("message", event => {
    if (event.data?.tipo === "FORZAR_SYNC_PERFIL") {
        event.waitUntil(enviarPerfilPendiente());
    }


    if (event.data?.tipo === "TOKEN") {
        (async () => {
            const db = await abrirAuthDB();
            db.transaction("auth", "readwrite")
                .objectStore("auth")
                .put({ key: "token", value: event.data.token });
        })();
    }
    if (event.data?.tipo === "SYNC_LIKES") {
        event.waitUntil(enviarLikesPendientes());
    }
});



/* =========================
   DB HELPERS
========================= */
function abrirLikesDB() {
    return new Promise(resolve => {
        const req = indexedDB.open("likesDB", 5);

        req.onupgradeneeded = e => {
            const db = e.target.result;

            if (!db.objectStoreNames.contains("pendientes")) {
                db.createObjectStore("pendientes");
            }
        };

        req.onsuccess = () => resolve(req.result);
    });
}


function abrirPerfilDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open("perfilDB", 4);

        req.onupgradeneeded = e => {
            const db = e.target.result;

            if (!db.objectStoreNames.contains("perfil")) {
                db.createObjectStore("perfil", { keyPath: "id" });
            }

            if (!db.objectStoreNames.contains("perfilPendiente")) {
                db.createObjectStore("perfilPendiente", {
                    keyPath: "id",
                    autoIncrement: true
                });
            }
        };

        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}


function abrirAuthDB() {
    return new Promise(resolve => {
        const req = indexedDB.open("authDB", 1);
        req.onupgradeneeded = e => {
            e.target.result.createObjectStore("auth", { keyPath: "key" });
        };
        req.onsuccess = () => resolve(req.result);
    });
}

async function obtenerToken() {
    const db = await abrirAuthDB();
    return new Promise(resolve => {
        const req = db.transaction("auth")
            .objectStore("auth")
            .get("token");
        req.onsuccess = () => resolve(req.result?.value || null);
    });
}



async function enviarPerfilPendiente() {
    const db = await new Promise(resolve => {
        const req = indexedDB.open("perfilDB", 4);
        req.onsuccess = () => resolve(req.result);
    });

    // 1️⃣ LEER TODO (solo lectura)
    const pendientes = await new Promise(resolve => {
        const tx = db.transaction("perfilPendiente", "readonly");
        const store = tx.objectStore("perfilPendiente");
        const req = store.getAll();
        req.onsuccess = () => resolve(req.result || []);
    });

    if (!pendientes.length) return;

    for (const item of pendientes) {
        let exito = false;

        try {
            /* ===== FOTO PERFIL ===== */
            if (item.campo === "foto") {
                const formData = new FormData();

                formData.append(
                    "foto",
                    item.blob,
                    item.name || "perfil.jpg"
                );

                const token = await obtenerToken();

                const resp = await fetch("/api/Usuarios/perfil/foto", {
                    method: "POST",
                    headers: token
                        ? { "Authorization": `Bearer ${token}` }
                        : {},
                    body: formData
                });

                if (!resp.ok) {
                    const text = await resp.text();
                    console.error("RESPUESTA FOTO:", text);
                    throw new Error("Error subiendo foto de perfil");
                }

                // limpiar cache
                const cache = await caches.open(FOTO_CACHE);
                const keys = await cache.keys();
                await Promise.all(keys.map(k => cache.delete(k)));



                exito = true;

                // ✅ GUARDAR FOTO FINAL LOCAL (PERSISTENTE)
                const dbFinal = await abrirPerfilDB();
                const txFinal = dbFinal.transaction("perfil", "readwrite");
                txFinal.objectStore("perfil").put({
                    id: "fotoFinal",
                    blob: item.blob,
                    type: item.type,
                    fecha: Date.now()
                });


                self.clients.matchAll().then(clients => {
                    clients.forEach(c => {
                        c.postMessage({
                            tipo: "FOTO_ACTUALIZADA",
                            ts: Date.now()
                        });
                    });
                });

            }



            /* ===== TEXTO ===== */
            if (item.campo === "nombre" || item.campo === "correo") {
                const url =
                    item.campo === "nombre"
                        ? "/api/Usuarios/perfil/nombre"
                        : "/api/Usuarios/perfil/correo";

                const body =
                    item.campo === "nombre"
                        ? { Nombre: item.valor }
                        : { Correo: item.valor };

                const token = await obtenerToken();

                const resp = await fetch(url, {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                        ...(token ? { "Authorization": `Bearer ${token}` } : {})
                    },
                    body: JSON.stringify(body)
                });

                if (!resp.ok) {
                    const text = await resp.text();
                    console.error("RESPUESTA SERVIDOR PERFIL:", text);
                    throw new Error("Error actualizando perfil");
                }

                exito = true;


            }

        } catch (err) {
            console.error("Error enviando perfil:", err);
            // ❌ NO borrar, se reintentará en el próximo sync
        }


        // 2️⃣ BORRAR EN TRANSACCIÓN NUEVA
        if (exito) {
            const txDelete = db.transaction("perfilPendiente", "readwrite");
            txDelete.objectStore("perfilPendiente").delete(item.id);
        }

    }
}


async function enviarLikesPendientes() {
    const db = await abrirLikesDB();

    const pendientes = await new Promise(res => {
        const tx = db.transaction("pendientes", "readonly");
        const store = tx.objectStore("pendientes");
        const req = store.getAllKeys();
        req.onsuccess = async () => {
            const claves = req.result;
            const valores = await Promise.all(
                claves.map(k =>
                    new Promise(r => {
                        const g = store.get(k);
                        g.onsuccess = () => r({ idParte: k, ...g.result });
                    })
                )
            );
            res(valores);
        };
    });


    if (!pendientes.length) return;

    const token = await obtenerToken();

    for (const item of pendientes) {
        try {
            const resp = await fetch(`/api/Libros/parte/${item.idParte}/like`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    ...(token ? { Authorization: `Bearer ${token}` } : {})
                },
                body: JSON.stringify({ accion: item.accion })
            });

            if (!resp.ok) throw new Error("Error enviando like");
            // 🧹 limpiar cache de la parte
            const cache = await caches.open(CACHE_NAME);
            const keys = await cache.keys();

            await Promise.all(
                keys
                    .filter(r => r.url.includes(`/api/Libros/parte/${item.idParte}`))
                    .map(r => cache.delete(r))
            );

            // ✅ borrar si fue exitoso
            const tx = db.transaction("pendientes", "readwrite");
            tx.objectStore("pendientes").delete(item.idParte);

        } catch (err) {
            console.error("Like pendiente falló, se reintentará", err);
        }
    }
}


self.addEventListener("push", event => {
    let data = {};
    try {
        data = event.data.json();
    } catch {
        data = {
            titulo: "Notificación",
            mensaje: event.data.text(),
            idLibro: null,
            portada: null
        };
    }

    const tagUnico = `libro-${data.idLibro}-${Date.now()}`;

    const options = {
        body: data.mensaje || "",
        icon: data.portada || "img/logo.png",
        tag: tagUnico,
        data: { idLibro: data.idLibro }
    };

    event.waitUntil(
        self.registration.showNotification(data.titulo || "Notificación", options)
            .then(() => {
                return self.clients.matchAll({ includeUncontrolled: true }).then(clients => {
                    clients.forEach(c => {
                        c.postMessage({
                            tipo: "RECIBIDA",
                            titulo: data.titulo,
                            mensaje: data.mensaje,
                            idLibro: data.idLibro,
                            portada: data.portada
                        });
                    });
                });
            })
    );
});




self.addEventListener("notificationclick", event => {
    console.log("CLICK NOTI:", event.notification.data); // 👈 AQUÍ

    event.notification.close();

    const idLibro = event.notification.data?.idLibro;

    const url = idLibro
        ? `/libro.html?id=${idLibro}`
        : "/home.html";

    event.waitUntil(
        clients.openWindow(url)
    );
});

