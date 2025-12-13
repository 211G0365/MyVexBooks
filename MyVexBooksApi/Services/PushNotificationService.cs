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
            vapid = new VapidDetails
            {
                PrivateKey = configuration["VapidKeys:privateKey"],
                PublicKey = configuration["VapidKeys:publicKey"],
                Subject = configuration["VapidKeys:subject"]
            };

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
            }
        }
        public string GetPublicKey()
        {
            return vapid.PublicKey;
        }


        public async Task EnviarMensaje(string titulo, string mensaje)
        {
            var destinatarios = Repository.GetAll().Where(x => x.Activo==true).ToList();

            foreach (var d in destinatarios)
            {
                try
                {
                    var cliente = new WebPushClient();
                    var sub = new PushSubscription(d.Endpoint, d.P256dh, d.Auth);

                    var message = new { titulo, mensaje };
                    await cliente.SendNotificationAsync(sub, JsonSerializer.Serialize(message), vapid);

                  
                    Repository.Update(d);
                }
                catch (WebPushException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        Repository.Delete(d.Id);
                    }
                }
            }
        }

        public void Desuscribir(string endpoint)
        {
            var sub = Repository.GetAll().FirstOrDefault(x => x.Endpoint == endpoint);
            if (sub != null)
            {
                sub.Activo = false;
                Repository.Update(sub);
            }
        }

    }
}
