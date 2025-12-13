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
