using MyVexBooks.Repositories;
using System.Text.Json;
using MyVexBooks.Models.DTOs;
using WebPush;
using MyVexBooks.Models.Entities;

namespace MyVexBooks.Services
{
    public class PushNotificationService
    {
        VapidDetails vapid;
        public IRepository<Notificaciones> Repository { get; }
        public IConfiguration Configuration { get; }

        public PushNotificationService(IRepository<Notificaciones> repository, IConfiguration configuration)
        {
            Repository = repository;
            Configuration = configuration;

            try
            {
                var privateKey = configuration["VAPID:privateKey"];
                var publicKey = configuration["VAPID:publicKey"];
                var subject = configuration["VAPID:subject"];

                if (string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(subject))
                {
                    Console.WriteLine("ERROR: Configuración VAPID incompleta.");
                    Console.WriteLine($"privateKey: {privateKey}");
                    Console.WriteLine($"publicKey: {publicKey}");
                    Console.WriteLine($"subject: {subject}");
                }
                else
                {
                    Console.WriteLine("VAPID Configuración leída correctamente:");
                    Console.WriteLine($"publicKey: {publicKey}");
                    Console.WriteLine($"subject: {subject}");
                }

                vapid = new VapidDetails
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    Subject = subject
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR al crear VapidDetails: " + ex);
                throw;
            }
        }

        public void Suscribir(SubscriptionDTO dto)
        {
            var entidad = Repository.GetAll().FirstOrDefault(x => x.Endpoint == dto.Endpoint);

            if (entidad == null)
            {
                entidad = new Notificaciones
                {
                    Endpoint = dto.Endpoint,
                    Auth = dto.Keys.Auth,
                    P256dh = dto.Keys.P256dh,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                Repository.Insert(entidad);
                Console.WriteLine($"Nueva suscripción registrada: {dto.Endpoint}");
            }
            else
            {
                Console.WriteLine($"Suscripción ya existente: {dto.Endpoint}");
            }
        }

        public void Desuscribir(string endpoint)
        {
            var entidad = Repository.GetAll().FirstOrDefault(x => x.Endpoint == endpoint);
            if (entidad != null)
            {
                Repository.Delete(entidad);
                Console.WriteLine($"Suscripción eliminada: {endpoint}");
            }
        }

        public string GetPublicKey()
        {
            if (vapid == null || string.IsNullOrEmpty(vapid.PublicKey))
            {
                Console.WriteLine("WARNING: VapidDetails o PublicKey es null.");
                return null;
            }

            Console.WriteLine($"GET PublicKey: {vapid.PublicKey}");
            return vapid.PublicKey;
        }

        public async Task EnviarMensaje(object payload)
        {
            // Obtener todos los suscriptores activos
            var destinatarios = Repository.GetAll().Where(x => x.Activo == true).ToList();
            Console.WriteLine($"Enviando mensaje a {destinatarios.Count} destinatarios.");

            foreach (var d in destinatarios)
            {
                try
                {
                    var cliente = new WebPushClient();
                    PushSubscription quien = new PushSubscription(d.Endpoint, d.P256dh, d.Auth);

                    // Serializar el payload completo (titulo, mensaje, idLibro, portada, tag)
                    var jsonMessage = JsonSerializer.Serialize(payload);

                    await cliente.SendNotificationAsync(quien, jsonMessage, vapid);

                    // Marcar como actualizado (opcional)
                    Repository.Update(d);

                    Console.WriteLine($"Mensaje enviado a {d.Endpoint}");
                }
                catch (WebPushException ex)
                {
                    Console.WriteLine($"Error enviando a {d.Endpoint}: {ex.Message}");
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        Repository.Delete(d.Id);
                        Console.WriteLine($"Suscripción eliminada por estar obsoleta: {d.Endpoint}");
                    }
                }
            }
        }

    }

}
